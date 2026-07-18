using System;

namespace ExpressBus.Protocol;

public interface ISerializable<T> where T : ISerializable<T>
{
    void ToBytes(Func<ReadOnlyMemory<byte>> bufferProducer);
    static abstract T FromBytes(ReadOnlyMemory<byte> bytes);
}
