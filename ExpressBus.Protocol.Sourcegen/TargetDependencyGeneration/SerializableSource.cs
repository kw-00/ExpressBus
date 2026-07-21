using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.Sourcegen.TargetDependencyGeneration
{
    public class SerializableSource
    {
        private readonly IncrementalGeneratorPostInitializationContext _context;

        public SerializableSource(IncrementalGeneratorPostInitializationContext context)
        {
            _context = context;
        }

        public void AddSource()
        {
            const string source = """
                using System;

                namespace ExpressBus.Protocol.Sourcegen.TargetDependencies;

                public interface ISerializable<T> where T : ISerializable<T>
                {
                    int ByteCount { get; }
                    void ToBytes(Span<byte> buffer);
                     static abstract T FromBytes(ReadOnlyMemory<byte> buffer);
                }
                """;
            _context.AddSource("ExpressBus.Protocol.Sourcegen.TargetDependencies.ISerializable.g.cs", source);
        }
    }
}
