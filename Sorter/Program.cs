// See https://aka.ms/new-console-template for more information

var TestOutputDir = "output";
var size = 100;//MBytes
var unsortedFilePath = Path.Combine(TestOutputDir, "unsorted.txt");
var sortedFilePath = "${unsortedFilePath}.sorted";

Console.WriteLine(@"Generate new unsorted file? (Y\n)");
// var l = Console.ReadLine();
// size = Int32.Parse(l);
if (false)
{
    PrepareTestDir(TestOutputDir);
    Console.WriteLine("Enter size (Default 100MB)");
    // var l = Console.ReadLine();
    // size = Int32.Parse(l);
    Console.WriteLine($"{DateTime.UtcNow}|Creating unsorted file...");
    var fw = new FileWriter(unsortedFilePath, size);
    var isGenerated = fw.GenerateFile();

    if (isGenerated)
        Console.WriteLine($"{DateTime.UtcNow}|File created: {unsortedFilePath}. Size: {size} MB");
}
else
{
    Console.WriteLine(@"Provide path to unsorted (default: output\unsorted.txt)");
    // var l = Console.ReadLine();
    // path = Int32.Parse(l);
}
var sw = new Stopwatch();
sw.Start();

using (var proc = Process.GetCurrentProcess())
{
    Console.WriteLine($"{DateTime.UtcNow}|Sorting file... size: {size} MB");
    var fs = new FileSorter(unsortedFilePath, sortedFilePath);
    var isSorted = fs.SortFile();

    proc.Refresh();
    var memUsed = proc.PrivateMemorySize64 / (1024 * 1024);

    if (isSorted)
    {
        sw.Stop();
        Console.WriteLine($"{DateTime.UtcNow}|Sort complete. Memory usage: {memUsed} MB. Time elapsed: {sw.Elapsed}");
    }
    else
    {
        Console.WriteLine($"Iincomplete");
    }
}

static void PrepareTestDir(string TestOutputDir)
{
    if (Directory.Exists(TestOutputDir))
        Directory.Delete(TestOutputDir, recursive: true);

    // if (Directory.Exists("sortertemp"))
    //     Directory.Delete("sortertemp", recursive: true);

    Directory.CreateDirectory(TestOutputDir);
}