
namespace MNet.Tcp.Interfaces;

public interface ITcpSerializer {

    public ReadOnlySpan<byte> SerializeAsSpan<T>(T target)
        where T : class;

    public ReadOnlyMemory<byte> SerializeAsMemory<T>(T target)
        where T : class;

    public T? Deserialize<T>(ReadOnlySpan<byte> source)
        where T : class;

}
