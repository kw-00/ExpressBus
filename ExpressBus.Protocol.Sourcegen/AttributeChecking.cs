using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExpressBus.Protocol.Sourcegen
{
    public static class AttributeChecking
    {
        public static bool HasAttribute(INamedTypeSymbol targetType, Type attributeType)
        {
            string attributeFullName = TypeNameResolution.GetFullyQualifiedName(attributeType);

            if (string.IsNullOrEmpty(attributeFullName))
            {
                return false;
            }

            foreach (var attribute in targetType.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == attributeFullName)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAttribute(TypeDeclarationSyntax target, Type attributeType)
        {
            string fullyQualifiedName = TypeNameResolution.GetFullyQualifiedName(attributeType);

            if (string.IsNullOrEmpty(fullyQualifiedName)) return false;

            string simpleName = attributeType.Name;
            string unqualifiedFullName = fullyQualifiedName.StartsWith("global::") ? fullyQualifiedName.Substring(7) : fullyQualifiedName;

            foreach (var attributeList in target.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    string attrName = attribute.Name.ToString();
                    if (attrName == simpleName || attrName == unqualifiedFullName || attrName == fullyQualifiedName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
