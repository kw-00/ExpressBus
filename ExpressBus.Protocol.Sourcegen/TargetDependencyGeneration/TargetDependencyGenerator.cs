using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.Sourcegen.TargetDependencyGeneration;

[Generator]
public class TargetDependencyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            new SerializableSource(ctx).AddSource();
            new ByteReaderSource(ctx).AddSource();
            new ByteWriterSource(ctx).AddSource();
            new WithTypeIdSource(ctx).AddSource();
        });
    }
}
