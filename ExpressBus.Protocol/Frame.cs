namespace ExpressBus.Protocol;

public struct Frame
{
    public byte TypeId { get; }
    public int ByteCount { get; }

    public Frame(byte typeId, int byteCount)
    {
        TypeId = typeId;
        ByteCount = byteCount;
    }
}

public struct FrameAndMessage
{
    public Frame Frame { get; }
    public ReadOnlyMemory<byte> Message { get; }

    public FrameAndMessage(Frame frame, ReadOnlyMemory<byte> message)
    {
        Frame = frame;
        Message = message;
    }
}
