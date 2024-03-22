
namespace MNet.Tcp;

public sealed class TcpServer : TcpBase {

    public TcpServerOptions Options { get; private set; }

    public ILogger Logger { get; private set; }
    public TcpUnderlyingConnectionType ConnectionType { get; private set; }

    private IConnectionFactory ConnectionFactory { get; set; } = default!;

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

        InitFactory();
        
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

}
