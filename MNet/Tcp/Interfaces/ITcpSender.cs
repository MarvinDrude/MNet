
namespace MNet.Tcp.Interfaces;

internal interface ITcpSender {

    public void Send<T>(string identifier, T payload) where T : class;

    public void Send(string identifier, Memory<byte> payload);

    internal void Send(ITcpFrame frame);

}
