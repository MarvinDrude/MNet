
namespace MNet.Tcp;

public class TcpBase {

    public IPEndPoint? EndPoint { get; protected set; }
    public Socket? Socket { get; protected set; }

}
