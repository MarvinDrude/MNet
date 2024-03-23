
namespace MNet.Ws;

public sealed class WsClientHandshaker : ITcpClientHandshaker {

    public bool Handshake(Tcp.TcpClient client, ref ReadOnlySequence<byte> buffer, out SequencePosition position) {

        var reader = new SequenceReader<byte>(buffer);

        if(!reader.TryReadTo(out ReadOnlySequence<byte> target, Delimiter.Span, true)) {
            position = buffer.Start;
            return false;
        }

        string content = Encoding.UTF8.GetString(target);
        if(content == null || !content.ToLowerInvariant().Contains("upgrade: websocket")) {
            position = buffer.Start;
            return false;
        }

        buffer = buffer.Slice(target.Length);
        position = buffer.Start;

        return false;

    }

    public bool StartHandshake(Tcp.TcpClient client) {

        client.Send(HeaderBytes);

        return false; // wait for response

    }

    private static readonly Memory<byte> Delimiter = "\r\n\r\n"u8.ToArray();

    private static Memory<byte> HeaderBytes => Encoding.UTF8.GetBytes(
            "GET / HTTP/1.1" + NewLine
            + $"Host: Host" + NewLine
            + $"Connection: upgrade" + NewLine
            + $"Pragma: no-cache" + NewLine
            + $"User-Agent: Mozilla/5.0 (None) Chrome" + NewLine
            + $"Upgrade: websocket" + NewLine
            + $"Origin: websocket" + NewLine
            + $"Sec-WebSocket-Version: 13" + NewLine
            + $"Accept-Encoding: gzip, deflate, br" + NewLine
            + $"Accept-Language: en,en-US;q=0.9" + NewLine
            + $"Sec-WebSocket-Key: {RandomUtils.CreateWebsocketBase64Key()}" + NewLine
            + $"Sec-WebSocket-Extensions: " + NewLine
            + NewLine);

    private static readonly string NewLine = "\r\n"; // HTTP Protocol

}
