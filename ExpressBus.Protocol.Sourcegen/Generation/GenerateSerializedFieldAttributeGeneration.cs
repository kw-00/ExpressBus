namespace ExpressBus.Protocol.Sourcegen.Generation;

public static class GenerateSerializedFieldAttributeGeneration
{
    public static string Generate()
    {
        return """
            namespace ExpressBus.Protocol;

            [System.AttributeUsage(System.AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
            public class GenerateSerializedFieldAttribute : System.Attribute
            {
                public string Name { get; }
                public SerializableFieldType Type { get; }

                public GenerateSerializedFieldAttribute(string name, SerializableFieldType type)
                {
                    Name = name;
                    Type = type;
                }
            }
            """;
    }
}
