using System.Diagnostics.CodeAnalysis;

namespace ExpressBus.Protocol;

/// <summary>
/// A discriminated union that wraps either a success response or an error response.
/// </summary>
/// <typeparam name="TSuccess">The success response type (must implement IMessage).</typeparam>
/// <typeparam name="TError">The error response type (must implement IMessage).</typeparam>
public struct Result<TSuccess, TError>
	: IByteSerializable<Result<TSuccess, TError>>
	, IMessageSize
	where TSuccess : struct, IByteSerializable<TSuccess>, IMessage, IMessageSize
	where TError : struct, IByteSerializable<TError>, IMessage, IMessageSize
{
	/// <summary>
	/// The success response, or null if this is an error.
	/// </summary>
	public TSuccess? Success { get; }

	/// <summary>
	/// The error response, or null if this is a success.
	/// </summary>
	public TError? Error { get; }

	/// <summary>
	/// True if this instance wraps a success response.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Success))]
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess => Success is not null;

	/// <summary>
	/// The maximum of the two payload sizes, used to allocate a buffer large enough
	/// for either variant.
	/// </summary>
	public int ByteSize => Math.Max(default(TSuccess).ByteSize, default(TError).ByteSize);

	private static readonly byte SuccessId = default(TSuccess).MessageTypeIdentifier;
	private static readonly byte ErrorId = default(TError).MessageTypeIdentifier;

	/// <summary>
	/// Creates a success response.
	/// </summary>
	public Result(TSuccess success)
	{
		Success = success;
		Error = null;
	}

	/// <summary>
	/// Creates an error response.
	/// </summary>
	public Result(TError error)
	{
		Success = null;
		Error = error;
	}

	/// <summary>
	/// Deserializes a <see cref="Result{TSuccess, TError}"/> from a byte buffer.
	/// </summary>
	/// <remarks>
	/// The first byte is the MessageTypeIdentifier of the active payload type.
	/// If it matches <typeparamref name="TSuccess"/>, the buffer is forwarded to
	/// <c>TSuccess.FromBytes</c>; otherwise it is forwarded to <c>TError.FromBytes</c>.
	/// </remarks>
	public static Result<TSuccess, TError> FromBytes(Memory<byte> buffer)
	{
		var firstByte = buffer.Span[0];

		if (firstByte == SuccessId)
		{
			return new Result<TSuccess, TError>(TSuccess.FromBytes(buffer));
		}
		else if (firstByte == ErrorId)
		{
			return new Result<TSuccess, TError>(TError.FromBytes(buffer));
		}

		throw new FormatException(
			$"Unrecognized MessageTypeIdentifier 0x{firstByte:X2} in buffer (expected 0x{SuccessId:X2} or 0x{ErrorId:X2}).");
	}

	/// <summary>
	/// Serializes this instance into the given buffer and returns the used portion.
	/// </summary>
	/// <remarks>
	/// The buffer is resized to the actual serialized payload size
	/// (either <c>TSuccess.ByteSize</c> or <c>TError.ByteSize</c>, whichever is active).
	/// </remarks>
	public ReadOnlyMemory<byte> ToBytes(Memory<byte> buffer)
	{
		var requiredSize = IsSuccess ? Success.Value.ByteSize : Error.Value.ByteSize;
		if (buffer.Length < requiredSize)
			throw new ArgumentException(
				$"Buffer is too small ({buffer.Length} bytes). "
				+ $"Required: {requiredSize}.");

		if (IsSuccess)
		{
			var success = Success.Value;
			var result = success.ToBytes(buffer);
			return result.Slice(0, Success.Value.ByteSize);
		}
		else
		{
			var error = Error.Value;
			var result = error.ToBytes(buffer);
			return result.Slice(0, Error.Value.ByteSize);
		}
	}
}
