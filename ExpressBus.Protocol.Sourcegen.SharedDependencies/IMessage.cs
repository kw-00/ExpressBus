using System;

namespace ExpressBus.Protocol.Sourcegen.SharedDependencies;

public interface IMessage<T> : ISerializable<T> where T : IMessage<T>
{
    static abstract byte MessageTypeId { get; }
}
