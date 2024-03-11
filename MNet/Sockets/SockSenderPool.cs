
namespace MNet.Sockets;

// taken from kestrel internal sockets
internal sealed class SockSenderPool(PipeScheduler scheduler) : IDisposable {

    private const int MaxQueueSize = 1024 * 2;

    public PipeScheduler Scheduler = scheduler;

    private readonly ConcurrentQueue<SockSender> Queue = new();
    private int Count;
    private bool Disposed;

    public SockSender Rent() {

        if (Queue.TryDequeue(out var sender)) {

            Interlocked.Decrement(ref Count);
            return sender;

        }

        return new SockSender(Scheduler);

    }

    public void Return(SockSender sender) {

        // This counting isn't accurate, but it's good enough for what we need to avoid using _queue.Count which could be expensive
        if (Disposed || Interlocked.Increment(ref Count) > MaxQueueSize) {

            Interlocked.Decrement(ref Count);
            sender.Dispose();

            return;

        }

        sender.Reset();
        Queue.Enqueue(sender);

    }

    public void Dispose() {

        if (!Disposed) {

            Disposed = true;

            while (Queue.TryDequeue(out var sender)) {
                sender.Dispose();
            }

        }

    }

}
