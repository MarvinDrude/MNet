
using MNet.Ws.Options;
using TcpClient = MNet.Tcp.TcpClient;

namespace MNet.Ws;

public class WsClient : TcpClient {

    public WsClient(WsClientOptions options) 
        : base(options) {

    }

}
