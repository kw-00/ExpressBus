using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.Sourcegen.TargetDependencyGeneration
{
    public class WithTypeIdSource
    {
        private readonly SourceProductionContext _context;

        public WithTypeIdSource(SourceProductionContext context)
        {
            _context = context;
        }

        public void AddSource()
        {
            const string source = """
                namespace ExpressBus.Protocol.Sourcegen.SharedDependencies;

                public interface IWithTypeId
                {
                    static abstract byte TypeId { get; }
                }
                """;
            _context.AddSource("ExpressBus.Protocol.Sourcegen.SharedDependencies.IWithTypeId.g.cs", source);
        }
    }
}
