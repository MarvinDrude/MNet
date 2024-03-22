

namespace MNet.Tcp.Handshakers;

public sealed class TcpClientHandshaker : ITcpClientHandshaker {

    public bool Handshake(TcpClient client, ref ReadOnlySequence<byte> buffer, out SequencePosition position) {

        position = buffer.Start; // this method should never be called in default handshaker as start handshaking already true
        return true;

    }

    public bool StartHandshake(TcpClient client) {

        return true; // just be handshaked upfront

    }

}
