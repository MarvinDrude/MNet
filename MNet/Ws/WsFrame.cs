
namespace MNet.Ws;

public sealed class WsFrame : ITcpFrame {

    private TcpFrame InternalFrame { get; set; } = new TcpFrame();

    public WsOpcode Opcode { get; set; } = WsOpcode.BinaryFrame; //currently all messages treated as binary

    public string? Identifier { 
        get {
            return InternalFrame?.Identifier;
        }
        set {
            InternalFrame.Identifier = value;
        }
    }

    public ReadOnlyMemory<byte> Data {
        get {
            return InternalFrame.Data;
        }
        set {
            InternalFrame.Data = value;
        }
    }

    public bool IsRawOnly {
        get {
            return InternalFrame.IsRawOnly;
        }
        set {
            InternalFrame.IsRawOnly = value;
        }
    }

    public bool IsSending { 
        get {
            return InternalFrame.IsSending;
        }
        set {
            InternalFrame.IsSending = value;
        }
    }

    public int GetBinarySize() {

        var size = InternalFrame.GetBinarySize();
        return 1 + GetLengthByteSize(size) + size;

    }

    public void Write(ref Span<byte> buffer) {

        byte bitFin = 0x80;
        byte first = (byte)(bitFin | (byte)Opcode);

        buffer[0] = first;
        var size = InternalFrame.GetBinarySize();

        var sizeWrittenBytes = WriteLengthBytes(ref buffer, size);
        var rest = buffer[(1 + sizeWrittenBytes)..];

        InternalFrame.Write(ref rest);

    }

    public SequencePosition Read(ref ReadOnlySequence<byte> buffer) {

        bool bitFinSet = false;
        bool res = false;
        bool firstFragment = true;



        return buffer.Start;

    }

    private static int WriteLengthBytes(ref Span<byte> buffer, int size) {

        if(size <= 125) {

            buffer[1] = (byte)size;
            return 1;

        } else if(size <= 65535) {

            buffer[1] = 126;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(2, 2), (ushort)size);
            return 3;

        } else {

            buffer[1] = 127;
            BinaryPrimitives.WriteUInt64BigEndian(buffer.Slice(2, 8), (ulong)size);
            return 9;

        }

    }

    private static int GetLengthByteSize(int innerSize) {

        return innerSize switch {
            (<= 125) => 1,
            (> 125) and (<= 65535) => 3,
            _ => 9
        };

    }

    public void Dispose() {

        InternalFrame?.Dispose();

    }

}
