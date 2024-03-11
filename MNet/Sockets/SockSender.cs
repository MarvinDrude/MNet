
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MNet.Sockets;

// taken from kestrel internal sockets
internal sealed class SockSender(PipeScheduler ioScheduler)
    : SockAwaitableEventArgs(ioScheduler) {

    private List<ArraySegment<byte>>? _bufferList;

    public ValueTask<SockOperationResult> SendAsync(Socket socket, in ReadOnlySequence<byte> buffers) {

        if (buffers.IsSingleSegment) {
            return SendAsync(socket, buffers.First);
        }

        SetBufferList(buffers);

        if (socket.SendAsync(this)) {
            return new ValueTask<SockOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SockOperationResult>(new SockOperationResult(bytesTransferred))
            : new ValueTask<SockOperationResult>(new SockOperationResult(CreateException(error)));

    }

    public void Reset() {

        // We clear the buffer and buffer list before we put it back into the pool
        // it's a small performance hit but it removes the confusion when looking at dumps to see this still
        // holds onto the buffer when it's back in the pool
        if (BufferList != null) {

            BufferList = null;
            _bufferList?.Clear();

        } else {

            SetBuffer(null, 0, 0);

        }

    }

    private ValueTask<SockOperationResult> SendAsync(Socket socket, ReadOnlyMemory<byte> memory) {

        SetBuffer(MemoryMarshal.AsMemory(memory));

        if (socket.SendAsync(this)) {
            return new ValueTask<SockOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SockOperationResult>(new SockOperationResult(bytesTransferred))
            : new ValueTask<SockOperationResult>(new SockOperationResult(CreateException(error)));

    }
    private void SetBufferList(in ReadOnlySequence<byte> buffer) {

        Debug.Assert(!buffer.IsEmpty);
        Debug.Assert(!buffer.IsSingleSegment);

        if (_bufferList == null) {
            _bufferList = [];
        }

        foreach (var b in buffer) {
            _bufferList.Add(b.GetArray());
        }

        // The act of setting this list, sets the buffers in the internal buffer list
        BufferList = _bufferList;

    }

}
