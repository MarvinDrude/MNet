
namespace MNet.Internal;

internal sealed class SocketSenderPool(PipeScheduler scheduler) : IDisposable {

    private const int MaxQueueSize = 1024 * 2;

    public readonly PipeScheduler Scheduler = scheduler;

    public void Dispose() {



    }

}
