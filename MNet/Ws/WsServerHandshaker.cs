
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace MNet.Ws;

public sealed partial class WsServerHandshaker : ITcpServerHandshaker {

    public bool Handshake(TcpServerConnection connection, ref ReadOnlySequence<byte> buffer, out SequencePosition position) {

        var reader = new SequenceReader<byte>(buffer);

        if (!reader.TryReadTo(out ReadOnlySequence<byte> target, Delimiter.Span, true)) {
            position = buffer.Start;
            return false;
        }

        string content = Encoding.UTF8.GetString(target);
        Regex getRegex = new Regex(@"^GET(.*)HTTP\/1\.1", RegexOptions.IgnoreCase);
        Match getRegexMatch = getRegex.Match(content);

        //TODO: get some headers

        if(getRegexMatch.Success) {

            var mem = GetHandshakeBody(content);
            connection.Send(mem);

            buffer = buffer.Slice(target.Length);
            position = buffer.Start;

            return true;

        }

        position = buffer.Start;
        return false;

    }

    public void StartHandshake(TcpServerConnection connection) {

        // server waits for client first request

    }

    private static readonly Memory<byte> Delimiter = "\r\n\r\n"u8.ToArray();

    private static Memory<byte> GetHandshakeBody(string data) {

        var bytes = Encoding.UTF8.GetBytes(WebSocketKeyRegex().Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
        var hash = Convert.ToBase64String(SHA1.HashData(bytes));

        return Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols" + NewLine
                + "Connection: Upgrade" + NewLine
                + "Upgrade: websocket" + NewLine
                + "Sec-Websocket-Accept: " + hash + NewLine
                + NewLine
            );

    }

    private static readonly string NewLine = "\r\n"; // HTTP Protocol

    [GeneratedRegex("Sec-WebSocket-Key: (.*)")]
    private static partial Regex WebSocketKeyRegex();
}
