namespace RPGMakerExtractor;

public struct ArchiveFile
{
    public string Name;
    public int Size;
    public long Offset;
    public uint DecryptionKey;

    internal RPGMakerArchiveReader Reader;

    public byte[] GetBytes()
    {
        // Store reader state before seeking
        uint oldDecryptionKey = Reader.DecryptionKey;
        long oldOffset = Reader.Position;

        // Seek to ArchiveFile data's position and set decryption key
        Reader.SeekTo(Offset, DecryptionKey);
        byte[] data = Reader.ReadEncryptedBytes(Size);

        // Seek back to previous state to prevent key synchronization issues
        Reader.SeekTo(oldOffset, oldDecryptionKey);

        return data;
    }

    internal unsafe void RemoveInvalidNameCharacters()
    {
        fixed (char* namePtr = Name)
        {
            for (int i = 0; i < Name.Length; i++)
                if (s_InvalidPathCharacters.Contains(namePtr[i]))
                    namePtr[i] = '_';
        }
    }
    private static readonly char[] s_InvalidPathCharacters = Path.GetInvalidPathChars();
}
