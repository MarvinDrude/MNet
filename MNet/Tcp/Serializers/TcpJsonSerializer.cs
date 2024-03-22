
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MNet.Tcp.Serializers;

public sealed class TcpJsonSerializer : ITcpSerializer {

    private static readonly JsonSerializerOptions JsonOptions = new() {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonWriterOptions WriterOptions = new() {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly int InitialBufferSize = 1024 * 2;

    public T? Deserialize<T>(ReadOnlySpan<byte> source) where T : class {

        return JsonSerializer.Deserialize<T>(source, JsonOptions);

    }

    public ReadOnlyMemory<byte> SerializeAsMemory<T>(T target) where T : class {

        ArrayBufferWriter<byte> buffer = new(InitialBufferSize);
        FillWriterBuffer(buffer, target);

        return buffer.WrittenMemory;

    }

    public ReadOnlySpan<byte> SerializeAsSpan<T>(T target) where T : class {

        ArrayBufferWriter<byte> buffer = new(InitialBufferSize);
        FillWriterBuffer(buffer, target);

        return buffer.WrittenSpan;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FillWriterBuffer<T>(ArrayBufferWriter<byte> buffer, T target) {

        using Utf8JsonWriter writer = new(buffer, WriterOptions);
        JsonSerializer.Serialize(writer, target, JsonOptions);

    }

}
