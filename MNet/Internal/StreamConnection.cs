
namespace MNet.Internal;

internal sealed class StreamConnection : IDuplexPipe {

    private static readonly int MinAllocBufferSize = PinnedBlockMemoryPool.BlockSize / 2;

    private readonly Pipe _ReadPipe;
    private readonly Pipe _WritePipe;

    private readonly Stream _Stream;

    public StreamConnection(Stream stream, PipeOptions? writeOptions, PipeOptions? readOptions) {

        _Stream = stream;

        writeOptions ??= PipeOptions.Default;
        readOptions ??= PipeOptions.Default;

        ArgumentOutOfRangeException.ThrowIfEqual(stream.CanWrite, false);
        ArgumentOutOfRangeException.ThrowIfEqual(stream.CanRead, false);

        _WritePipe = new Pipe(writeOptions);
        _ReadPipe = new Pipe(readOptions);

        writeOptions.WriterScheduler.Schedule(ob => ((StreamConnection)ob!).CopyWritePipeToStream().PipeFireAndForget(), this);
        readOptions.ReaderScheduler.Schedule(ob => ((StreamConnection)ob!).CopyStreamToReadPipe().PipeFireAndForget(), this);

    }

    public PipeWriter Output {
        get {
            return _WritePipe.Writer;
        }
    }

    public PipeReader Input {
        get {
            return _ReadPipe.Reader;
        }
    }

    private async Task CopyWritePipeToStream() {

        var reader = _WritePipe.Reader;

        try {

            while (true) {

                var pending = reader.ReadAsync();

                if (!pending.IsCompleted) { // nothing to do synchronously
                    await _Stream.FlushAsync(); // flush now then
                }

                var result = await pending;
                ReadOnlySequence<byte> buffer;

                do {

                    buffer = result.Buffer;

                    if (!buffer.IsEmpty) {
                        await SetBuffer(buffer);
                    }

                    reader.AdvanceTo(buffer.End);

                } while (!(buffer.IsEmpty && result.IsCompleted) && reader.TryRead(out result));

                if (buffer.IsEmpty && result.IsCompleted) {
                    break;
                }
                if (result.IsCanceled) {
                    break;
                }

            }

            try {

                reader.Complete(null);

            } finally { }

        } catch (Exception er) {

            try {

                reader.Complete(er);

            } finally { }

        }

    }

    private async Task CopyStreamToReadPipe() {

        Exception? error = null;
        var writer = _ReadPipe.Writer;

        try {

            while (true) {

                var memory = writer.GetMemory(MinAllocBufferSize);
                int read = await _Stream.ReadAsync(memory);

                if (read <= 0) {
                    break;
                }

                writer.Advance(read);

                var fres = await writer.FlushAsync();
                if (fres.IsCanceled || fres.IsCompleted) {
                    break;
                }

            }

        } catch (Exception er) {

            error = er;

        }

        writer.Complete(error);

    }

    private Task SetBuffer(in ReadOnlySequence<byte> data) {

        if (data.IsSingleSegment) {

            var vtask = _Stream.WriteAsync(data.First);
            return vtask.IsCompletedSuccessfully ? Task.CompletedTask : vtask.AsTask();

        } else {

            return SetBufferSegments(data);

        }

    }

    private async Task SetBufferSegments(ReadOnlySequence<byte> data) {

        foreach (var segment in data) {

            await _Stream.WriteAsync(segment);

        }

    }

}
