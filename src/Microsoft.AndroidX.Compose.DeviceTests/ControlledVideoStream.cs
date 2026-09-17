namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class ControlledVideoStream : Stream
{
    readonly long _length;
    readonly long? _throwAt;
    long _position;

    internal ControlledVideoStream(long length, long? throwAt = null)
    {
        _length = length;
        _throwAt = throwAt;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_throwAt is long throwAt && _position >= throwAt)
            throw new IOException("Controlled read failure.");
        if (_position >= _length)
            return 0;
        int read = (int)Math.Min(count, _length - _position);
        Array.Clear(buffer, offset, read);
        _position += read;
        return read;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
