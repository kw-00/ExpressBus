using System;

namespace ExpressBus.Protocol;

public interface ISerializable<T> where T : ISerializable<T>
{
    int ByteCount { get; }
    void ToBytes(Span<byte> buffer);
    static abstract T FromBytes(Span<byte> buffer);
}
