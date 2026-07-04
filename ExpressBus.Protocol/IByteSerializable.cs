namespace ExpressBus.Protocol;

public interface IByteSerializable<TSelf>
{
	static abstract TSelf FromBytes(Memory<byte> buffer);
	ReadOnlyMemory<byte> ToBytes(Memory<byte> buffer);
}
