using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace ExpressBus.Protocol.Sourcegen;

public static class DeclarationGeneration
{
    /// <summary>
    /// Generates a normalized string representation of a partial type declaration,
    /// excluding the base list, primary constructor, members, and the opening block.
    /// </summary>
    /// <param name="originalDeclaration">The type declaration to process.</param>
    /// <returns>A normalized string representation of the declaration.</returns>
    /// <exception cref="ArgumentException">Thrown if the declaration is not marked as partial.</exception>
    public static string GeneratePartial(TypeDeclarationSyntax originalDeclaration)
    {
        if (!originalDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            throw new ArgumentException("The declaration must be partial.", nameof(originalDeclaration));
        }

        var modifiers = originalDeclaration.Modifiers.ToString().Trim();
        var identifier = originalDeclaration.Identifier.ToString();
        var typeParameterList = originalDeclaration.TypeParameterList?.ToString() ?? string.Empty;
        var constraintClauses = originalDeclaration.ConstraintClauses.ToString().Trim();

        var kind = originalDeclaration.Kind().ToString()
            .Replace("Declaration", "")
            .ToLowerInvariant();

        var result = $"{modifiers} {kind} {identifier}{typeParameterList}";

        if (!string.IsNullOrEmpty(constraintClauses))
        {
            result += $" {constraintClauses}";
        }

        return result;
    }
}
