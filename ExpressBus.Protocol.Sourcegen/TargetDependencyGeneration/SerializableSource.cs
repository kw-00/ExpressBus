using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.Sourcegen.TargetDependencyGeneration
{
    public class SerializableSource
    {
        private readonly SourceProductionContext _context;

        public SerializableSource(SourceProductionContext context)
        {
            _context = context;
        }

        public void AddSource()
        {
            const string source = """
                using System;

                namespace ExpressBus.Protocol.Sourcegen.SharedDependencies;

                public interface ISerializable<T> where T : ISerializable<T>
                {
                    int ByteCount { get; }
                    void ToBytes(Span<byte> buffer);
                    static abstract T FromBytes(Span<byte> buffer);
                }
                """;
            _context.AddSource("ExpressBus.Protocol.Sourcegen.SharedDependencies.ISerializable.g.cs", source);
        }
    }
}
