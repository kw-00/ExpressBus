namespace ExpressBus.Protocol.Sourcegen.Generation;

public static class GenerateSerializationAttributeGeneration
{
    public static string Generate()
    {
        return """
            namespace ExpressBus.Protocol;

            [System.AttributeUsage(System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            public class GenerateSerializationAttribute : System.Attribute { }
            """;
    }
}
