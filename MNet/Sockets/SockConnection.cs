
namespace MNet.Sockets;

// taken from kestrel internal sockets
internal sealed class SockConnection : ITransportConnection {

    private static readonly int MinAllocBufferSize = PinnedBlockMemoryPool.BlockSize / 2;

    private readonly Socket Socket;
    private readonly SockReceiver Receiver;
    private SockSender? Sender;
    private readonly SockSenderPool SenderPool;
    private readonly IDuplexPipe OgTransport;
    private readonly CancellationTokenSource ConnectionClosedTokenSource = new ();



}


