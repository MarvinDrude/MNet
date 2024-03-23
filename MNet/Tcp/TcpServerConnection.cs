
namespace MNet.Tcp;

public sealed class TcpServerConnection : IAsyncDisposable, ITcpSender {

    public required Socket Socket { get; init; }
    public required IDuplexPipe DuplexPipe { get; init; }
    public required TcpServer Server { get; init; }
    public required string UniqueId { get; set; }
    public Stream? Stream { get; init; }
    public bool IsHandshaked { get; set; } = false;

    public Channel<ITcpFrame> OutgoingFramesQueue { get; private set; } = Channel.CreateUnbounded<ITcpFrame>();

    private bool _Disposed = false;

    public void Send<T>(string identifier, T payload) where T : class {

        if(identifier.StartsWith(TcpConstants.StartSequenceSerialize)) {
            throw new ArgumentOutOfRangeException("Send identifier invalid.");
        }

        using var frame = Server.Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.Identifier = TcpConstants.StartSequenceSerialize + identifier;

        frame.IsRawOnly = false;
        frame.IsSending = true;
        
        frame.Data = Server.Options.Serializer.SerializeAsMemory(payload);
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    public void Send(string identifier, Memory<byte> payload) {

        if (identifier.StartsWith(TcpConstants.StartSequenceSerialize)) {
            throw new ArgumentOutOfRangeException("Send identifier invalid.");
        }

        using var frame = Server.Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.Identifier = identifier;

        frame.IsRawOnly = false;
        frame.IsSending = true;

        frame.Data = payload;
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    public void Send(Memory<byte> payload) {

        using var frame = Server.Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.IsRawOnly = true;
        frame.IsSending = true;

        frame.Data = payload;
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    void ITcpSender.Send(ITcpFrame frame) {

        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    public async ValueTask DisposeAsync() {

        if(_Disposed) {
            return;
        }

        try {

            if(DuplexPipe != null && DuplexPipe is SocketConnection socketConnection) {

                await socketConnection.DisposeAsync(); // does the socket dispose itself 

            } else {

                try {

                    Socket?.Shutdown(SocketShutdown.Both);

                } catch (Exception) {
                }

                Stream?.Dispose();
                Socket?.Dispose();

            }

        } catch(Exception) {
        }

        _Disposed = true;

    }

}
