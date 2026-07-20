using System;
using System.IO;
using System.Threading.Tasks;

namespace ExpressBus.Protocol;

public class FramableStreamer : IDisposable
{
    private readonly Stream _stream;

    public FramableStreamer(Stream stream)
    {
        _stream = stream;
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public async Task<FrameAndMessage> ReadAsync()
    {
        byte[] header = new byte[5];
        await _stream.ReadExactlyAsync(header);

        byte typeId = header[0];
        int byteCount = BitConverter.ToInt32(header, 1);

        var disposable = new DisposableMemory<byte>(byteCount);
        try
        {
            await _stream.ReadExactlyAsync(disposable.Memory);
            return new FrameAndMessage(new Frame(typeId, byteCount), disposable.Memory);
        }
        catch
        {
            disposable.Dispose();
            throw;
        }
    }

    public async Task WriteAsync<T>(T framable) where T : IFramable<T>
    {
        using var disposable = new DisposableMemory<byte>(5 + framable.ByteCount);
        var span = disposable.Memory.Span;

        span[0] = T.TypeId;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span.Slice(1), framable.ByteCount);

        framable.ToBytes(span.Slice(5));

        await _stream.WriteAsync(disposable.Memory);
    }
}
