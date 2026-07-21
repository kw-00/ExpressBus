using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressBus.Protocol.Sourcegen;

public record NamedTypeMetadata(INamedTypeSymbol Symbol, TypeDeclarationSyntax Syntax)
{
    public virtual bool Equals(NamedTypeMetadata? other) => other != null && Symbol.Equals(other.Symbol);
    public override int GetHashCode() => Symbol.GetHashCode();
}
