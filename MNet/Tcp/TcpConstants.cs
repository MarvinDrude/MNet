
namespace MNet.Tcp;

public static class TcpConstants {

    public const int UniqueIdLength = 9;

    public const int SafeStackBufferSize = 1024;

    public const int MaxFrameIdentifierLength = 512;

    public const int MaxFrameDataLength = 1024 * 1024 * 50;

    public const string StartSequenceSerialize = "#_#serialize#_#";

}
