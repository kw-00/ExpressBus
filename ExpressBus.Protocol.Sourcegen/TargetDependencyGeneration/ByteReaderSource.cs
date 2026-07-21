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

                namespace ExpressBus.Protocol.Sourcegen.TargetDependencies;

                public struct ByteReader
                {
                    private ReadOnlyMemory<byte> _memory;
                    private int _position;

                    public ByteReader(ReadOnlyMemory<byte> memory)
                    {
                        _memory = memory;
                        _position = 0;
                    }

                    public byte ReadByte()
                    {
                        byte value = _memory.Span[_position];
                        _position++;
                        return value;
                    }

                    public int ReadInt()
                    {
                        int value = BinaryPrimitives.ReadInt32LittleEndian(_memory.Span.Slice(_position, 4));
                        _position += 4;
                        return value;
                    }

                    public Guid ReadGuid()
                    {
                        Guid value = new Guid(_memory.Span.Slice(_position, 16));
                        _position += 16;
                        return value;
                    }

                    public ReadOnlyMemory<byte> ReadByteMemory()
                    {
                        int size = ReadInt();
                        var slice = _memory.Slice(_position, size);
                        _position += size;
                        return slice;
                    }
                }
                """;
            _context.AddSource("ExpressBus.Protocol.Sourcegen.TargetDependencies.ByteReader.g.cs", source);
        }
    }
}
