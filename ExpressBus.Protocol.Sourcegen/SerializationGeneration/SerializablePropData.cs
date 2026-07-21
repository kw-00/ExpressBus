namespace ExpressBus.Protocol.Sourcegen.SerializationGeneration;

using Microsoft.CodeAnalysis;
using ExpressBus.Protocol.Sourcegen.SharedDependencies;

public class SerializablePropData
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

        var value = attributeData.ConstructorArguments[1].Value;
        if (value is SerializablePropType type)
        {
            Type = type;
        }
        else if (value is int intValue)
        {
            Type = (SerializablePropType)intValue;
        }
        else if (value is long longValue)
        {
            Type = (SerializablePropType)longValue;
        }
        else
        {
            throw new System.ArgumentException($"Attribute argument 'type' is not of type SerializablePropType. Found: {value?.GetType().Name ?? "null"}");
        }
    }
}
