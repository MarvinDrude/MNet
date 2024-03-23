
using System.Diagnostics;

namespace MNet.Internal;

internal sealed class SocketConnection : IDuplexPipe, IAsyncDisposable {

    private static readonly int MinAllocBufferSize = PinnedBlockMemoryPool.BlockSize / 2;

    public PipeReader Input {
        get {
            return _Input;
        }
    }

    public PipeWriter Output {
        get {
            return _Output;
        }
    }

    private readonly CancellationTokenSource _ConnectionClosedTokenSource = new();
    private readonly TaskCompletionSource _WaitForConnectionClosedTcs = new();
    private bool _ConnectionClosed;

    private readonly bool _WaitForData = false;
    private readonly bool _FinOnError = true;

    private readonly Pipe _SendSocket;
    private readonly Pipe _ReceiveSocket;

    private readonly SocketReceiver _Receiver;
    private SocketSender? _Sender;

    private readonly SocketSenderPool _SenderPool;

    private readonly PipeReader _Input;
    private readonly PipeWriter _Output;

    private Task? SendingTask;
    private Task? ReceivingTask;

    private readonly Socket _Socket;
    private Exception? _ShutdownReason;
    private readonly object _ShutdownLock = new();

    private readonly MemoryPool<byte> _MemoryPool;

    public SocketConnection(Socket socket,
        MemoryPool<byte> memoryPool, PipeScheduler scheduler,
        SocketSenderPool senderPool, PipeOptions sendOptions,
        PipeOptions receiveOptions) {

        _Socket = socket;
        _MemoryPool = memoryPool;

        _Receiver = new SocketReceiver(scheduler);
        _SenderPool = senderPool;

        _SendSocket = new Pipe(sendOptions);
        _ReceiveSocket = new Pipe(receiveOptions);

        _Input = _ReceiveSocket.Reader;
        _Output = _SendSocket.Writer;

        Start();

    }

    public void Start() {

        try {

            ReceivingTask = DoReceive();
            SendingTask = DoSend();

        } catch (Exception) {

        }

    }

    private async Task DoReceive() {

        Exception? error = null;

        try {

            while (_ShutdownReason == null) {

                if (_WaitForData) {

                    var waitForDataResult = await _Receiver.WaitForDataAsync(_Socket);

                    if (!IsNormalCompletion(waitForDataResult)) {
                        break;
                    }

                }

                var buffer = _ReceiveSocket.Writer.GetMemory(MinAllocBufferSize);
                var receiveResult = await _Receiver.ReceiveAsync(_Socket, buffer);

                if (!IsNormalCompletion(receiveResult)) {
                    break;
                }

                var bytesReceived = receiveResult.BytesTransferred;

                if (bytesReceived == 0) {
                    // FIN
                    break;
                }

                _ReceiveSocket.Writer.Advance(bytesReceived);

                var flushTask = _ReceiveSocket.Writer.FlushAsync();
                var paused = !flushTask.IsCompleted;
                var result = await flushTask;

                if (result.IsCompleted || result.IsCanceled) {
                    // Pipe consumer is shut down, do we stop writing
                    break;
                }

                bool IsNormalCompletion(SocketOperationResult result) {

                    // There's still a small chance that both DoReceive() and DoSend() can log the same connection reset.
                    // Both logs will have the same ConnectionId. I don't think it's worthwhile to lock just to avoid this.
                    // When _shutdownReason is set, error is ignored, so it does not need to be initialized.
                    if (_ShutdownReason is not null) {
                        return false;
                    }

                    if (!result.HasError) {
                        return true;
                    }

                    if (IsConnectionResetError(result.SocketError.SocketErrorCode)) {
                        return false;
                    }

                    if (IsConnectionAbortError(result.SocketError.SocketErrorCode)) {
                        error = result.SocketError;
                        return false;
                    }

                    // This is unexpected.
                    error = result.SocketError;

                    return false;

                }

            }

        } catch (SocketException er) when (er.SocketErrorCode == SocketError.ConnectionReset) {

            error = er;

        } catch (SocketException er) when (er.SocketErrorCode == SocketError.Interrupted
            || er.SocketErrorCode == SocketError.ConnectionAborted
            || er.SocketErrorCode == SocketError.OperationAborted
            || er.SocketErrorCode == SocketError.InvalidArgument) {

            error = er;

        } catch (ObjectDisposedException er) {

            error = er;

        } catch (Exception er) {

            error = er;

        } finally {

            _ReceiveSocket.Writer.Complete(_ShutdownReason ?? error);

            FireConnectionClosed();
            await _WaitForConnectionClosedTcs.Task;

        }

    }

