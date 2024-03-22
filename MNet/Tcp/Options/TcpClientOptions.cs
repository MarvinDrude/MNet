
namespace MNet.Tcp.Options;

public sealed class TcpClientOptions : TcpOptions {

    /// <summary>
    /// Needed for secure authentication
    /// </summary>
    public string? Host {  get; init; }

    /// <summary>
    /// Default interval is 3 seconds
    /// </summary>
    public TimeSpan ReconnectInterval { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Default handshaker just lets anyone connect and send valid messages
    /// </summary>
    public ITcpClientHandshaker Handshaker { get; init; } = new TcpClientHandshaker();

}
