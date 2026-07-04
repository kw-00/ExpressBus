using System;

namespace ExpressBus.Protocol.SourceGeneration;

/// <summary>
/// Metadata describing one of the source-generated SerializableMemory types.
/// </summary>
internal readonly record struct MemoryTypeConfig(
	AllowedType Type,
	string TypeName,
	string ItemTypeName,
	int ItemSize);

/// <summary>
/// Extension methods for <see cref="AllowedType"/> — serialization metadata.
/// </summary>
internal static class AllowedTypeExtensions
{
	/// <summary>
	/// The complete set of source-generated SerializableMemory types.
	/// This is the single source of truth — the generator and all consumers
	/// reference this collection.
	/// </summary>
	public static readonly MemoryTypeConfig[] MemoryTypes =
	[
		new(AllowedType.ByteMemory, "SerializableByteMemory", "byte", 1),
		new(AllowedType.BoolMemory, "SerializableBoolMemory", "bool", 1),
		new(AllowedType.IntMemory, "SerializableIntMemory", "int", 4),
		new(AllowedType.LongMemory, "SerializableLongMemory", "long", 8),
		new(AllowedType.GuidMemory, "SerializableGuidMemory", "Guid", 16),
	];

	/// <summary>
	/// Returns <c>true</c> if the given <see cref="AllowedType"/> is a memory type.
	/// </summary>
	public static bool IsMemoryType(this AllowedType type) => type switch
	{
		AllowedType.ByteMemory or AllowedType.BoolMemory or
		AllowedType.IntMemory or AllowedType.LongMemory or AllowedType.GuidMemory
			=> true,
		_ => false
	};

	/// <summary>
	/// Returns the item size (in bytes) for a memory type. Throws for non-memory types.
	/// </summary>
	public static int GetMemoryItemSize(this AllowedType type) => type switch
	{
		AllowedType.ByteMemory or AllowedType.BoolMemory => 1,
		AllowedType.IntMemory => 4,
		AllowedType.LongMemory => 8,
		AllowedType.GuidMemory => 16,
		_ => throw new InvalidOperationException($"Expected memory type, got {type}.")
	};

	/// <summary>
	/// Returns the byte size for serialization. Returns -1 for <see cref="AllowedType.Invalid"/>.
	/// </summary>
#pragma warning disable CS8524 // internal enum switch not exhaustive for unnamed values
	public static int ByteSize(this AllowedType type) => type switch
	{
		AllowedType.Byte => 1,
		AllowedType.Bool => 1,
		AllowedType.Short => 2,
		AllowedType.Int => 4,
		AllowedType.Long => 8,
		AllowedType.Float => 4,
		AllowedType.Double => 8,
		AllowedType.Guid => 16,
		AllowedType.Invalid => -1,
		AllowedType.ByteMemory => throw new NotSupportedException("Memory types have dynamic size"),
		AllowedType.BoolMemory => throw new NotSupportedException("Memory types have dynamic size"),
		AllowedType.IntMemory => throw new NotSupportedException("Memory types have dynamic size"),
		AllowedType.LongMemory => throw new NotSupportedException("Memory types have dynamic size"),
		AllowedType.GuidMemory => throw new NotSupportedException("Memory types have dynamic size"),
	};
#pragma warning restore CS8524

	/// <summary>
	/// Returns the C# type name (keyword or fully qualified) to emit in generated code.
	/// </summary>
	public static string CSharpTypeName(this AllowedType type) => type switch
	{
		AllowedType.Byte => "byte",
		AllowedType.Bool => "bool",
		AllowedType.Short => "short",
		AllowedType.Int => "int",
		AllowedType.Long => "long",
		AllowedType.Float => "float",
		AllowedType.Double => "double",
		AllowedType.Guid => "global::System.Guid",
		AllowedType.ByteMemory => "global::ExpressBus.Protocol.SerializableByteMemory",
		AllowedType.BoolMemory => "global::ExpressBus.Protocol.SerializableBoolMemory",
		AllowedType.IntMemory => "global::ExpressBus.Protocol.SerializableIntMemory",
		AllowedType.LongMemory => "global::ExpressBus.Protocol.SerializableLongMemory",
		AllowedType.GuidMemory => "global::ExpressBus.Protocol.SerializableGuidMemory",
		_ => throw new NotSupportedException($"Unsupported type: {type}")
	};

	/// <summary>
	/// Returns the <c>ByteReader</c> method name for deserialization.
	/// </summary>
	public static string ReadMethodName(this AllowedType type) => type switch
	{
		AllowedType.Byte => "ReadByte",
		AllowedType.Bool => "ReadBool",
		AllowedType.Short => "ReadShort",
		AllowedType.Int => "ReadInt",
		AllowedType.Long => "ReadLong",
		AllowedType.Float => "ReadFloat",
		AllowedType.Double => "ReadDouble",
		AllowedType.Guid => "ReadGuid",
		AllowedType.ByteMemory => throw new NotSupportedException("Memory types use custom deserialization"),
		AllowedType.BoolMemory => throw new NotSupportedException("Memory types use custom deserialization"),
		AllowedType.IntMemory => throw new NotSupportedException("Memory types use custom deserialization"),
		AllowedType.LongMemory => throw new NotSupportedException("Memory types use custom deserialization"),
		AllowedType.GuidMemory => throw new NotSupportedException("Memory types use custom deserialization"),
		_ => throw new NotSupportedException($"Unsupported type: {type}")
	};

	/// <summary>
	/// Returns the <c>ByteWriter</c> method name for serialization.
	/// </summary>
	public static string WriteMethodName(this AllowedType type) => type switch
	{
		AllowedType.Byte => "WriteByte",
		AllowedType.Bool => "WriteBool",
		AllowedType.Short => "WriteShort",
		AllowedType.Int => "WriteInt",
		AllowedType.Long => "WriteLong",
		AllowedType.Float => "WriteFloat",
		AllowedType.Double => "WriteDouble",
		AllowedType.Guid => "WriteGuid",
		AllowedType.ByteMemory => throw new NotSupportedException("Memory types use custom serialization"),
		AllowedType.BoolMemory => throw new NotSupportedException("Memory types use custom serialization"),
		AllowedType.IntMemory => throw new NotSupportedException("Memory types use custom serialization"),
		AllowedType.LongMemory => throw new NotSupportedException("Memory types use custom serialization"),
		AllowedType.GuidMemory => throw new NotSupportedException("Memory types use custom serialization"),
		_ => throw new NotSupportedException($"Unsupported type: {type}")
	};
}
