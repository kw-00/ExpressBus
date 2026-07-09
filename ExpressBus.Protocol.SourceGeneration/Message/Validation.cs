using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressBus.Protocol.SourceGeneration.Message;

/// <summary>
/// Result of validating message structs for the protocol source generator.
/// </summary>
internal sealed record ValidationResult(
	IReadOnlyList<SyntaxSymbol> AllValid,
	IReadOnlyCollection<string> SerializationTypeFqns);

/// <summary>
/// Validates message structs against the requirements for source-generated
/// binary serialization.
/// </summary>
/// <remarks>
/// Validation rules:
/// <list type="bullet">
///   <item><description>All message types: must be partial struct.</description></item>
///   <item><description>[GenerateSerialization] types: must not have user-defined members; prop types must be valid.</description></item>
///   <item><description>[Message] only: user-defined members are allowed.</description></item>
/// </list>
/// <para>
/// Attribute dependency chain — [GenerateSerialization] requires [Message], and
/// [GenerateSerializedProp] requires [GenerateSerialization] — is documented on the
/// attribute classes themselves and is not enforced via diagnostics.
/// </para>
/// </remarks>
internal static class Validation
{
	/// <summary>
	/// Validates all collected message types and returns both valid types and
	/// the set of serialization-enabled type fully-qualified names.
	/// </summary>
	/// <remarks>
	/// Reports every diagnostic for every type — validation does not stop at the
	/// first error so developers can fix multiple issues in one build.
	/// </remarks>
	public static ValidationResult Validate(
		IReadOnlyList<SyntaxSymbol> symbols,
		Compilation compilation,
		SourceProductionContext ctx)
	{
		// Separate symbols by attribute type.
		var messageOnly = new List<SyntaxSymbol>();
		var serializationTypes = new List<SyntaxSymbol>();

		foreach (var symbol in symbols)
		{
			if (HasAttribute(symbol, "GenerateSerialization", "GenerateSerializationAttribute"))
			{
				serializationTypes.Add(symbol);
			}
			else
			{
				messageOnly.Add(symbol);
			}
		}

		// Validate [Message]-only types.
		var validMessageOnly = ValidateMessageOnlyTypes(messageOnly, compilation, ctx);

		// Validate [GenerateSerialization] types.
		var validSerialization = ValidateSerializationTypes(serializationTypes, compilation, ctx);

		// Build the FQN set of serialization types (for isSerializationType check).
		var serializationFqns = new HashSet<string>(StringComparer.Ordinal);
		foreach (var sym in validSerialization)
		{
			serializationFqns.Add(sym.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
		}

		// Combine: both groups are valid.
		var allValid = new List<SyntaxSymbol>(validSerialization.Count + validMessageOnly.Count);
		allValid.AddRange(validSerialization);
		allValid.AddRange(validMessageOnly);

		return new ValidationResult(allValid, serializationFqns);
	}

	private static bool HasAttribute(SyntaxSymbol symbol, params string[] attributeNames)
	{
		return symbol.Declaration.AttributeLists
			.SelectMany(list => list.Attributes)
			.Any(attr => attributeNames.Contains(attr.Name.ToString()));
	}

	/// <summary>
	/// Validates types with only [Message] attribute — structural checks only, user members allowed.
	/// </summary>
	private static IReadOnlyList<SyntaxSymbol> ValidateMessageOnlyTypes(
		IReadOnlyList<SyntaxSymbol> symbols,
		Compilation compilation,
		SourceProductionContext ctx)
	{
		var valid = new List<SyntaxSymbol>();

		foreach (var symbol in symbols)
		{
			// [Message]-only types allow user-defined members.
			if (ValidateBasicStructure(symbol, compilation, ctx, allowMembers: true))
				valid.Add(symbol);
		}

		return valid;
	}

	/// <summary>
	/// Validates types with [GenerateSerialization] attribute — structural checks plus
	/// member and field type restrictions.
	/// </summary>
	private static IReadOnlyList<SyntaxSymbol> ValidateSerializationTypes(
		IReadOnlyList<SyntaxSymbol> symbols,
		Compilation compilation,
		SourceProductionContext ctx)
	{
		var valid = new List<SyntaxSymbol>();

		foreach (var symbol in symbols)
		{
			if (ValidateBasicStructure(symbol, compilation, ctx, allowMembers: false))
			{
				// Validate prop types.
				var semanticModel = compilation.GetSemanticModel(symbol.Declaration.SyntaxTree);
				var props = SerializedPropResolver.ParseSerializedPropAttributes(symbol, semanticModel);
				var propTypesValid = true;
				foreach (var prop in props)
				{
					if (prop.UnderlyingType == AllowedType.Invalid)
					{
						var loc = prop.DefiningAttributeSyntax.GetLocation();
						ctx.ReportDiagnostic(Diagnostic.Create(
							Diagnostics.UnsupportedFieldType,
							loc ?? symbol.Symbol.Locations[0],
							prop.Name,
							symbol.Symbol.Name,
							"Unknown"));
						propTypesValid = false;
					}
				}

				if (propTypesValid)
					valid.Add(symbol);
			}
		}

		return valid;
	}

	/// <summary>
	/// Validates basic structural requirements for any message type.
	/// </summary>
	private static bool ValidateBasicStructure(
		SyntaxSymbol symbol,
		Compilation compilation,
		SourceProductionContext ctx,
		bool allowMembers)
	{
		var hasError = false;

		// 1. Must be partial.
		var isPartial = symbol.Declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
		if (!isPartial)
		{
			ctx.ReportDiagnostic(Diagnostic.Create(
				Diagnostics.InvalidPartialModifier,
				symbol.Symbol.Locations[0],
				symbol.Symbol.Name));
			hasError = true;
		}

		// 2. Must be a struct.
		if (symbol.Symbol.TypeKind != TypeKind.Struct)
		{
			ctx.ReportDiagnostic(Diagnostic.Create(
				Diagnostics.InvalidTypeKind,
				symbol.Symbol.Locations[0],
				symbol.Symbol.Name,
				symbol.Symbol.TypeKind.ToString()));
			hasError = true;
		}

		// 3. Check for user-defined members (only for [GenerateSerialization] types).
		if (!allowMembers)
		{
			var memberDiagnostics = CheckNoUserMembers(symbol);
			foreach (var diagnostic in memberDiagnostics)
			{
				ctx.ReportDiagnostic(diagnostic);
				hasError = true;
			}
		}

		return !hasError;
	}

	/// <summary>
	/// Reports diagnostics for user-defined members on a [GenerateSerialization] struct.
	/// </summary>
	/// <remarks>
	/// [GenerateSerialization] types must not contain any user-defined fields, properties,
	/// or methods. All members come from the source generator. Static members are ignored.
	/// </remarks>
	private static IReadOnlyList<Diagnostic> CheckNoUserMembers(SyntaxSymbol symbol)
	{
		var diagnostics = new List<Diagnostic>();

		// Check for non-static fields.
		var userFields = symbol.Declaration.Members
			.OfType<FieldDeclarationSyntax>()
			.Where(f => !f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)));

