

namespace MNet.Tcp;

public class TcpClient : TcpBase, IAsyncDisposable, ITcpSender {

    public TcpClientOptions Options { get; private set; }

    public delegate void ConnectHandler();
    public event ConnectHandler? OnConnect;

    public delegate void DisconnectHandler();
    public event DisconnectHandler? OnDisconnect;

    private bool IsHandshaked { get; set; }
    private Channel<ITcpFrame> OutgoingFramesQueue { get; set; } = Channel.CreateUnbounded<ITcpFrame>();
    private IDuplexPipe? DuplexPipe { get; set; }
    private Stream? Stream { get; set; }

    public TcpClient(TcpClientOptions options) {

        if (options.IsSecure) {
            ArgumentNullException.ThrowIfNull(options.Host);
        }

        if (!IPAddress.TryParse(options.Address, out var address)) {
            throw new ArgumentException($"{nameof(options.Address)} is not a valid IP address.");
        }

        Options = options;
        Logger = Options.Logger;

        EndPoint = new IPEndPoint(address, options.Port);
        EventEmitter = new EventEmitter(Options.Serializer);

        InitFactory();

    }

    public void Connect() {

        if (RunTokenSource != null) {
            return;
        }

        Logger.LogDebug("{Source} Connecting the tcp client...", this);

        IsHandshaked = false;
        OutgoingFramesQueue = Channel.CreateUnbounded<ITcpFrame>();

        RunTokenSource = new CancellationTokenSource();
        var _ = DoConnect(RunTokenSource.Token);

    }

    public async Task Disconnect() {

        if (RunTokenSource == null) {
            return;
        }

        Logger.LogDebug("{Source} Disconnecting the tcp client...", this);

        IsHandshaked = false;

        try {
            RunTokenSource?.Cancel();
            RunTokenSource?.Dispose();
        } catch (Exception) { }

        try {
            if(OutgoingFramesQueue.Writer.TryComplete()) {

            }
        } catch(Exception) { }

        RunTokenSource = null;

        if (DuplexPipe != null && DuplexPipe is SocketConnection conn) {

            await conn.DisposeAsync();

        } else {

            try {

                Socket?.Shutdown(SocketShutdown.Both);

            } catch (Exception) {
            }

            Stream?.Dispose();
            Socket?.Dispose();

        }

        OnDisconnect?.Invoke();
        Logger.LogInformation("{Source} Tcp client disconnected. {Endpoint}", this, EndPoint);

    }

    private async Task InternalDisconnect() {

        await Disconnect();
        await Task.Delay(Options.ReconnectInterval);

        Connect();

    }

    public void On<T>(string identifier, EventDelegate<T> handler) {
        InternalOn<T>(identifier, handler);
    }

    public void On<T>(string identifier, EventDelegateAsync<T> handler) {
        InternalOn<T>(identifier, handler);
    }

    private void InternalOn<T>(string identifier, Delegate handler) {
        EventEmitter.On<T>(identifier, handler);
    }

