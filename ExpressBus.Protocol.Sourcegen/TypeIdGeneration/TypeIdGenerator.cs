using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;
using ExpressBus.Protocol.Sourcegen;
using Microsoft.CodeAnalysis.CSharp;

namespace ExpressBus.Protocol.Sourcegen.TypeIdGeneration;

[Generator]
public class TypeIdGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributeType = typeof(GenerateTypeIdAttribute);

        var candidates =
            context.SyntaxProvider.ForAttributeWithMetadataName(
                attributeType.FullName!,
                static (node, _) =>
                    node is TypeDeclarationSyntax t && t.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                static (ctx, _) =>
                    new NamedTypeMetadata((INamedTypeSymbol)ctx.TargetSymbol, (TypeDeclarationSyntax)ctx.TargetNode));

        var deduplicatedCandidates = candidates
            .Collect()
            .Select(static (c, _) => 
            {
                return c
                    .GroupBy(candidate => candidate.Symbol, SymbolEqualityComparer.Default)
                    .Select(g => g.First());
            });

        context.RegisterImplementationSourceOutput(
            deduplicatedCandidates,
            static (generator, ctx) =>
            {
                var source = new TypeIdSource(generator, 0);

                foreach (var type in ctx)
                    source.AddSource(type);
            });   
    }

}
