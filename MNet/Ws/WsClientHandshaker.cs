
namespace MNet.Ws;

public sealed class WsClientHandshaker : ITcpClientHandshaker {

    public bool Handshake(Tcp.TcpClient client, ref ReadOnlySequence<byte> buffer, out SequencePosition position) {

        var reader = new SequenceReader<byte>(buffer);



        position = buffer.Start;
        return false;

    }

    public bool StartHandshake(Tcp.TcpClient client) {

        client.Send(HeaderBytes);

        return false; // wait for response

    }

    private static readonly Memory<byte> HeaderBytes = Encoding.UTF8.GetBytes(
            "GET / HTTP/1.1" + Environment.NewLine
            + $"Host: Host" + Environment.NewLine
            + $"Connection: upgrade" + Environment.NewLine
            + $"Pragma: no-cache" + Environment.NewLine
            + $"User-Agent: Mozilla/5.0 (None) Chrome" + Environment.NewLine
            + $"Upgrade: websocket" + Environment.NewLine
            + $"Origin: websocket" + Environment.NewLine
            + $"Sec-WebSocket-Version: 13" + Environment.NewLine
            + $"Accept-Encoding: gzip, deflate, br" + Environment.NewLine
            + $"Accept-Language: en,en-US;q=0.9" + Environment.NewLine
            + $"Sec-WebSocket-Key: {RandomUtils.CreateWebsocketBase64Key()}" + Environment.NewLine //use same key for all client connections, maybe change?
            + $"Sec-WebSocket-Extensions: " + Environment.NewLine
            + Environment.NewLine);

}
