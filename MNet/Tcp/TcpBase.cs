
namespace MNet.Tcp;

public class TcpBase {

    public IPEndPoint EndPoint { get; protected set; } = default!;
    public Socket? Socket { get; protected set; }

}
