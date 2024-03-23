
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

        return buffer.Start;

    }

    private int _Steps = 0;
    private int _LengthId;
    private long _LengthData;
    private string? _Identifier;

    private Memory<byte> _Data;
    private IMemoryOwner<byte>? _DataOwner;
    private IMemoryOwner<byte>? _DataOwnerTemp;

    public void Dispose() {

        _DataOwner?.Dispose();
        _DataOwnerTemp?.Dispose();

    }

}
