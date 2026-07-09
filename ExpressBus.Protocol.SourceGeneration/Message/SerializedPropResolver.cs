using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressBus.Protocol.SourceGeneration.Message;

/// <summary>
/// Resolves <c>[GenerateSerializedProp]</c> attribute arguments from a message struct declaration.
/// </summary>
/// <remarks>
/// Responsibility: parse attribute arguments and resolve types semantically.
/// No structural validation — diagnostics are handled by <c>Validation</c>.
/// </remarks>
internal static class SerializedPropResolver
{
	/// <summary>
	/// Maps a Roslyn <see cref="SpecialType"/> to the corresponding <see cref="AllowedType"/>.
	/// Falls back to <see cref="AllowedType.Guid"/> when the symbol is <c>System.Guid</c>.
	/// </summary>
	/// <remarks>
	/// This is used to resolve the underlying type of <c>[GenerateSerializedProp]</c> arguments.
	/// </remarks>
	private static AllowedType ResolveSpecialType(ITypeSymbol typeSymbol) =>
		typeSymbol.SpecialType switch
		{
			SpecialType.System_Byte    => AllowedType.Byte,
			SpecialType.System_Boolean => AllowedType.Bool,
			SpecialType.System_Int16   => AllowedType.Short,
			SpecialType.System_Int32   => AllowedType.Int,
			SpecialType.System_Int64   => AllowedType.Long,
			SpecialType.System_Single  => AllowedType.Float,
			SpecialType.System_Double  => AllowedType.Double,
			SpecialType.None => typeSymbol.ContainingNamespace.ToDisplayString() == "System"
				&& typeSymbol.Name == "Guid"
					? AllowedType.Guid
					: AllowedType.Invalid,
			_ => AllowedType.Invalid
		};

	/// <summary>
	/// Resolves <c>[GenerateSerializedProp("propName", typeof(PropType))]</c> attributes from the struct.
	/// </summary>
	/// <remarks>
	/// Resolves types semantically: enums are unwrapped to their underlying integral type,
	/// and built-in types are mapped to <see cref="AllowedType"/>. Returns zero or more
	/// <see cref="MessagePropInfo"/> objects.
	/// </remarks>
	public static IEnumerable<MessagePropInfo> ParseSerializedPropAttributes(
		SyntaxSymbol symbol,
		SemanticModel semanticModel)
	{
		var serializedPropAttrs = symbol.Declaration.AttributeLists
			.SelectMany(list => list.Attributes)
			.Where(attr =>
			{
				var name = attr.Name.ToString();
				return name == "GenerateSerializedProp" || name == "GenerateSerializedPropAttribute";
			});

		foreach (var attr in serializedPropAttrs)
		{
			var argList = attr.ArgumentList?.Arguments;
			if (argList == null || argList.Value.Count < 2)
				continue;

			// First argument: property name (string literal).
			string? propName = null;
			if (argList.Value[0].Expression is LiteralExpressionSyntax literal &&
				literal.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				propName = literal.Token.ValueText;
			}

			// Second argument: typeof(expression).
			AllowedType underlyingType = AllowedType.Invalid;
			string? enumType = null;
			if (argList.Value[1].Expression is TypeOfExpressionSyntax typeOfExpr)
			{
				var typeName = typeOfExpr.Type.ToString();

				// Check for SerializableMemory types by name (these are source-generated, so semantic
				// resolution may not work during all compilation stages).
				var config = AllowedTypeExtensions.MemoryTypes.FirstOrDefault(m => m.TypeName == typeName);
				if (config.Type != AllowedType.Invalid)
					underlyingType = config.Type;

				// If not a SerializableMemory type, resolve semantically.
				if (underlyingType == AllowedType.Invalid)
				{
					var typeInfo = semanticModel.GetTypeInfo(typeOfExpr.Type);
					var typeSymbol = typeInfo.Type;
					if (typeSymbol != null)
					{
						if (typeSymbol.TypeKind == TypeKind.Enum)
						{
							enumType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
							var underlying = ((INamedTypeSymbol)typeSymbol).EnumUnderlyingType!;
							underlyingType = ResolveSpecialType(underlying);
						}
						else
						{
							underlyingType = ResolveSpecialType(typeSymbol);
						}
					}
				}
			}

			if (propName != null)
			{
				yield return new MessagePropInfo(propName, underlyingType, enumType, enumType is not null, attr);
			}
		}
	}
}
