
namespace MNet.Internal;

internal sealed class SocketConnection : IDuplexPipe {

    public PipeReader Input {
        get {
            return _Input;
        }
    }

    public PipeWriter Output {
        get {
            return _Output;
        }
    }

    private readonly PipeReader _Input;
    private readonly PipeWriter _Output;

    private readonly Pipe _SendSocket;
    private readonly Pipe _ReceiveSocket;

    private readonly Socket _Socket;

    public SocketConnection(Socket socket,
        MemoryPool<byte> memoryPool, PipeScheduler scheduler,
        SocketSenderPool senderPool, PipeOptions sendOptions,
        PipeOptions receiveOptions) {

        _Socket = socket;

        _SendSocket = new Pipe(sendOptions);
        _ReceiveSocket = new Pipe(receiveOptions);

        _Input = _ReceiveSocket.Reader;
        _Output = _SendSocket.Writer;

    }

}
