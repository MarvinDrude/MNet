
namespace MNet.Tcp.Options;

public sealed class TcpServerOptions : TcpOptions {

    /// <summary>
    /// Needed if secure is enabled
    /// </summary>
    public X509Certificate2? Certificate { get; set; } = null;

}
