namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

using Microsoft.CodeAnalysis;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

internal class SerializablePropData
{
    public string Name { get; }
    public SerializablePropType Type { get; }

    public SerializablePropData(AttributeData attributeData)
    {
        if (attributeData.AttributeClass?.Name != "GenerateSerializedPropAttribute" ||
            attributeData.AttributeClass?.ContainingNamespace.ToDisplayString() != "ExpressBus.Protocol.Sourcegen.SharedDependencies")
        {
            throw new System.ArgumentException("Attribute must be GenerateSerializedPropAttribute from SharedDependencies namespace.");
        }

        if (attributeData.ConstructorArguments.Length < 2)
        {
            throw new System.ArgumentException("Attribute must have name and type arguments.");
        }

        Name = attributeData.ConstructorArguments[0].Value?.ToString()
            ?? throw new System.ArgumentException("Attribute argument 'name' is missing.");

        if (attributeData.ConstructorArguments[1].Value is not SerializablePropType type)
        {
            throw new System.ArgumentException("Attribute argument 'type' is not of type SerializablePropType.");
        }

        Type = type;
    }
}
