using ExpressBus.Protocol.Sourcegen.TargetDependencies;

namespace ExpressBus.Protocol;

public interface IFramable<T> : IWithTypeId, ISerializable<T>
    where T : IFramable<T>, IWithTypeId, ISerializable<T>
{
}
