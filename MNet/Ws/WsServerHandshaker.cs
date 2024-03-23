

namespace MNet.Ws;

public sealed class WsServerHandshaker : ITcpServerHandshaker {

    public bool Handshake(TcpServerConnection connection, ref ReadOnlySequence<byte> buffer, out SequencePosition position) {



        position = buffer.Start;
        return false;

    }

    public void StartHandshake(TcpServerConnection connection) {

        // server waits for client first request

    }

}
