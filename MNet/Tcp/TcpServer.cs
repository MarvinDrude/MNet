
namespace MNet.Tcp;

public class TcpServer {

    private readonly SockConnectionListener Listener;

    private readonly TcpServerOptions Options;

    public TcpServer(TcpServerOptions options) {

        if(!IPAddress.TryParse(options.Address, out var address)) {
            throw new ArgumentException($"{nameof(options.Address)} is not a valid IP address.");
        }

        Options = options;

        Listener = new SockConnectionListener(new IPEndPoint(address, options.Port), new SockConnectionFactoryOptions() {
            FinOnError = true,
        });

    }



}
