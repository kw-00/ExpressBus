namespace ExpressBus.Protocol.Sourcegen.SharedDependencies;

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public class GenerateSerializedFieldAttribute : System.Attribute
{
    public string Name { get; }
    public SerializablePropType Type { get; }

    public GenerateSerializedFieldAttribute(string name, SerializablePropType type)
    {
        Name = name;
        Type = type;
    }
}
