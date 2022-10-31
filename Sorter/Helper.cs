public static class Helper
{
    public static readonly string WelcomeText = @"
        //Args
        Usage: Sorter.exe [Generate new file|use existsing: y/n] [File size in MB] (Lines per chunk) (Parralel tasks count)
        
        //run 1GB file generation->sort with 10000 lines chunks and 100 parralel sort/merge tasks
        Usage: Sorter.exe y 1000 10000 100  

        //run output\unsorted.txt file sort with 100 lines chunks and 1000 parralel sort/merge tasks
        Usage: Sorter.exe n 1000 100 1000

        Source: https://github.com/aslepenkov/HugeTextFileSorter
        ";

    public static void PrepareTestDir(string TestOutputDir)
    {
        if (Directory.Exists(TestOutputDir))
            Directory.Delete(TestOutputDir, recursive: true);

        Directory.CreateDirectory(TestOutputDir);
    }

    public static FileSorterOptions InitOptions(string[] args, string testOutputDir)
    {
        var unsortedFilePath = Path.Combine(testOutputDir, "unsorted.txt");
        var sortedFilePath = $"{unsortedFilePath}.sorted";
        var buff = 1;
        var pool = 1;
        var createNew = args[0].ToLower().Equals("y");
        Int32.TryParse(args[createNew ? 2 : 1], out buff);
        Int32.TryParse(args[createNew ? 3 : 2], out pool);

        return new FileSorterOptions
        {
            CreateNew = createNew,
            FileSizeMByte = createNew ? Int32.Parse(args[1]) : 1000,
            MaxLinesPerChunk = buff,
            PoolSize = pool,
            InputPath = unsortedFilePath,
            OutputPath = sortedFilePath
        };
    }
    public static void ShowSuccessSortMessage(Process proc, Stopwatch sw)
    {
        sw.Stop();
        proc.Refresh();
        var memUsed = proc.PrivateMemorySize64 / 1024 / 1024;
        Console.WriteLine($"{DateTime.UtcNow}|Sort complete. Memory usage: {memUsed} MB. Time elapsed: {sw.Elapsed}");
    }

    public static void ShowStartSortMessage(FileInfo fi)
    {
        Console.WriteLine($"{DateTime.UtcNow}|Sorting file... size: {fi.Length / 1024 / 1024} MB");
    }
    public static void ShowSuccessFWMessage(FileSorterOptions sorterOptions, FileInfo fi)
    {
        Console.WriteLine($"{DateTime.UtcNow}|File created: {sorterOptions.InputPath}. Size: {fi.Length / 1024 / 1024} MB");
    }

    public static void ShowMessage()
    {
        Console.WriteLine($"{DateTime.UtcNow}|Creating unsorted file...");
    }
    public static void ShowOptionsMessage(FileSorterOptions sorterOptions)
    {
        var fileSizeText = sorterOptions.CreateNew ? $"File size: {sorterOptions.FileSizeMByte} MB. " : string.Empty;
        Console.WriteLine($"Sorter.exe| {fileSizeText}Chunks: {sorterOptions.MaxLinesPerChunk} lines. Pool: {sorterOptions.PoolSize} tasks");
    }
}
