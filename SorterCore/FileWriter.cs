namespace SorterCore;

public class FileWriter
{
    public const string DictPath = @"txt\words.txt";
    public string OutputPath { get; }
    public int Size { get; }

    public FileWriter(string path, int sizeinMBytes)
    {
        OutputPath = path;
        Size = sizeinMBytes;
    }

    public bool GenerateFile()
    {
        if (Size <= 0 || File.Exists(OutputPath))
            return false;

        var rnd = new Random();
        var words = File.ReadAllLines(DictPath);
        long sizeLimitMBytes = (long)Size * 1024 * 1024;

        using (var stream = new FileStream(OutputPath, FileMode.CreateNew))
        using (var bs = new BufferedStream(stream))
        using (var writer = new StreamWriter(bs))
        {
            while (stream.Position < sizeLimitMBytes)
            {
                //digit
                var digit = rnd.Next(1, 10000); //max: Int32.MaxValue ?

                //1st word
                var word = words[rnd.Next(45, words.Length - 1)];

                //2nd word has random occurance
                var word2 = rnd.Next() % 2 == 0 ?
                    string.Empty :
                    $" {words[rnd.Next(45, words.Length - 1)]}";

                //result line
                writer.WriteLine($"{digit}.{word}{word2}");
            }
        }

        return true;
    }
}
