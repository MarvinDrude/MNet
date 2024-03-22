
namespace MNet.Tcp.Options;

public class TcpOptions {

    public required string Address { get; set; }

    public required ushort Port { get; set; }

    public bool IsSecure { get; set; } = false;

    public int MaxHandshakeSizeBytes { get; set; } = 1024 * 2;

    public SocketConnectionOptions SocketConnectionOptions { get; set; } = new();

    public StreamConnectionOptions StreamConnectionOptions { get; set; } = new();



    /// <summary>
    /// Only used for internal testing
    /// </summary>
    internal TcpUnderlyingConnectionType ConnectionType { get; set; } = TcpUnderlyingConnectionType.Unset;

}

public enum TcpUnderlyingConnectionType : byte {

    Unset = 0,
    NetworkStream = 1,
    SslStream = 2,
    FastSocket = 3

}