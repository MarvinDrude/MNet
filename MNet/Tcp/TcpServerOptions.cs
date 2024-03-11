
using System.Security.Cryptography.X509Certificates;

namespace MNet.Tcp;

public class TcpServerOptions {

    public required string Address { get; set; }

    public required ushort Port { get; set; }

    public X509Certificate2? Certificate { get; set; } = null;

    public bool SecureEnabled { get; set; } = false;

}
