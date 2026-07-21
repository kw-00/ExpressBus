using Microsoft.CodeAnalysis;

namespace ExpressBus.Protocol.Sourcegen.TargetDependencyGeneration
{
    public class ByteReaderSource
    {
        private readonly IncrementalGeneratorPostInitializationContext _context;

        public ByteReaderSource(IncrementalGeneratorPostInitializationContext context)
        {
            _context = context;
        }

        public void AddSource()
        {
            const string source = """
                using System;
                using System.Buffers.Binary;

                namespace ExpressBus.Protocol.Sourcegen.SharedDependencies;

                public ref struct ByteReader
                {
                    private ReadOnlySpan<byte> _span;
                    private int _position;

                    public ByteReader(ReadOnlySpan<byte> span)
                    {
                        _span = span;
                        _position = 0;
                    }

                    public byte ReadByte()
                    {
                        byte value = _span[_position];
                        _position++;
                        return value;
                    }

                    public int ReadInt()
                    {
                        int value = BinaryPrimitives.ReadInt32LittleEndian(_span.Slice(_position, 4));
                        _position += 4;
                        return value;
                    }

                    public Guid ReadGuid()
                    {
                        Guid value = new Guid(_span.Slice(_position, 16));
                        _position += 16;
                        return value;
                    }

                    public ReadOnlySpan<byte> ReadByteMemory()
                    {
                        int size = ReadInt();
                        var slice = _span.Slice(_position, size);
                        _position += size;
                        return slice;
                    }
                }
                """;
            _context.AddSource("ExpressBus.Protocol.Sourcegen.SharedDependencies.ByteReader.g.cs", source);
        }
    }
}