		foreach (var field in userFields)
		{
			diagnostics.Add(Diagnostic.Create(
				Diagnostics.FieldDeclarationNotSupported,
				field.GetLocation(),
				symbol.Symbol.Name));
		}

		// Check for non-static properties.
		var userProperties = symbol.Declaration.Members
			.OfType<PropertyDeclarationSyntax>()
			.Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)));

		foreach (var prop in userProperties)
		{
			diagnostics.Add(Diagnostic.Create(
				Diagnostics.FieldDeclarationNotSupported, // Reuse the same diagnostic — still "no user members"
				prop.GetLocation(),
				symbol.Symbol.Name));
		}

		// Check for non-static methods (excluding the generated constructor).
		var userMethods = symbol.Declaration.Members
			.OfType<MethodDeclarationSyntax>()
			.Where(m =>
				!m.Modifiers.Any(x => x.IsKind(SyntaxKind.StaticKeyword)) &&
				m.Identifier.Text != symbol.Symbol.Name); // Skip constructor

		foreach (var method in userMethods)
		{
			diagnostics.Add(Diagnostic.Create(
				Diagnostics.FieldDeclarationNotSupported, // Reuse the same diagnostic — still "no user members"
				method.GetLocation(),
				symbol.Symbol.Name));
		}

		return diagnostics;
	}
}
