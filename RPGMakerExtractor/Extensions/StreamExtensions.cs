using System.Text;

namespace RPGMakerExtractor.Extensions;

internal static unsafe class StreamExtensions
{
    public static byte[] ReadBytesLE(this Stream stream, int len)
    {
        byte[] bytes = new byte[len];
        stream.Read(bytes, 0, len);
        return bytes;
    }

    public static string ReadCString(this Stream stream, int len)
    {
        return Encoding.UTF8.GetString(stream.ReadBytesLE(len));
    }

    public static TPrimitive ReadPrimitive<TPrimitive>(this Stream stream) where TPrimitive : unmanaged
    {
        fixed (byte* b = stream.ReadBytesLE(sizeof(TPrimitive)))
        {
            return *(TPrimitive*)b;
        }
    }
}
