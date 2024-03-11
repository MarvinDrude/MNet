
namespace MNet.Sockets;

// taken from kestrel internal sockets
internal sealed class SockConnection : ITransportConnection {

    private static readonly int MinAllocBufferSize = PinnedBlockMemoryPool.BlockSize / 2;

    public MemoryPool<byte> MemoryPool { get; }
    public CancellationToken ConnectionClosed { get; }

    public EndPoint? LocalEndPoint { get; }
    public EndPoint? RemoteEndPoint { get; }

    public IDuplexPipe Transport { get; }
    public IDuplexPipe Application { get; }

    private readonly Socket Socket;
    private readonly SockReceiver Receiver;
    private SockSender? Sender;

    private readonly SockSenderPool SenderPool;
    private readonly IDuplexPipe OgTransport;

    private readonly CancellationTokenSource ConnectionClosedTokenSource = new ();

    private readonly object ShutdownLock = new();
    private volatile Exception? ShutdownReason;

    private Task? SendingTask;
    private Task? ReceivingTask;

    private readonly TaskCompletionSource WaitForConnectionClosedTcs = new ();
    private bool _connectionClosed;

    private readonly bool WaitForData;
    private readonly bool FinOnError;

    public SockConnection(
        Socket socket, MemoryPool<byte> memoryPool,
        PipeScheduler socketScheduler, SockSenderPool senderPool,
        PipeOptions inputOptions, PipeOptions outputOptions,
        bool waitForData = true, bool finOnError = false) {

        Socket = socket;
        MemoryPool = memoryPool;

        WaitForData = waitForData;
        FinOnError = finOnError;
        SenderPool = senderPool;

        LocalEndPoint = Socket.LocalEndPoint;
        RemoteEndPoint = Socket.RemoteEndPoint;

        ConnectionClosed = ConnectionClosedTokenSource.Token;
        Receiver = new SockReceiver(socketScheduler);

        var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

        Transport = OgTransport = pair.Transport;
        Application = pair.Application;

    }

    public PipeWriter Input => Application.Output;

    public PipeReader Output => Application.Input;

    public void Start() {

        try {

            ReceivingTask = DoReceive();
            SendingTask = DoSend();

        } catch (Exception) {

        }

    }

    public void Abort(Exception abortReason) {

        // Try to gracefully close the socket to match libuv behavior.
        Shutdown(abortReason);

        // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
        Output.CancelPendingRead();

    }

    // Only called after connection middleware is complete which means the ConnectionClosed token has fired.
    public async ValueTask DisposeAsync() {

        OgTransport.Input.Complete();
        OgTransport.Output.Complete();

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

            Receiver.Dispose();
            Sender?.Dispose();

        }

        ConnectionClosedTokenSource.Dispose();

    }

    private async Task DoReceive() {

        Exception? error = null;

        try {

            while (ShutdownReason is null) {

                if (WaitForData) {

                    // Wait for data before allocating a buffer.
                    var waitForDataResult = await Receiver.WaitForDataAsync(Socket);

                    if (!IsNormalCompletion(waitForDataResult)) {
                        break;
                    }

                }

                // Ensure we have some reasonable amount of buffer space
                var buffer = Input.GetMemory(MinAllocBufferSize);
                var receiveResult = await Receiver.ReceiveAsync(Socket, buffer);

                if (!IsNormalCompletion(receiveResult)) {
                    break;
                }

                var bytesReceived = receiveResult.BytesTransferred;

                if (bytesReceived == 0) {
                    // FIN
                    break;
                }

                Input.Advance(bytesReceived);

                var flushTask = Input.FlushAsync();
                var paused = !flushTask.IsCompleted;
                var result = await flushTask;

                if (result.IsCompleted || result.IsCanceled) {
                    // Pipe consumer is shut down, do we stop writing
                    break;
                }

                bool IsNormalCompletion(SockOperationResult result) {
                    // There's still a small chance that both DoReceive() and DoSend() can log the same connection reset.
                    // Both logs will have the same ConnectionId. I don't think it's worthwhile to lock just to avoid this.
                    // When _shutdownReason is set, error is ignored, so it does not need to be initialized.
                    if (ShutdownReason is not null) {
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

        } catch (ObjectDisposedException ex) {

            // This exception should always be ignored because _shutdownReason should be set.
            error = ex;

        } catch (Exception ex) {

            // This is unexpected.
            error = ex;

        } finally {

            // If Shutdown() has already been called, assume that was the reason ProcessReceives() exited.
            Input.Complete(ShutdownReason ?? error);

            FireConnectionClosed();
            await WaitForConnectionClosedTcs.Task;

        }
    }

    private async Task DoSend() {

        Exception? shutdownReason = null;
        Exception? unexpectedError = null;

        try {

            while (true) {

                var result = await Output.ReadAsync();

                if (result.IsCanceled) {
                    break;
                }

                var buffer = result.Buffer;

                if (!buffer.IsEmpty) {

                    Sender = SenderPool.Rent();
                    var transferResult = await Sender.SendAsync(Socket, buffer);

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
                    SenderPool.Return(Sender);
                    Sender = null;

                }

                Output.AdvanceTo(buffer.End);

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
            Output.Complete(unexpectedError);

            // Cancel any pending flushes so that the input loop is un-paused
            Input.CancelPendingFlush();

        }

    }

    private void FireConnectionClosed() {

        // Guard against scheduling this multiple times
        if (_connectionClosed) {
            return;
        }

        _connectionClosed = true;

        ThreadPool.UnsafeQueueUserWorkItem(state => {
            state.CancelConnectionClosedToken();

            state.WaitForConnectionClosedTcs.TrySetResult();
        },
        this,
        preferLocal: false);

    }

    private void Shutdown(Exception? shutdownReason) {

        lock (ShutdownLock) {

            if (ShutdownReason is not null) {
                return;
            }

            // Make sure to dispose the socket after the volatile _shutdownReason is set.
            // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
            // a BadHttpRequestException is thrown instead of a TaskCanceledException.
            //
            // The shutdownReason argument should only be null if the output was completed gracefully, so no one should ever
            // ever observe this ConnectionAbortedException except for connection middleware attempting
            // to half close the connection which is currently unsupported. The message is always logged though.
            ShutdownReason = shutdownReason ?? new Exception("The Socket transport's send loop completed gracefully.");

            // NB: not _shutdownReason since we don't want to do this on graceful completion
            if (!FinOnError && shutdownReason is not null) {

                // This forces an abortive close with linger time 0 (and implies Dispose)
                Socket.Close(timeout: 0);
                return;

            }

            try {
                Socket.Shutdown(SocketShutdown.Both);
            } catch {
                // Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway.
            }

            Socket.Dispose();

        }

    }

    private void CancelConnectionClosedToken() {

        try {

            ConnectionClosedTokenSource.Cancel();

        } catch (Exception) {

        }

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


