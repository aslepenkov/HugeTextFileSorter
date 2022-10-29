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



        return true;
    }
}