    public void Send<T>(string identifier, T payload) where T : class {

        if (identifier.StartsWith(TcpConstants.StartSequenceSerialize)) {
            throw new ArgumentOutOfRangeException("Send identifier invalid.");
        }

        using var frame = Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.Identifier = TcpConstants.StartSequenceSerialize + identifier;

        frame.IsRawOnly = false;
        frame.IsSending = true;

        frame.Data = Options.Serializer.SerializeAsMemory(payload);
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    public void Send(string identifier, Memory<byte> payload) {

        if (identifier.StartsWith(TcpConstants.StartSequenceSerialize)) {
            throw new ArgumentOutOfRangeException("Send identifier invalid.");
        }

        using var frame = Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.Identifier = identifier;

        frame.IsRawOnly = false;
        frame.IsSending = true;

        frame.Data = payload;
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    /// <summary>
    /// Only use for handshaking
    /// </summary>
    /// <param name="payload"></param>
    public void Send(Memory<byte> payload) {

        using var frame = Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.IsRawOnly = true;
        frame.IsSending = true;

        frame.Data = payload;
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    void ITcpSender.Send(ITcpFrame frame) {

        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    private async Task DoConnect(CancellationToken token) {

        while(!token.IsCancellationRequested) {

            try {

                Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                Socket.NoDelay = true;

                Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                await Socket.ConnectAsync(EndPoint);

                if(ConnectionType == TcpUnderlyingConnectionType.FastSocket) {

                    DuplexPipe = ConnectionFactory.Create(Socket, null);

                } else {

                    Stream = await GetStream(Socket);
                    DuplexPipe = ConnectionFactory.Create(Socket, Stream);

                }

                var _ = DoReceive(token);
                _ = DoSend(token);

                Logger.LogInformation("{Source} Tcp client connected. {Endpoint}", this, EndPoint);
                return; // exit this connecting loop

            } catch(Exception) {



            }

            await Task.Delay(Options.ReconnectInterval, token);

        }

    }

    private async Task DoReceive(CancellationToken token) {

        var frame = Options.FrameFactory.Create();

        try {

            if (Options.Handshaker.StartHandshake(this)) {

                Logger.LogDebug("{Source} handshaked successfully", this);
                IsHandshaked = true;
                OnConnect?.Invoke();

            }

            while (!token.IsCancellationRequested
                && DuplexPipe != null) {

                var result = await DuplexPipe.Input.ReadAsync(token);
                var position = ParseFrame(ref frame, ref result);

                DuplexPipe.Input.AdvanceTo(position);

                if (result.IsCanceled || result.IsCompleted) {
                    break;
                }

            }

        } catch (Exception) {

        } finally {

            frame?.Dispose();
            await InternalDisconnect();

        }

    }

    private SequencePosition ParseFrame(ref ITcpFrame frame, ref ReadResult result) {

        var buffer = result.Buffer;

        if (buffer.Length == 0) {
            return buffer.Start;
        }

        if(IsHandshaked) {

            var endPosition = frame.Read(ref buffer);

            if (frame.Identifier != null) {

                EventEmitter.Emit(frame.Identifier, frame);

                frame.Dispose(); // just disposes internal variables, no need to worry
                frame = Options.FrameFactory.Create();

            }

            return endPosition;

        } else if (Options.Handshaker.Handshake(this, ref buffer, out var headerPosition)) {

            Logger.LogDebug("{Source} handshaked successfully", this);

            IsHandshaked = true;
            OnConnect?.Invoke();

            return headerPosition;

        } else if (buffer.Length > Options.MaxHandshakeSizeBytes) {

            throw new Exception($"Handshake not valid and exceeded {nameof(Options.MaxHandshakeSizeBytes)}.");

        }

        return buffer.End;

    }

    private async Task DoSend(CancellationToken token) {

        while(!token.IsCancellationRequested && DuplexPipe != null
            && await OutgoingFramesQueue.Reader.WaitToReadAsync(token)) {

            try {

                var frame = await OutgoingFramesQueue.Reader.ReadAsync(token);

                if (frame.Data.Length == 0) {
                    continue;
                }

                if (frame.IsRawOnly) {

                    await DuplexPipe.Output.WriteAsync(frame.Data, token); // flushes automatically

                } else {

                    SendFrame(frame);
                    await DuplexPipe.Output.FlushAsync(token); // not flushed inside frame

                }

            } catch (Exception) {

            }

        }

    }

    private void SendFrame(ITcpFrame frame) {

        int binarySize = frame.GetBinarySize();

        if (binarySize <= TcpConstants.SafeStackBufferSize) {

            Span<byte> span = stackalloc byte[binarySize];
            frame.Write(ref span);

            DuplexPipe!.Output.Write(span);

        } else {

            using var memoryOwner = MemoryPool<byte>.Shared.Rent(binarySize); // gives back memory at end of scope
            Span<byte> span = memoryOwner.Memory.Span[..binarySize]; // rent memory can be bigger, clamp it

            frame.Write(ref span);

            DuplexPipe!.Output.Write(span);

        }

    }

    private void InitFactory() {

        TcpUnderlyingConnectionType type = TcpUnderlyingConnectionType.NetworkStream;

        if (Options.ConnectionType != TcpUnderlyingConnectionType.Unset) {

            Logger.LogDebug("{Source} Underlying connection type overwritten, initialising with: {Type}", this, Options.ConnectionType);
            type = Options.ConnectionType;

        } else {

            if (Options.IsSecure) {
                type = TcpUnderlyingConnectionType.SslStream;
            } else {
                type = TcpUnderlyingConnectionType.FastSocket;
            }

            Logger.LogDebug("{Source} Underlying connection type chosen automatically: {Type}", this, type);

        }

        CreateFactory(type);

    }

    private void CreateFactory(TcpUnderlyingConnectionType type) {

        ConnectionType = type;

        switch (ConnectionType) {

            case TcpUnderlyingConnectionType.FastSocket:
                ConnectionFactory = new SocketConnectionFactory(Options.SocketConnectionOptions);
                break;

            case TcpUnderlyingConnectionType.SslStream:
                ConnectionFactory = new StreamConnectionFactory(Options.StreamConnectionOptions);
                break;

            case TcpUnderlyingConnectionType.NetworkStream:
                ConnectionFactory = new StreamConnectionFactory(Options.StreamConnectionOptions);
                break;

        }

    }

    private async Task<Stream?> GetStream(Socket socket) {

        NetworkStream stream = new(socket);

        if (ConnectionType == TcpUnderlyingConnectionType.NetworkStream) {
            return stream;
        }

        SslStream? sslStream = null;

        try {

            sslStream = new SslStream(stream, false);

            var task = sslStream.AuthenticateAsClientAsync(Options.Host!);
            await task.WaitAsync(TimeSpan.FromSeconds(60));

            return sslStream;

        } catch (Exception err) {

            sslStream?.Dispose();

            if (err is TimeoutException) {
                Logger.LogDebug("{Source} Ssl handshake timeouted.", this);
            }

            Logger.LogDebug("{Source} Certification fail get stream. {Error}", this, err);
            return null;

        }

    }

    public async ValueTask DisposeAsync() {

        ConnectionFactory?.Dispose();

        await Disconnect();

    }
}
