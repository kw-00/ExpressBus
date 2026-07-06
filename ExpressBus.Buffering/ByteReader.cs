using System.Buffers.Binary;

namespace ExpressBus.Buffering;

public ref struct ByteReader
{
	private int _index = 0;

	private readonly ReadOnlySpan<byte> Bytes { get; }

	public ByteReader(ReadOnlySpan<byte> bytes)
	{
		Bytes = bytes;
	}

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var readBytes = Bytes.Slice(_index, count);
        _index += count;
        return readBytes;
    }

    public ReadOnlySpan<byte> ReadRemaining()
    {
        var remainder = Bytes[_index..];
        _index = Bytes.Length;
        return remainder;
    }

    public byte ReadByte()
    {
        return Bytes[_index++];
    }

    public bool ReadBool()
    {
        return ReadByte() == 1;
    }

    public short ReadShort()
    {
        var result = BinaryPrimitives
			.ReadInt16LittleEndian(Bytes[_index..]);
        _index += 2;
        return result;
    }

    public int ReadInt()
    {
        var result = BinaryPrimitives
			.ReadInt32LittleEndian(Bytes[_index..]);
        _index += 4;
        return result;
    }

    public long ReadLong()
    {
        var result = BinaryPrimitives
			.ReadInt64LittleEndian(Bytes[_index..]);
        _index += 8;
        return result;
    }

    public float ReadFloat()
    {
        var raw = BinaryPrimitives
			.ReadInt32LittleEndian(Bytes[_index..]);
        _index += 4;
        return BitConverter.Int32BitsToSingle(raw);
    }

    public double ReadDouble()
    {
        var raw = BinaryPrimitives
			.ReadInt64LittleEndian(Bytes[_index..]);
        _index += 8;
        return BitConverter.Int64BitsToDouble(raw);
    }

    public Guid ReadGuid()
    {
        var bytes = Bytes.Slice(_index, 16);
        _index += 16;
        return new Guid(bytes);
    }
}
