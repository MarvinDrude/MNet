

namespace MNet.Tcp;

public sealed class TcpClient : TcpBase, IAsyncDisposable, ITcpSender {

    public TcpClientOptions Options { get; private set; }

    public delegate void ConnectHandler();
    public event ConnectHandler? OnConnect;

    public delegate void DisconnectHandler();
    public event DisconnectHandler? OnDisconnect;

    private bool IsHandshaked { get; set; }
    private Channel<ITcpFrame> OutgoingFramesQueue { get; set; } = Channel.CreateUnbounded<ITcpFrame>();

    public TcpClient(TcpClientOptions options) {

        if (options.IsSecure) {
            ArgumentNullException.ThrowIfNull(options.Host);
        }

        if (!IPAddress.TryParse(options.Address, out var address)) {
            throw new ArgumentException($"{nameof(options.Address)} is not a valid IP address.");
        }

        Options = options;
        Logger = Options.Logger;



        InitFactory();

    }

    public void Send<T>(string identifier, T payload) where T : class {

        if (identifier.StartsWith(TcpConstants.StartSequenceSerialize)) {
            throw new ArgumentOutOfRangeException("Send identifier invalid.");
        }

        using var frame = Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.Identifier = TcpConstants.StartSequenceSerialize + identifier;

        frame.IsRawOnly = false;
        frame.IsSending = true;

        frame.Data = Options.Serializer.SerializeAsMemory(payload);
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    public void Send(string identifier, Memory<byte> payload) {

        if (identifier.StartsWith(TcpConstants.StartSequenceSerialize)) {
            throw new ArgumentOutOfRangeException("Send identifier invalid.");
        }

        using var frame = Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.Identifier = identifier;

        frame.IsRawOnly = false;
        frame.IsSending = true;

        frame.Data = payload;
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    /// <summary>
    /// Only use for handshaking
    /// </summary>
    /// <param name="payload"></param>
    public void Send(Memory<byte> payload) {

        using var frame = Options.FrameFactory.Create(); // dispose is ok here for sending

        frame.IsRawOnly = true;
        frame.IsSending = true;

        frame.Data = payload;
        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    void ITcpSender.Send(ITcpFrame frame) {

        OutgoingFramesQueue.Writer.TryWrite(frame);

    }

    private void InitFactory() {

        TcpUnderlyingConnectionType type = TcpUnderlyingConnectionType.NetworkStream;

        if (Options.ConnectionType != TcpUnderlyingConnectionType.Unset) {

            Logger.LogDebug("{Source} Underlying connection type overwritten, initialising with: {Type}", this, Options.ConnectionType);
            type = Options.ConnectionType;

        } else {

            if (Options.IsSecure) {
                type = TcpUnderlyingConnectionType.SslStream;
            } else {
                type = TcpUnderlyingConnectionType.FastSocket;
            }

            Logger.LogDebug("{Source} Underlying connection type chosen automatically: {Type}", this, type);

        }

        CreateFactory(type);

    }

    private void CreateFactory(TcpUnderlyingConnectionType type) {

        ConnectionType = type;

        switch (ConnectionType) {

            case TcpUnderlyingConnectionType.FastSocket:
                ConnectionFactory = new SocketConnectionFactory(Options.SocketConnectionOptions);
                break;

            case TcpUnderlyingConnectionType.SslStream:
                ConnectionFactory = new StreamConnectionFactory(Options.StreamConnectionOptions);
                break;

            case TcpUnderlyingConnectionType.NetworkStream:
                ConnectionFactory = new StreamConnectionFactory(Options.StreamConnectionOptions);
                break;

        }

    }

    public async ValueTask DisposeAsync() {



    }
}
