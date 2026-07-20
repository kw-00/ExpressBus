using ExpressBus.Protocol.Sourcegen.SharedDependencies;

namespace ExpressBus.Protocol;

public interface IFramable<T> : IWithTypeId, ISerializable<T>
    where T : IFramable<T>, IWithTypeId, ISerializable<T>
{
}