    private async Task DoSend() {

        Exception? shutdownReason = null;
        Exception? unexpectedError = null;

        try {

            while (true) {

                var result = await _SendSocket.Reader.ReadAsync();

                if (result.IsCanceled) {
                    break;
                }

                var buffer = result.Buffer;

                if (!buffer.IsEmpty) {

                    _Sender = _SenderPool.Rent();
                    var transferResult = await _Sender.SendAsync(_Socket, buffer);

                    if (transferResult.HasError) {

                        if (IsConnectionResetError(transferResult.SocketError.SocketErrorCode)) {

                            var ex = transferResult.SocketError;
                            shutdownReason = new Exception(ex.Message, ex);

                            break;

                        }

                        if (IsConnectionAbortError(transferResult.SocketError.SocketErrorCode)) {

                            shutdownReason = transferResult.SocketError;
                            break;

                        }

                        unexpectedError = shutdownReason = transferResult.SocketError;

                    }

                    // We don't return to the pool if there was an exception, and
                    // we keep the _sender assigned so that we can dispose it in StartAsync.
                    _SenderPool.Return(_Sender);
                    _Sender = null;

                }

                _SendSocket.Reader.AdvanceTo(buffer.End);

                if (result.IsCompleted) {
                    break;
                }

            }

        } catch (ObjectDisposedException ex) {

            // This should always be ignored since Shutdown() must have already been called by Abort().
            shutdownReason = ex;

        } catch (Exception ex) {

            shutdownReason = ex;
            unexpectedError = ex;

        } finally {

            Shutdown(shutdownReason);

            // Complete the output after disposing the socket
            try { _SendSocket.Writer.Complete(unexpectedError); } catch { }
            try { _SendSocket.Reader.Complete(unexpectedError); } catch { }

            // Cancel any pending flushes so that the input loop is un-paused
            _ReceiveSocket.Writer.CancelPendingFlush();

        }

    }

    private void FireConnectionClosed() {

        // Guard against scheduling this multiple times
        if (_ConnectionClosed) {
            return;
        }

        _ConnectionClosed = true;

        ThreadPool.UnsafeQueueUserWorkItem(state => {
            state.CancelConnectionClosedToken();

            state._WaitForConnectionClosedTcs.TrySetResult();
        },
        this,
        preferLocal: false);

    }

    private void CancelConnectionClosedToken() {

        try {

            _ConnectionClosedTokenSource.Cancel();

        } catch (Exception) {

        }

    }

    private void Shutdown(Exception? shutdownReason) {

        lock (_ShutdownLock) {

            if (_ShutdownReason != null) {
                return;
            }

            // Make sure to dispose the socket after the volatile _shutdownReason is set.
            // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
            // a BadHttpRequestException is thrown instead of a TaskCanceledException.
            //
            // The shutdownReason argument should only be null if the output was completed gracefully, so no one should ever
            // ever observe this ConnectionAbortedException except for connection middleware attempting
            // to half close the connection which is currently unsupported. The message is always logged though.
            _ShutdownReason = shutdownReason ?? new Exception("The Socket transport's send loop completed gracefully.");

            // NB: not _shutdownReason since we don't want to do this on graceful completion
            if (!_FinOnError && shutdownReason is not null) {

                // This forces an abortive close with linger time 0 (and implies Dispose)
                _Socket.Close(timeout: 0);
                return;

            }

            try {
                _Socket.Shutdown(SocketShutdown.Both);
            } catch {
                // Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway.
            }

            _Socket.Dispose();

        }

    }

    public async ValueTask DisposeAsync() {

        try {

            _ReceiveSocket.Writer.Complete();
            _ReceiveSocket.Reader.Complete();

            _SendSocket.Writer.Complete();
            _SendSocket.Reader.Complete();

        } catch (Exception er) {

            Debug.Assert(er == null);

        }

        try {

            // Now wait for both to complete
            if (ReceivingTask != null) {
                await ReceivingTask;
            }

            if (SendingTask != null) {
                await SendingTask;
            }

        } catch (Exception) {

        } finally {

            _Receiver.Dispose();
            _Sender?.Dispose();

        }

        _ConnectionClosedTokenSource.Dispose();

    }

    private static bool IsConnectionResetError(SocketError errorCode) {

        return errorCode == SocketError.ConnectionReset ||
               errorCode == SocketError.Shutdown ||
               (errorCode == SocketError.ConnectionAborted && OperatingSystem.IsWindows());

    }

    private static bool IsConnectionAbortError(SocketError errorCode) {

        // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
        return errorCode == SocketError.OperationAborted ||
               errorCode == SocketError.Interrupted ||
               (errorCode == SocketError.InvalidArgument && !OperatingSystem.IsWindows());

    }

}
