
namespace MNet.Tcp.Interfaces;

public interface ITcpFrame : IDisposable {

    public string? Identifier { get; set; }

    public Memory<byte> Data { get; set; }

    public SequencePosition Read(ref ReadOnlySequence<byte> buffer);

    public Memory<byte> GetMemory();

    public Span<byte> GetSpan();

}
