
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

        Logger.LogInformation("{Source} Server was stopped. {Endpoint}", this, EndPoint);

    }

    public async Task DoAccept() {



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

    public void Dispose() {

        ConnectionFactory?.Dispose();

    }

}
