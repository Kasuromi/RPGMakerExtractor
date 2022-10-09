using RPGMakerExtractor.Extensions;
using System.Runtime.CompilerServices;
using System.Text;

namespace RPGMakerExtractor;

public class RPGMakerArchiveReader
{
    public long Position => m_FileStream.Position;
    public uint DecryptionKey => m_DecryptionKey;
    public Stream Stream => m_FileStream;

    public RPGMakerArchiveReader(string filename)
    {
        m_FileStream = new(filename, FileMode.Open);
        m_DecryptionKey = 0xDEADCAFE;

        if (m_FileStream.ReadCString(6) != "RGSSAD")
            throw new NotSupportedException("RGSSAD magic header not found. Invalid or unsupported archive format.");
        m_FileStream.ReadPrimitive<ushort>();

        m_ArchiveFilesOffset = m_FileStream.Position;
    }

    public IEnumerable<ArchiveFile> ReadArchiveFiles(bool removeInvalidNameChars = false)
    {
        m_FileStream.Seek(m_ArchiveFilesOffset, SeekOrigin.Begin);
        while (m_FileStream.Position != m_FileStream.Length)
        {
            ArchiveFile file = ReadArchiveFile(removeInvalidNameChars);
            m_FileStream.Seek(file.Size, SeekOrigin.Current);
            yield return file;
        }
    }

    internal void SeekTo(long position, uint decryptionKey)
    {
        m_DecryptionKey = decryptionKey;
        m_FileStream.Seek(position, SeekOrigin.Begin);
    }

    internal unsafe ArchiveFile ReadArchiveFile(bool removeInvalidNameChars)
    {
        ArchiveFile file = new();

        int fileNameLength = ReadEncryptedI32();

        file.Name = ReadEncryptedCString(fileNameLength);
        if (removeInvalidNameChars) file.RemoveInvalidNameCharacters();

        file.Size = ReadEncryptedI32(); // u32?
        file.Offset = m_FileStream.Position;
        file.DecryptionKey = m_DecryptionKey;
        file.Reader = this;

        return file;
    }

    internal byte ReadEncryptedU8()
    {
        int readByte = m_FileStream.ReadByte();
        if (readByte == -1) throw new EndOfStreamException();

        byte b = (byte)(readByte ^ m_DecryptionKey);

        SlideKey();
        return b;
    }

    internal int ReadEncryptedI32()
    {
        int i32 = (int)(m_FileStream.ReadPrimitive<int>() ^ m_DecryptionKey);

        SlideKey();
        return i32;
    }

    internal string ReadEncryptedCString(int len)
    {
        byte[] bytes = m_FileStream.ReadBytesLE(len);

        for (int i = 0; i < bytes.Length; ++i)
        {
            bytes[i] ^= (byte)m_DecryptionKey;
            SlideKey();
        }

        return Encoding.UTF8.GetString(bytes);
    }

    internal unsafe byte[] ReadEncryptedBytes(int len)
    {
        // Read the bytes out of the file
        byte[] bytes = m_FileStream.ReadBytesLE(len);

        fixed (uint* decryptionKeyPtr = &m_DecryptionKey)
        {
            // Decrypt the bytes in sections of 4 bytes
            // This could be made faster if we align the original byte array
            // to a 4 byte boundary and xor a uint to a uint
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (i % 4 == 0 && i != 0) SlideKey();
                bytes[i] ^= ((byte*)decryptionKeyPtr)[i % 4];
            }
        }

        return bytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void SlideKey()
    {
        m_DecryptionKey = (m_DecryptionKey * 7) + 3;
    }

    private uint m_DecryptionKey;
    private readonly FileStream m_FileStream;
    private readonly long m_ArchiveFilesOffset;
}
