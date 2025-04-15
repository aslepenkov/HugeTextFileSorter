namespace SorterCore;

public class FileWriter
{
    private const int WORDS_DICT_START = 42;
    public readonly string DictPath;
    public string OutputPath { get; }
    public int Size { get; }

    public FileWriter(string path, int sizeinMBytes)
    {
        OutputPath = path;
        Size = sizeinMBytes;
        DictPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "txt", "words.txt");
    }

    public bool GenerateFile()
    {
        if (Size <= 0 || File.Exists(OutputPath))
            return false;

        var rnd = new Random();
        var words = File.ReadAllLines(DictPath);
        var sizeLimitMBytes = (long)Size * 1024 * 1024;

        using (var stream = new FileStream(OutputPath, FileMode.CreateNew))
        using (var writer = new StreamWriter(stream))
        {
            while (stream.Position < sizeLimitMBytes)
            {
                //digit
                var digit = rnd.Next(1, 1000);

                //1st word
                var word = words[rnd.Next(WORDS_DICT_START, words.Length - 1)];

                //2nd word has random occurance
                var word2 = rnd.Next() % 2 == 0 ?
                    string.Empty :
                    $" {words[rnd.Next(WORDS_DICT_START, words.Length - 1)]}";

                //result line
                writer.WriteLine($"{digit}. {word}{word2}");
            }
        }

        return true;
    }
}
