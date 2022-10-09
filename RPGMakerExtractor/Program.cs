namespace RPGMakerExtractor;

internal static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine($"Usage: RPGMakerExtractor.exe <RpgMakerArchivePath> <ExtractionPath>");
            return;
        }

        string archivePath = args[0];
        string extractionPath = args[1];

        var reader = new RPGMakerArchiveReader(archivePath);

        if (!Directory.Exists(extractionPath)) Directory.CreateDirectory(extractionPath);

        foreach (ArchiveFile file in reader.ReadArchiveFiles(true))
        {
            string filePath = Path.Combine(extractionPath, file.Name);

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            Console.WriteLine($"Writing file '{file.Name}' (Size {file.Size})");
            File.WriteAllBytes(filePath, file.GetBytes());
        }
    }
}