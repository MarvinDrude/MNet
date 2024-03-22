

namespace MNet.Tcp;

public sealed class TcpServerConnection : IAsyncDisposable, ITcpSender {

    public required Socket Socket { get; init; }
    public required IDuplexPipe DuplexPipe { get; init; }
    public required TcpServer Server { get; init; }
    public required string UniqueId { get; init; }
    public Stream? Stream { get; init; }

    public Channel<ITcpFrame> OutogingBytes { get; private set; } = Channel.CreateUnbounded<ITcpFrame>();


    public async ValueTask DisposeAsync() {



    }
}
