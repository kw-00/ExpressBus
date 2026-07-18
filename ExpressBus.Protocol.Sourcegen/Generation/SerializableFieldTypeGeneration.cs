namespace ExpressBus.Protocol.Sourcegen.Generation;

public static class SerializableFieldTypeGeneration
{
    public static string Generate()
    {
        return """
            namespace ExpressBus.Protocol;

            public enum SerializableFieldType
            {
                Byte,
                Int,
                Guid,
                Bytes
            }
            """;
    }
}
