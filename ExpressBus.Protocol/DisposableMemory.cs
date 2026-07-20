using System;
using System.Buffers;

namespace ExpressBus.Protocol;

public struct DisposableMemory<T> : IDisposable
{
    private readonly T[] _array;
    private readonly int _length;

    public DisposableMemory(int size)
    {
        _array = ArrayPool<T>.Shared.Rent(size);
        _length = size;
    }

    public Memory<T> Memory => _array.AsMemory(0, _length);

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_array);
    }
}
