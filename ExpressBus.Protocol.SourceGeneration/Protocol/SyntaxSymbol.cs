using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressBus.Protocol.SourceGeneration.Protocol;

/// <summary>
/// Holds both the syntax node and the compiled symbol for a message struct.
/// </summary>
/// <remarks>
/// The syntax node is used for structural checks (e.g., partial modifier).
/// The type symbol provides metadata (field types, accessibility, namespace).
/// Both are needed because attribute detection is done at the syntax level
/// while field validation requires semantic analysis.
/// </remarks>
internal sealed record SyntaxSymbol(
	TypeDeclarationSyntax Declaration,
	ITypeSymbol Symbol);
