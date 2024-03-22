
namespace MNet.Internal.Settings;

internal class ConnectionQueueSettings : IDisposable {

    public PipeOptions InputOptions { get; init; } = default!;

    public PipeOptions OutputOptions { get; init; } = default!;

    public MemoryPool<byte> MemoryPool { get; init; } = default!;

    private bool _AlreadyDisposed = false;

    public virtual void Dispose() {

        if(_AlreadyDisposed) {
            return;
        }

        MemoryPool?.Dispose();

        _AlreadyDisposed = true;

    }

}
