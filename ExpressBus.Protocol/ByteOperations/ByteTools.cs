using System.Runtime.CompilerServices;

namespace ExpressBus.Protocol.ByteOperations;

public static class ByteTools
{
	public static int SizeOf<T>()
		where T : unmanaged, allows ref struct
	{
		return Unsafe.SizeOf<T>();
	}

	
	public static void EnsureLength(ReadOnlySpan<byte> bytes, int requiredLength)
	{
		var length = bytes.Length;
		if (length != requiredLength)
			throw new FormatException(
				$"Unexpected buffer length ({length} bytes)."
				+ $" Expected {requiredLength}."
			);
	}

	public static void EnsureLength(ReadOnlySpan<byte> bytes, int min, int? max)
	{
		var length = bytes.Length;
		if (length < min)
			throw new FormatException(
				$"Buffer is  unexpectedly short ({length} bytes)."
				+ $" Expected minimum: {min}."
			);
		if (max is not null && length > max)
			throw new FormatException(
				$"Buffer is unexpectedly long ({length} bytes)."
				+ $" Expected maximum: {max}."
			);
	}

	public static void EnsureLength<T>(
		ReadOnlySpan<byte> bytes, bool strict = false
	)
		where T : unmanaged, allows ref struct
	{
		var size = SizeOf<T>();
		if (strict) EnsureLength(bytes, size);
		else EnsureLength(bytes, size, null);
	}

	public static Span<byte> FitTo<T>(Span<byte> buffer)
		where T : unmanaged, allows ref struct
	{
		var size = SizeOf<T>();
		ByteTools.EnsureLength(buffer, size, null);
		return buffer.Slice(0, size);
	}

	
}
