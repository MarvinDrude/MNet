
namespace MNet.Internal.Settings;

internal sealed class SocketConnectionQueueSettings : ConnectionQueueSettings {

    public PipeScheduler Scheduler { get; init; } = default!;

    public SocketSenderPool SocketSenderPool { get; init; } = default!;

    private bool _AlreadyDisposed = false;

    public override void Dispose() {

        if(_AlreadyDisposed) {
            return;
        }

        SocketSenderPool?.Dispose();
        base.Dispose();

        _AlreadyDisposed = true;

    }

}
