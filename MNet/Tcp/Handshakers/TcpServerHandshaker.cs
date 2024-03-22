
namespace MNet.Tcp.Handshakers;

public sealed class TcpServerHandshaker : ITcpServerHandshaker {

    public bool Handshake(TcpServerConnection connection, ref ReadOnlySequence<byte> buffer, out SequencePosition position) {

        position = buffer.Start;
        return true; // just make everyone handshaked without messages

    }

    public void StartHandshake(TcpServerConnection connection) {

        // no action required, everyone instantly handshaked by default

    }

}
