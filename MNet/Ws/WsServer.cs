
using MNet.Ws.Options;

namespace MNet.Ws;

public class WsServer : TcpServer {

    public WsServer(WsServerOptions options) 
        : base(options) {

    }

}
