namespace ExpressBus.Protocol.Sourcegen.Generation;

using ExpressBus.Protocol.Sourcegen.SharedDependencies;

public static class RWCallGeneration
{
    public static string GenerateReadCall(string readerName, SerializablePropType propType)
    {
        return $"{readerName}.Read{propType}()";
    }

    public static string GenerateWriteCall(string writerName, SerializablePropData propData)
    {
        return $"{writerName}.Write{propData.Type}({propData.Name})";
    }
}
