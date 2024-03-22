
namespace MNet.Tcp.Options;

public abstract class ConnectionOptions {

    /// <summary>
    /// Defaults to processor count but max 16
    /// </summary>
    public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

    /// <summary>
    /// Max size of unread buffer
    /// </summary>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Max outgoing buffer size before applying write backpressure
    /// </summary>
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    internal abstract ConnectionQueueSettings CreateQueueSettings();

}
