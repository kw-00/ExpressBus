using System.Runtime.CompilerServices;

namespace ExpressBus.Buffering;

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

	// --- Async Stream Helpers ---

	/// <summary>
	/// Reads exactly one byte from the stream.
	/// </summary>
	public static async Task<byte> ReadSingleByteAsync(this Stream stream)
	{
		var buffer = new byte[1];
		var bytesRead = await stream.ReadAsync(buffer, 0, 1).ConfigureAwait(false);
		if (bytesRead == 0)
			throw new InvalidDataException("Unexpected end of stream while reading single byte.");
		return buffer[0];
	}

	/// <summary>
	/// Reads exactly the specified number of bytes into the provided buffer.
	/// </summary>
	public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer)
	{
		int totalRead = 0;
		while (totalRead < buffer.Length)
		{
			var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead)).ConfigureAwait(false);
			if (bytesRead == 0)
				throw new InvalidDataException("Unexpected end of stream during read.");
			totalRead += bytesRead;
		}
	}

	/// <summary>
	/// Reads exactly the specified number of bytes into the provided memory.
	/// </summary>
	public static async Task ReadExactlyAsync(this Stream stream, Memory<byte> buffer)
	{
		int totalRead = 0;
		while (totalRead < buffer.Length)
		{
			var bytesRead = await stream.ReadAsync(buffer.Slice(totalRead)).ConfigureAwait(false);
			if (bytesRead == 0)
				throw new InvalidDataException("Unexpected end of stream during read.");
			totalRead += bytesRead;
		}
	}
}
