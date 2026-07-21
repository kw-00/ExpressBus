using System;
using System.Buffers.Binary;
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
        var header = new DisposableMemory<byte>(5);
        await _stream.ReadExactlyAsync(header.Memory);

        byte typeId = header.Memory.Span[0];
        int byteCount = BinaryPrimitives.ReadInt32LittleEndian(header.Memory.Span[1..]);

        var message = new DisposableMemory<byte>(byteCount);
        try
        {
            await _stream.ReadExactlyAsync(message.Memory);
            return new FrameAndMessage(new Frame(typeId, byteCount), message.Memory);
        }
        catch
        {
            message.Dispose();
            throw;
        }
    }

    public async Task WriteAsync<T>(T framable) where T : IFramable<T>
    {
        using var buffer = new DisposableMemory<byte>(5 + framable.ByteCount);
        var span = buffer.Memory.Span;

        span[0] = T.TypeId;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(span[1..], framable.ByteCount);

        framable.ToBytes(span[5..]);

        await _stream.WriteAsync(buffer.Memory);
    }
}
