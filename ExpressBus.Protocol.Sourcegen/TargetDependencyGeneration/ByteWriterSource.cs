using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.Sourcegen.TargetDependencyGeneration;

public class ByteWriterSource
{
    private readonly IncrementalGeneratorPostInitializationContext _context;

    public ByteWriterSource(IncrementalGeneratorPostInitializationContext context)
    {
        _context = context;
    }

    public void AddSource()
    {
        const string source = """
            using System;
            using System.Buffers.Binary;

            namespace ExpressBus.Protocol.Sourcegen.SharedDependencies;

            public ref struct ByteWriter
            {
                private Span<byte> _span;
                private int _position;

                public ByteWriter(Span<byte> span)
                {
                    _span = span;
                    _position = 0;
                }

                public void WriteByte(byte value)
                {
                    _span[_position] = value;
                    _position++;
                }

                public void WriteInt(int value)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(_span.Slice(_position, 4), value);
                    _position += 4;
                }

                public void WriteGuid(Guid value)
                {
                    value.TryWriteBytes(_span.Slice(_position, 16));
                    _position += 16;
                }

                public void WriteByteMemory(ReadOnlySpan<byte> data)
                {
                    WriteInt(data.Length);
                    data.CopyTo(_span.Slice(_position, data.Length));
                    _position += data.Length;
                }
            }
            """;
        _context.AddSource("ExpressBus.Protocol.Sourcegen.SharedDependencies.ByteWriter.g.cs", source);
    }
}
