
namespace MNet.Tcp.Frames;

public sealed class TcpFrameFactory : ITcpFrameFactory {

    public ITcpFrame Create() {
        return new TcpFrame();
    }

}
