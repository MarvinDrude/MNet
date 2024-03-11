
namespace MNet.Sockets;

// taken from kestrel internal sockets
internal class SockAwaitableEventArgs(PipeScheduler ioScheduler)
        : SocketAsyncEventArgs(unsafeSuppressExecutionContextFlow: true), IValueTaskSource<SockOperationResult> {

    private static readonly Action<object?> ContinuationCompleted = _ => { };

    private readonly PipeScheduler IoScheduler = ioScheduler;

    private Action<object?>? Continuation;

    protected override void OnCompleted(SocketAsyncEventArgs _) {

        var c = Continuation;

        if (c != null || (c = Interlocked.CompareExchange(ref Continuation, ContinuationCompleted, null)) != null) {

            var continuationState = UserToken;
            UserToken = null;
            Continuation = ContinuationCompleted; // in case someone's polling IsCompleted

            IoScheduler.Schedule(c, continuationState);

        }

    }

    public SockOperationResult GetResult(short token) {

        Continuation = null;

        if (SocketError != SocketError.Success) {
            return new SockOperationResult(CreateException(SocketError));
        }

        return new SockOperationResult(BytesTransferred);

    }

    protected static SocketException CreateException(SocketError e) {

        return new SocketException((int)e);

    }

    public ValueTaskSourceStatus GetStatus(short token) {

        return !ReferenceEquals(Continuation, ContinuationCompleted) ? ValueTaskSourceStatus.Pending :
                SocketError == SocketError.Success ? ValueTaskSourceStatus.Succeeded :
                ValueTaskSourceStatus.Faulted;

    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) {

        UserToken = state;
        var prevContinuation = Interlocked.CompareExchange(ref Continuation, continuation, null);

        if (ReferenceEquals(prevContinuation, ContinuationCompleted)) {

            UserToken = null;
            ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);

        }

    }

}
