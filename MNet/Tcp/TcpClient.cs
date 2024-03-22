
namespace MNet.Tcp;

public sealed class TcpClient : TcpBase {

    public TcpClientOptions Options { get; private set; }

    public ILogger Logger { get; private set; }
    public TcpUnderlyingConnectionType ConnectionType { get; private set; }

    public delegate void ConnectHandler();
    public event ConnectHandler? OnConnect;

    public delegate void DisconnectHandler();
    public event DisconnectHandler? OnDisconnect;



}
