using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressBus.Protocol.SourceGeneration.Protocol;

/// <summary>
/// Represents a parsed <c>[MessageField]</c> attribute value.
/// </summary>
/// <param name="Name">The field name as declared in the attribute.</param>
/// <param name="UnderlyingType">The serialized type (enum underlying type for enum fields).</param>
/// <param name="EnumType">Fully-qualified enum type name, or null for non-enum fields.</param>
/// <param name="IsEnum">Whether <see cref="EnumType"/> is non-null — i.e. the field wraps an enum.</param>
/// <param name="DefiningAttributeSyntax">The <c>[MessageField]</c> <see cref="AttributeSyntax"/>.</param>
internal record MessageFieldInfo(
	string Name,
	AllowedType UnderlyingType,
	[property: NullWhen(true)] string? EnumType,
	bool IsEnum,
	AttributeSyntax DefiningAttributeSyntax);
