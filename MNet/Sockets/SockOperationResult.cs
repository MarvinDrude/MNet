
namespace MNet.Sockets;

// taken from kestrel internal sockets
internal readonly struct SockOperationResult {

    public readonly SocketException? SocketError;

    public readonly int BytesTransferred;

    [MemberNotNullWhen(true, nameof(SocketError))]
    public readonly bool HasError => SocketError != null;

    public SockOperationResult(int bytesTransferred) {

        SocketError = null;
        BytesTransferred = bytesTransferred;

    }

    public SockOperationResult(SocketException exception) {

        SocketError = exception;
        BytesTransferred = 0;

    }

}
