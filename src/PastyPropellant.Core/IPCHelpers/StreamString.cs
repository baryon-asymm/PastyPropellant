using System.Text;

namespace PastyPropellant.Core.IPCHelpers;

public class StreamString
{
    public static readonly string InitReceiveResult = "init-receive-result";
    public static readonly string InitWorkerExit = "init-exit";

    private readonly Stream ioStream;
    private readonly UnicodeEncoding streamEncoding;

    public StreamString(Stream ioStream)
    {
        this.ioStream = ioStream;
        streamEncoding = new UnicodeEncoding();
    }

    public async Task<string> ReadStringAsync()
    {
        int len;
        len = ioStream.ReadByte() * 256;
        len += ioStream.ReadByte();
        var inBuffer = new byte[len];
        await ioStream.ReadAsync(inBuffer, 0, len);

        return streamEncoding.GetString(inBuffer);
    }

    public async Task<int> WriteStringAsync(string outString)
    {
        var outBuffer = streamEncoding.GetBytes(outString);
        var len = outBuffer.Length;
        if (len > ushort.MaxValue) len = ushort.MaxValue;
        ioStream.WriteByte((byte)(len / 256));
        ioStream.WriteByte((byte)(len & 255));
        await ioStream.WriteAsync(outBuffer, 0, len);
        await ioStream.FlushAsync();

        return outBuffer.Length + 2;
    }
}
