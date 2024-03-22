
using System.Security.Authentication;

namespace MNet.Tcp;

public sealed class TcpServer : TcpBase, IDisposable {

    public TcpServerOptions Options { get; private set; }

    public ILogger Logger { get; private set; }
    public TcpUnderlyingConnectionType ConnectionType { get; private set; }

    public int ConnectionCount {
        get {
            return _Connections.Count;
        }
    }

    public delegate void ConnectHandler(TcpServerConnection connection);
    public event ConnectHandler? OnConnect;

    public delegate void DisconnectHandler(TcpServerConnection connection);
    public event DisconnectHandler? OnDisconnect;

    private IConnectionFactory ConnectionFactory { get; set; } = default!;
    private CancellationTokenSource? RunTokenSource { get; set; }

    private readonly ConcurrentDictionary<string, TcpServerConnection> _Connections;

    public TcpServer(TcpServerOptions options) {

        if(options.IsSecure) {
            ArgumentNullException.ThrowIfNull(options.Certificate, nameof(options.Certificate));
        }

        if (!IPAddress.TryParse(options.Address, out var address)) {
            throw new ArgumentException($"{nameof(options.Address)} is not a valid IP address.");
        }

        Options = options;
        EndPoint = new IPEndPoint(address, options.Port);

        Logger = Options.Logger;
        _Connections = new ConcurrentDictionary<string, TcpServerConnection>();

        InitFactory();
        
    }

    public void Start() {

        if(RunTokenSource != null) {
            return;
        }

        Logger.LogDebug("{Source} Starting the tcp server...", this);

        RunTokenSource = new CancellationTokenSource();
        BindSocket();

        var _ = DoAccept(RunTokenSource.Token);

        Logger.LogInformation("{Source} Server was started. {Endpoint}", this, EndPoint);

    }

    public void Stop() {

        if(RunTokenSource == null) {
            return;
        }

        Logger.LogDebug("{Source} Stopping the tcp server...", this);

        _Connections.Clear();

        RunTokenSource?.Cancel();
        RunTokenSource?.Dispose();

        RunTokenSource = null;

        try {

            Socket?.Shutdown(SocketShutdown.Both);

        } catch(Exception) {

        }

        Socket?.Dispose();
        Socket = null;

        Logger.LogInformation("{Source} Server was stopped. {Endpoint}", this, EndPoint);

    }

    private async Task DoAccept(CancellationToken token) {

        while(!token.IsCancellationRequested) {

            var connection = await Accept(token);
            if (connection == null) return;

            var _ = DoReceive(connection, token);
            _ = DoSend(connection, token);

        }

    }

    private async ValueTask<TcpServerConnection?> Accept(CancellationToken token = default) {

        while(!token.IsCancellationRequested && Socket != null) {

            try {

                var socket = await Socket.AcceptAsync(token);
                socket.NoDelay = true;

                if(ConnectionType == TcpUnderlyingConnectionType.FastSocket) {

                    return new TcpServerConnection() {
                        DuplexPipe = ConnectionFactory.Create(socket, null),
                        Server = this,
                        Socket = socket,
                        UniqueId = RandomUtils.RandomString(TcpConstants.UniqueIdLength),
                    };

                } else {

                    var stream = await GetStream(socket);
                    if(stream == null) {
                        continue;
                    }

                    return new TcpServerConnection() {
                        DuplexPipe = ConnectionFactory.Create(socket, stream),
                        Server = this,
                        Socket = socket,
                        UniqueId = RandomUtils.RandomString(TcpConstants.UniqueIdLength),
                    };

                }

            } catch (ObjectDisposedException) {

                return null;

            } catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted) {

                return null;

            } catch (Exception) {

                // The connection got reset while it was in the backlog, so we try again.

            }

        }

        return null;

    }



    private async Task DoReceive(TcpServerConnection connection, CancellationToken token) {

        var frame = Options.FrameFactory.Create();

        try {

            Options.Handshaker.StartHandshake(connection);

            while(!token.IsCancellationRequested) {



            }

        } catch(Exception err) {

            Logger.LogDebug("{Source} DoReceive error on connection {UniqueId}, {Error}", this, connection.UniqueId, err);

        } finally {

            // disconnection

            if (connection.UniqueId != null) {
                _Connections.TryRemove(connection.UniqueId, out _);
            }

            frame?.Dispose();
            await connection.DisposeAsync();

            OnDisconnect?.Invoke(connection);

        }

    }

    private async Task DoSend(TcpServerConnection connection, CancellationToken token) {

        while(!token.IsCancellationRequested && await connection.OutgoingFramesQueue.Reader.WaitToReadAsync()) {

            try {

                var frame = await connection.OutgoingFramesQueue.Reader.ReadAsync(token);

                if (frame.Data.Length == 0) {
                    continue;
                }

                if (frame.IsRawOnly) {

                    await connection.DuplexPipe.Output.WriteAsync(frame.Data, token); // flushes automatically

                } else {

                    SendFrame(connection, frame);
                    await connection.DuplexPipe.Output.FlushAsync(token); // not flushed inside frame

                }

            } catch(Exception) {

            }

        }

    }

    private void SendFrame(TcpServerConnection connection, ITcpFrame frame) {

        int binarySize = frame.GetBinarySize();

        if(binarySize <= TcpConstants.SafeStackBufferSize) {

            Span<byte> span = stackalloc byte[binarySize];
            frame.Write(ref span);

            connection.DuplexPipe.Output.Write(span);

        } else {

            using var memoryOwner = MemoryPool<byte>.Shared.Rent(binarySize); // gives back memory at end of scope
            Span<byte> span = memoryOwner.Memory.Span[..binarySize]; // rent memory can be bigger, clamp it

            frame.Write(ref span);

            connection.DuplexPipe.Output.Write(span);

        }

    }

    private void BindSocket() {

        Socket listenSocket;
        try {

            listenSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            listenSocket.NoDelay = true;
            
            listenSocket.Bind(EndPoint);

            Logger.LogDebug("{Source} Binding to following endpoint {Endpoint}", this, EndPoint);

        } catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse) {

            throw new Exception(e.Message, e);

        }

        Socket = listenSocket;
        listenSocket.Listen();

    }

    private void InitFactory() {

        TcpUnderlyingConnectionType type = TcpUnderlyingConnectionType.NetworkStream;

        if(Options.ConnectionType != TcpUnderlyingConnectionType.Unset) {

            Logger.LogDebug("{Source} Underlying connection type overwritten, initialising with: {Type}", this, Options.ConnectionType);
            type = Options.ConnectionType;

        } else {

            if(Options.IsSecure) {
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

        switch(ConnectionType) {

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

        if(ConnectionType == TcpUnderlyingConnectionType.NetworkStream) {
            return stream;
        }

        SslStream? sslStream = null;

        try {

            sslStream = new SslStream(stream, false);

            var task = sslStream.AuthenticateAsServerAsync(Options.Certificate!, false, SslProtocols.None, true);
            await task.WaitAsync(TimeSpan.FromSeconds(60));

            return sslStream;

        } catch(Exception err) {

            sslStream?.Dispose();

            if(err is TimeoutException) {
                Logger.LogDebug("{Source} Ssl handshake timeouted.", this);
            }

            Logger.LogDebug("{Source} Certification fail get stream. {Error}", this, err);
            return null;

        }

    }

    public void Dispose() {

        ConnectionFactory?.Dispose();

    }

}
