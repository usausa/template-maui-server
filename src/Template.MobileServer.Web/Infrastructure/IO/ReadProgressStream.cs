namespace Template.MobileServer.Web.Infrastructure.IO;

public sealed class ReadProgressStream : Stream
{
    private readonly Stream source;

    private readonly long total;

    private readonly Action<int> progress;

    private long readTotal;

    private int lastPercent = -1;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => total;

    public override long Position
    {
        get => readTotal;
        set => throw new NotSupportedException();
    }

    public ReadProgressStream(Stream source, long total, Action<int> progress)
    {
        this.source = source;
        this.total = total;
        this.progress = progress;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await source.ReadAsync(buffer, cancellationToken);
        readTotal += read;
        Report();
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            source.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Report()
    {
        var percent = total > 0 ? (int)(readTotal * 100 / total) : 100;
        if (percent != lastPercent)
        {
            lastPercent = percent;
            progress(percent);
        }
    }
}
