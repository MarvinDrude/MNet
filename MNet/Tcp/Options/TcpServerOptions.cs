
namespace MNet.Tcp.Options;

public class TcpServerOptions : TcpOptions {

    /// <summary>
    /// Needed if secure is enabled
    /// </summary>
    public X509Certificate2? Certificate { get; init; } = null;

    /// <summary>
    /// Default handshaker just lets anyone connect and send valid messages
    /// </summary>
    public ITcpServerHandshaker Handshaker { get; init; } = new TcpServerHandshaker();

}
