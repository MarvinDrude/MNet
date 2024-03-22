
namespace MNet.Tcp;

public sealed class TcpServer : TcpBase {

    public TcpServerOptions Options { get; private set; }

    public TcpServer(TcpServerOptions options) {

        if(options.IsSecure) {
            ArgumentNullException.ThrowIfNull(options.Certificate, nameof(options.Certificate));
        }

        if (!IPAddress.TryParse(options.Address, out var address)) {
            throw new ArgumentException($"{nameof(options.Address)} is not a valid IP address.");
        }

        Options = options;
        EndPoint = new IPEndPoint(address, options.Port);
        
    }

}
