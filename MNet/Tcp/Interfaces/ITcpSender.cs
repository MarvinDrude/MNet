
namespace MNet.Tcp.Interfaces;

internal interface ITcpSender {

    public void Send<T>(string identifier, T payload) where T : class;

    public void Send(string identifier, Memory<byte> payload);

    /// <summary>
    /// Should only be used for handshaking
    /// </summary>
    /// <param name="payload"></param>
    public void Send(Memory<byte> payload);

    internal void Send(ITcpFrame frame);

}
