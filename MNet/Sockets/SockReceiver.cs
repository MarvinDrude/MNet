
namespace MNet.Sockets;

// taken from kestrel internal sockets
internal sealed class SockReceiver(PipeScheduler ioScheduler) 
    : SockAwaitableEventArgs(ioScheduler) {

    public ValueTask<SockOperationResult> WaitForDataAsync(Socket socket) {

        SetBuffer(Memory<byte>.Empty);

        if (socket.ReceiveAsync(this)) {
            return new ValueTask<SockOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SockOperationResult>(new SockOperationResult(bytesTransferred))
            : new ValueTask<SockOperationResult>(new SockOperationResult(CreateException(error)));

    }

    public ValueTask<SockOperationResult> ReceiveAsync(Socket socket, Memory<byte> buffer) {

        SetBuffer(buffer);

        if (socket.ReceiveAsync(this)) {
            return new ValueTask<SockOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SockOperationResult>(new SockOperationResult(bytesTransferred))
            : new ValueTask<SockOperationResult>(new SockOperationResult(CreateException(error)));

    }

}
