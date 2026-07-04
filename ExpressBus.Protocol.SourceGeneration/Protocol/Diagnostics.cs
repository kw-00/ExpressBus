using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.SourceGeneration.Protocol;

/// <summary>
/// Diagnostic descriptors for the protocol source generator.
/// </summary>
/// <remarks>
/// DLMSG001–DLMSG005 cover all validation failures. Each diagnostic uses
/// <c>DiagnosticSeverity.Error</c> and is enabled by default.
/// </remarks>
internal static class Diagnostics
{
	/// <summary>
	/// The message struct must be declared as <c>partial</c> so the generator can
	/// append the serialization methods.
	/// </summary>
	public static readonly DiagnosticDescriptor InvalidPartialModifier = new(
		id: "DLMSG001",
		title: "Message type must be partial",
		messageFormat: "Message type '{0}' must be declared as partial",
		category: "ExpressBus.Protocol.SourceGeneration",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// The message type must be a <c>struct</c>, not a class or record.
	/// </summary>
	public static readonly DiagnosticDescriptor InvalidTypeKind = new(
		id: "DLMSG002",
		title: "Message type must be a struct",
		messageFormat: "Message type '{0}' must be a struct, not a {1}",
		category: "ExpressBus.Protocol.SourceGeneration",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// Message structs must not contain any user-declared fields.
	/// Fields are declared via <c>[GenerateSerializedField]</c> attributes only.
	/// </summary>
	public static readonly DiagnosticDescriptor FieldDeclarationNotSupported = new(
		id: "DLMSG003",
		title: "Message struct must not declare fields",
		messageFormat: "Message type '{0}' must not contain field declarations; " +
			"use [GenerateSerializedField] attributes to define fields",
		category: "ExpressBus.Protocol.SourceGeneration",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// There are more than 256 message types. This limit exists because
	/// MessageTypeIdentifier is a single byte.
	/// </summary>
	public static readonly DiagnosticDescriptor TooManyMessageTypes = new(
		id: "DLMSG004",
		title: "Too many message types",
		messageFormat: "Message type count ({0}) exceeds the maximum of {1} because " +
			"MessageTypeIdentifier is a byte",
		category: "ExpressBus.Protocol.SourceGeneration",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// A <c>[GenerateSerializedField]</c> attribute references an unsupported type for binary
	/// serialization (byte, bool, short, int, long, float, double, Guid, or enums thereof).
	/// </summary>
	public static readonly DiagnosticDescriptor UnsupportedFieldType = new(
		id: "DLMSG005",
		title: "Serialized field has unsupported type",
		messageFormat: "[GenerateSerializedField] '{0}' in message type '{1}' has unsupported type '{2}'",
		category: "ExpressBus.Protocol.SourceGeneration",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}
