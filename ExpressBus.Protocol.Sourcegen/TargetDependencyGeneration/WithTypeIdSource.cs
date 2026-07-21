using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.Sourcegen.TargetDependencyGeneration
{
    public class WithTypeIdSource
    {
        private readonly IncrementalGeneratorPostInitializationContext _context;

        public WithTypeIdSource(IncrementalGeneratorPostInitializationContext context)
        {
            _context = context;
        }

        public void AddSource()
        {
            const string source = """
                namespace ExpressBus.Protocol.Sourcegen.TargetDependencies;

                public interface IWithTypeId
                {
                    static abstract byte TypeId { get; }
                }
                """;
            _context.AddSource("ExpressBus.Protocol.Sourcegen.TargetDependencies.IWithTypeId.g.cs", source);
        }
    }
}
