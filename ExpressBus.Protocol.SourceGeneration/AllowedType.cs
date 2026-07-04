namespace ExpressBus.Protocol.SourceGeneration;

/// <summary>
/// Represents the set of types supported for binary serialization in message fields.
/// </summary>
/// <remarks>
/// Replaces repeated <c>type.SpecialType</c> / <c>type.Name</c> switch branches
/// across <c>Generation.cs</c> and <c>Validation.cs</c>. Enums are represented
/// by their underlying integral type (e.g., a byte enum maps to <c>Byte</c>).
/// </remarks>
internal enum AllowedType
{
	Invalid,
	Byte,
	Bool,
	Short,
	Int,
	Long,
	Float,
	Double,
	Guid,
	ByteMemory,
	BoolMemory,
	IntMemory,
	LongMemory,
	GuidMemory,
}
