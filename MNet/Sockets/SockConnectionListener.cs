
using System.Diagnostics;

namespace MNet.Sockets;

// taken from kestrel internal sockets
internal sealed class SockConnectionListener(EndPoint endpoint, SockConnectionFactoryOptions options) {

    private readonly SockConnectionContextFactory Factory = new (options);

    private readonly SockConnectionFactoryOptions Options = options;

    private Socket? ListenSocket;

    public EndPoint EndPoint { get; private set; } = endpoint;

    public void Bind() {

        if (ListenSocket != null) {
            throw new InvalidOperationException("Socket already listen bound.");
        }

        Socket listenSocket;
        try {

            listenSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            listenSocket.NoDelay = true;

            listenSocket.Bind(EndPoint);

        } catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse) {
            throw new Exception(e.Message, e);
        }

        Debug.Assert(listenSocket.LocalEndPoint != null);
        EndPoint = listenSocket.LocalEndPoint;

        listenSocket.Listen();
        ListenSocket = listenSocket;

    }

    public async ValueTask<SockConnection?> AcceptAsync(CancellationToken cancellationToken = default) {

        while (true) {

            try {

                Debug.Assert(ListenSocket != null, "Bind must be called first.");

                var acceptSocket = await ListenSocket.AcceptAsync(cancellationToken);

                // Only apply no delay to Tcp based endpoints
                if (acceptSocket.LocalEndPoint is IPEndPoint) {
                    acceptSocket.NoDelay = true;
                }

                return Factory.Create(acceptSocket);

            } catch (ObjectDisposedException) {

                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;

            } catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted) {

                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;

            } catch (SocketException) {

                // The connection got reset while it was in the backlog, so we try again.

            }

        }

    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default) {

        ListenSocket?.Dispose();
        return default;

    }

    public ValueTask DisposeAsync() {

        ListenSocket?.Dispose();
        Factory.Dispose();

        return default;

    }

}
