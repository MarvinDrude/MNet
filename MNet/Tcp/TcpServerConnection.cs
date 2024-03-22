

namespace MNet.Tcp;

public sealed class TcpServerConnection : IAsyncDisposable, ITcpSender {

    public required Socket Socket { get; init; }
    public required IDuplexPipe DuplexPipe { get; init; }
    public required TcpServer Server { get; init; }
    public required string UniqueId { get; init; }
    public Stream? Stream { get; init; }

    public Channel<ITcpFrame> OutgoingFramesQueue { get; private set; } = Channel.CreateUnbounded<ITcpFrame>();

    private bool _Disposed = false;

    public void Send<T>(string identifier, T payload) where T : class {
        throw new NotImplementedException();
    }

    public void Send(string identifier, Memory<byte> payload) {
        throw new NotImplementedException();
    }

    public void Send(Memory<byte> payload) {

        using var frame = Server.

    }

    void ITcpSender.Send(ITcpFrame frame) {

        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    public async ValueTask DisposeAsync() {

        if(_Disposed) {
            return;
        }



        _Disposed = true;

    }

}
