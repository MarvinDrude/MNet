

namespace MNet.Ws;

public sealed class WsFrame : ITcpFrame {

    public string? Identifier { get; set; }
    public ReadOnlyMemory<byte> Data { get; set; }

    public bool IsRawOnly { get; set; }
    public bool IsSending { get; set; }

    public int GetBinarySize() {
        throw new NotImplementedException();
    }

    public SequencePosition Read(ref ReadOnlySequence<byte> buffer) {
        throw new NotImplementedException();
    }

    public void Write(ref Span<byte> buffer) {
        throw new NotImplementedException();
    }

    public void Dispose() {
        throw new NotImplementedException();
    }

}
