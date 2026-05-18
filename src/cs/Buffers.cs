namespace HelionACS;

using System.Text;

public static class Buffers
{
    private static byte[] StaticBuffer = new byte[1024];

    public static byte[] GetUtf8BufferStatic(string str)
    {
        var byteCount = Encoding.UTF8.GetMaxByteCount(str.Length + 1);
        if (byteCount > StaticBuffer.Length)
            Array.Resize(ref StaticBuffer, byteCount * 2);

        var count = Encoding.UTF8.GetBytes(str.AsSpan(), StaticBuffer);
        StaticBuffer[count] = 0;
        return StaticBuffer;
    }

    public static byte[] GetUtf8Buffer(string str)
    {
        var byteCount = Encoding.UTF8.GetMaxByteCount(str.Length + 1);
        var buffer = new byte[byteCount];
        var count = Encoding.UTF8.GetBytes(str.AsSpan(), buffer);
        buffer[count] = 0;
        return buffer;
    }
}