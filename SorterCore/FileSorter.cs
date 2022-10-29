namespace SorterCore;

public class FileSorter
{
    private const string TestOutputDir = "sortertemp";
    public string OutputPath { get; }
    public string InputPath { get; }

    public FileSorter(string inputPath, string outputPath)
    {
        InputPath = inputPath;
        OutputPath = outputPath;
    }

    public bool SortFile()
    {
        if (!File.Exists(InputPath))
            return false;

        //DUMBSORT
        var lines = File.ReadAllLines(InputPath);
        var sortedLines = lines
            .OrderBy(l => l.Split('.')[1])
            .ThenBy(l => l.Split('.')[0])
            .ToArray();

        using (var stream = new FileStream(OutputPath, FileMode.CreateNew))
        using (var bs = new BufferedStream(stream))
        using (var writer = new StreamWriter(bs))
        {
            foreach (var line in sortedLines)
            {
                writer.WriteLine(line);
            }
        }

        return true;
    }
}
