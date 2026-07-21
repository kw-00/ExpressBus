using System;
using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.Sourcegen
{
    public static class TypeNameResolution
    {
        public static string GetNamespaceName(INamespaceSymbol symbol)
        {
            return symbol.ToDisplayString();
        }

        public static string GetFullyQualifiedName(INamedTypeSymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public static string GetFullyQualifiedName(INamespaceSymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public static string GetFullyQualifiedName(Type type)
        {
            string fullName = type.FullName ?? throw new InvalidOperationException($"Type {type} FullName is null.");

            return fullName.StartsWith("global::") ? fullName : "global::" + fullName;
        }
    }
}
