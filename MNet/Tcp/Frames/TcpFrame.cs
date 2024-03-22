
namespace MNet.Tcp.Frames;

public sealed class TcpFrame : ITcpFrame {

    public string? Identifier { get; set; }

    public ReadOnlyMemory<byte> Data { get; set; }

    public bool IsRawOnly { get; set; } = false;

    public bool IsSending { get; set; } = false;

    public int GetBinarySize() {

        return Data.Length;

    }

    public void Write(ref Span<byte> buffer) {



    }

    public SequencePosition Read(ref ReadOnlySequence<byte> buffer) {
        throw new NotImplementedException();
    }

    public void Dispose() {
        throw new NotImplementedException();
    }

}
