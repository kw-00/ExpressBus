namespace ExpressBus.Protocol;

/// <summary>
/// Base interface for protocol message types.
/// </summary>
/// <remarks>
/// Implemented by all message structs annotated with <c>[Message]</c>, including those that also have
/// <c>[GenerateSerialization]</c>. The interface declares <c>MessageTypeIdentifier</c> (a unique byte
/// tag used on the wire).
///
/// Serialization interfaces — <see cref="IByteSerializable{T}"/> and <see cref="IMessageSize"/> —
/// are implemented by types that also have <c>[GenerateSerialization]</c>. Types with only
/// <c>[Message]</c> must implement these interfaces manually if they provide serialization logic.
/// </remarks>
public interface IMessage
{
	/// <summary>
	/// Unique byte tag identifying this message type on the wire.
	/// </summary>
	static byte MessageTypeIdentifier { get; }
}
