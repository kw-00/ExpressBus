using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;
using ExpressBus.Protocol.Sourcegen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

[Generator]
public class SerializationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributeType = typeof(GenerateSerializationAttribute);

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
                var source = new SerializationSource(generator);

                foreach (var type in ctx)
                {
                    var propDataList = new List<SerializablePropData>();
                    foreach (var member in type.Symbol.GetMembers().OfType<IPropertySymbol>())
                    {
                        var attribute = member.GetAttributes().FirstOrDefault(a =>
                            a.AttributeClass?.Name == "GenerateSerializedPropAttribute" &&
                            a.AttributeClass?.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) 
                                == "global::ExpressBus.Protocol.Sourcegen.SharedDependencies");

                        if (attribute != null)
                        {
                            propDataList.Add(new SerializablePropData(attribute));
                        }
                    }

                    source.AddSource(type, propDataList);
                }
            });
    }
}
