
namespace MNet.Ws;

public sealed class WsFrameFactory : ITcpFrameFactory {

    public ITcpFrame Create() {
        return new WsFrame();
    }

}
