
namespace MNet.Tcp.Interfaces;

public interface ITcpClientHandshaker {

    /// <summary>
    /// Reads from the socket until handshake either successful or failed
    /// </summary>
    /// <param name="client"></param>
    /// <param name="buffer"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool Handshake(TcpClient client, ref ReadOnlySequence<byte> buffer, out SequencePosition position);

    /// <summary>
    /// If handshake needs the client to send the first message, use this here and if its considered handshaked immediatly return true
    /// </summary>
    /// <param name="client"></param>
    /// <returns>true if considered handshaked already after start</returns>
    public bool StartHandshake(TcpClient client);

}
