namespace ExpressBus.Protocol.Sourcegen.SharedDependencies;

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public class GenerateSerializedPropAttribute : System.Attribute
{
    public string Name { get; }
    public SerializablePropType Type { get; }

    public GenerateSerializedPropAttribute(string name, SerializablePropType type)
    {
        Name = name;
        Type = type;
    }
}
