
namespace MNet.Internal;

internal sealed class SocketReceiver(PipeScheduler ioScheduler)
    : SocketAwaitableEventArgs(ioScheduler) {

    public ValueTask<SocketOperationResult> WaitForDataAsync(Socket socket) {

        SetBuffer(Memory<byte>.Empty);

        if (socket.ReceiveAsync(this)) {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));

    }

    public ValueTask<SocketOperationResult> ReceiveAsync(Socket socket, Memory<byte> buffer) {

        SetBuffer(buffer);

        if (socket.ReceiveAsync(this)) {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));

    }

}
