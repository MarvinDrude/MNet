
namespace MNet.Tcp.Interfaces;

public interface ITcpServerHandshaker {

    /// <summary>
    /// Reads from the socket until handshake either successful or failed
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool Handshake(TcpServerConnection connection, ref ReadOnlySequence<byte> buffer, out SequencePosition position);

    /// <summary>
    /// if server has to send the first message for handshaking, do it in here
    /// </summary>
    public void StartHandshake(TcpServerConnection connection);

}
