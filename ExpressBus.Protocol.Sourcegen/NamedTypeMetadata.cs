using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressBus.Protocol.Sourcegen;

public class NamedTypeMetadata
{
    public INamedTypeSymbol Symbol { get; }
    public TypeDeclarationSyntax Syntax { get; }

    public NamedTypeMetadata(INamedTypeSymbol Symbol, TypeDeclarationSyntax Syntax)
    {
        this.Symbol = Symbol;
        this.Syntax = Syntax;
    }
}
