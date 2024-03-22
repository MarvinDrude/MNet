
namespace MNet.Tcp;

public class TcpBase {

    public IPEndPoint EndPoint { get; protected set; } = default!;
    public Socket? Socket { get; protected set; }

    public ILogger Logger { get; protected set; } = default!;
    public TcpUnderlyingConnectionType ConnectionType { get; protected set; }

    protected CancellationTokenSource? RunTokenSource { get; set; }
    protected EventEmitter EventEmitter { get; set; } = default!;

    internal IConnectionFactory ConnectionFactory { get; set; } = default!;

}
