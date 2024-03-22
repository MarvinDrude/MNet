
namespace MNet.Internal.Settings;

internal sealed class SocketConnectionQueueSettings : ConnectionQueueSettings {

    public PipeScheduler Scheduler { get; init; } = default!;

    public SocketSenderPool SocketSenderPool { get; init; } = default!;

}
