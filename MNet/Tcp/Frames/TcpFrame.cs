
namespace MNet.Tcp.Frames;

public sealed class TcpFrame : ITcpFrame {

    private const int IdentifierBufferSize = 4;
    private const int DataBufferSize = 4;

    public string? Identifier { get; set; }
    public ReadOnlyMemory<byte> Data { get; set; }

    public bool IsRawOnly { get; set; } = false;
    public bool IsSending { get; set; } = false;

    public int GetBinarySize() {

        return Encoding.UTF8.GetByteCount(Identifier!) + Data.Length + IdentifierBufferSize + DataBufferSize;

    }

    public void Write(ref Span<byte> buffer) {

        if (Identifier == null) return;

        Span<byte> dataId = Encoding.UTF8.GetBytes(Identifier).AsSpan(); //TODO: could be faster
        int dataIdLength = dataId.Length;
        
        BinaryPrimitives.WriteInt32BigEndian(buffer[..4], dataIdLength);
        dataId[0..dataIdLength].CopyTo(buffer.Slice(4, dataIdLength));

        BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4 + dataIdLength, 4), Data.Length);

        Data.Span.CopyTo(buffer.Slice(4 + 4 + dataIdLength, Data.Length));

    }

    public SequencePosition Read(ref ReadOnlySequence<byte> buffer) {

        var reader = new SequenceReader<byte>(buffer);
        var position = buffer.Start;

        if (_Steps == 0) {
            if (!ReadLengthIdBuffer(ref reader, ref buffer, out position, out _LengthId)) {
                return position;
            }
            if (_LengthId > TcpConstants.MaxFrameIdentifierLength) {
                throw new ArgumentOutOfRangeException(nameof(_LengthId));
            }
            _Steps = 1;
        }

        if (_Steps == 1) {
            if (!ReadIdString(ref reader, ref buffer, out position, out _Identifier)) {
                return position;
            }
            _Steps = 2;
        }

        if (_Steps == 2) {
            if (!ReadLengthDataBuffer(ref reader, ref buffer, out position, out _LengthData)) {
                return position;
            }
            _Steps = 3;
        }

        if (_Steps == 3) {
            if (!ReadData(ref reader, ref buffer, out position)) {
                return position;
            }
            _Steps = 4;
        }

        Identifier = _Identifier;

        return position;

    }


    private bool ReadData(ref SequenceReader<byte> reader, ref ReadOnlySequence<byte> buffer, out SequencePosition position) {

        int targetSize = (int)Math.Min(reader.Remaining, _LengthData); // remaining or buffer.length

        if (_Data.IsEmpty) {

            bool earlyExit = targetSize == _LengthData; // everything already here, no batching

            if (earlyExit) {
                Data = _Data = new byte[_LengthData];
            } else {
                _DataOwner = MemoryPool<byte>.Shared.Rent(targetSize);
                MemoryMarshal.AsBytes(_DataOwner.Memory.Span).Clear();

                _Data = _DataOwner.Memory[..targetSize];
            }

            reader.TryCopyTo(_Data.Span);
            buffer = buffer.Slice(_Data.Length);

            position = buffer.Start;
            _LengthData -= targetSize;

            return earlyExit;

        } else {

            bool finished = targetSize == _LengthData;

            if (finished) {

                Memory<byte> tempResult = new byte[targetSize + _Data.Length];

                _Data.CopyTo(tempResult.Slice(0, _Data.Length));

                var slice = buffer.Slice(0, targetSize);
                slice.CopyTo(tempResult.Span.Slice(_Data.Length, targetSize));

                Data = tempResult;
                _Data = null;

                _DataOwner?.Dispose();
                _DataOwner = null;

            } else {

                _DataOwnerTemp = MemoryPool<byte>.Shared.Rent(targetSize + _Data.Length);
                MemoryMarshal.AsBytes(_DataOwnerTemp.Memory.Span).Clear();

                var memory = _DataOwnerTemp.Memory.Slice(0, targetSize + _Data.Length);

                _Data.CopyTo(memory.Slice(0, _Data.Length));

                _DataOwner?.Dispose();
                _DataOwner = null;

                _DataOwner = _DataOwnerTemp;
                _DataOwnerTemp = null;

                var tempBuffer = buffer.Slice(0, targetSize);
                var tempTarget = memory.Span.Slice(_Data.Length, targetSize);

                _Data = memory;
                _LengthData -= targetSize;

                tempBuffer.CopyTo(tempTarget);

            }

            buffer = buffer.Slice(targetSize);
            position = buffer.Start;

            return finished;

        }

    }

    private bool ReadIdString(ref SequenceReader<byte> reader, ref ReadOnlySequence<byte> buffer, out SequencePosition position, out string? identifier) {

        Span<byte> span = stackalloc byte[_LengthId];

        if (!reader.TryCopyTo(span)) {

            identifier = null;
            position = buffer.Start;

            return false;

        }

        reader.Advance(span.Length);
        buffer = buffer.Slice(span.Length);

        identifier = Encoding.UTF8.GetString(span);
        position = buffer.Start;

        return true;

    }

    private bool ReadLengthDataBuffer(ref SequenceReader<byte> reader, ref ReadOnlySequence<byte> buffer, out SequencePosition position, out int length) {

        Span<byte> span = stackalloc byte[4];

        if (!reader.TryCopyTo(span)) {

            length = 0;
            position = buffer.Start;

            return false;

        }

        reader.Advance(span.Length);
        buffer = buffer.Slice(span.Length);

        position = buffer.Start;
        length = BinaryPrimitives.ReadInt32BigEndian(span);

        return true;

    }

    private bool ReadLengthIdBuffer(ref SequenceReader<byte> reader, ref ReadOnlySequence<byte> buffer, out SequencePosition position, out int length) {

        Span<byte> span = stackalloc byte[4];

        if (!reader.TryCopyTo(span)) {

            length = 0;
            position = buffer.Start;

            return false;

        }

        reader.Advance(span.Length);
        buffer = buffer.Slice(span.Length);

        position = buffer.Start;
        length = BinaryPrimitives.ReadInt32BigEndian(span);

        return true;

    }

    private int _Steps = 0;
    private int _LengthId;
    private int _LengthData;
    private string? _Identifier;

    private Memory<byte> _Data;
    private IMemoryOwner<byte>? _DataOwner;
    private IMemoryOwner<byte>? _DataOwnerTemp;

    public void Dispose() {

        _DataOwner?.Dispose();
        _DataOwnerTemp?.Dispose();

    }

}
