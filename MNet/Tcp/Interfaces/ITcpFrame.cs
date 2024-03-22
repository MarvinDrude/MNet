
namespace MNet.Tcp.Interfaces;

public interface ITcpFrame : IDisposable {

    public string? Identifier { get; set; }

    public Memory<byte> Data { get; set; }

    /// <summary>
    /// When sending and this is true, only <see cref="Data"/> is sent raw.
    /// </summary>
    public bool IsRawOnly { get; set; }

    /// <summary>
    /// While reading a incomming frame, the moment you set the <see cref="Identifier"/> something other than <see langword="null"/>, the frame will be 
    /// queued for consumption and a new reading starts from 0
    /// </summary>
    public SequencePosition Read(ref ReadOnlySequence<byte> buffer);

    public Memory<byte> GetMemory();

    public Span<byte> GetSpan();

}
