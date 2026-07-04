namespace ExpressBus.Protocol;

/// <summary>
/// Indicates that a message type has a known serialized byte size.
/// </summary>
/// <remarks>
/// Implemented by message types that have <c>[GenerateSerialization]</c> attribute.
/// Types with only <c>[Message]</c> do not implement this interface.
/// </remarks>
public interface IMessageSize
{
	/// <summary>
	/// The number of bytes required to serialize this message.
	/// </summary>
	int ByteSize { get; }
}
