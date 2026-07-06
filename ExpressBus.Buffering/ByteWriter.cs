using System.Buffers.Binary;

namespace ExpressBus.Buffering;

public ref struct ByteWriter
{
	private int _index = 0;
	private Span<byte> Bytes { get; }

	public ByteWriter(Span<byte> bytes)
	{
		Bytes = bytes;
	}

	public void WriteBytes(ReadOnlySpan<byte> bytes)
	{
		bytes.CopyTo(Bytes[_index..]);
		_index += bytes.Length;
	}

	public void WriteByte(byte value)
	{
		Bytes[_index++] = value;
	}

	public void WriteBool(bool value)
	{
		WriteByte(value ? (byte)1 : (byte)0);
	}

	public void WriteShort(short value)
	{
		BinaryPrimitives.WriteInt16LittleEndian(
			Bytes[_index..],
			value
		);
		_index += 2;
	}

	public void WriteInt(int value)
	{
		BinaryPrimitives.WriteInt32LittleEndian(
			Bytes[_index..],
			value
		);
		_index += 4;
	}

	public void WriteLong(long value)
	{
		BinaryPrimitives.WriteInt64LittleEndian(
			Bytes[_index..],
			value
		);
		_index += 8;
	}

	public void WriteFloat(float value)
	{
		BinaryPrimitives.WriteInt32LittleEndian(
			Bytes[_index..],
			BitConverter.SingleToInt32Bits(value)
		);
		_index += 4;
	}

	public void WriteDouble(double value)
	{
		BinaryPrimitives.WriteInt64LittleEndian(
			Bytes[_index..],
			BitConverter.DoubleToInt64Bits(value)
		);
		_index += 8;
	}

	public void WriteGuid(Guid value)
	{
		value.TryWriteBytes(
			Bytes[_index..]
		);
		_index += 16;
	}
}
