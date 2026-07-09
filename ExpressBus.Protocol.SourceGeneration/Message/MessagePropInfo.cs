using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressBus.Protocol.SourceGeneration.Message;

/// <summary>
/// Represents a parsed <c>[GenerateSerializedProp]</c> attribute value.
/// </summary>
/// <param name="Name">The property name as declared in the attribute.</param>
/// <param name="UnderlyingType">The serialized type (enum underlying type for enum properties).</param>
/// <param name="EnumType">Fully-qualified enum type name, or null for non-enum properties.</param>
/// <param name="IsEnum">Whether <see cref="EnumType"/> is non-null — i.e. the property wraps an enum.</param>
/// <param name="DefiningAttributeSyntax">The <c>[GenerateSerializedProp]</c> <see cref="AttributeSyntax"/>.</param>
internal record MessagePropInfo(
	string Name,
	AllowedType UnderlyingType,
	[property: NullWhen(true)] string? EnumType,
	bool IsEnum,
	AttributeSyntax DefiningAttributeSyntax);
