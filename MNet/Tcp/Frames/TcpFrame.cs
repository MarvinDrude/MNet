
namespace MNet.Tcp.Frames;

public sealed class TcpFrame : ITcpFrame {

    public string? Identifier { get; set; }

    public Memory<byte> Data { get; set; }

    public bool IsRawOnly { get; set; } = false;

    public Memory<byte> GetMemory() {
        throw new NotImplementedException();
    }

    public Span<byte> GetSpan() {
        throw new NotImplementedException();
    }

    public SequencePosition Read(ref ReadOnlySequence<byte> buffer) {
        throw new NotImplementedException();
    }

    public void Dispose() {
        throw new NotImplementedException();
    }

}
