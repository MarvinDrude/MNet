
namespace MNet.Sockets;

public interface ITransportConnection {

    public IDuplexPipe Transport { get; }

    public EndPoint? RemoteEndPoint { get; }

    public ValueTask<FlushResult> FlushAsync() {

        return Transport.Output.FlushAsync();

    }

    public void Start();

    public void Abort(Exception abortReason);

    public ValueTask DisposeAsync();

}
