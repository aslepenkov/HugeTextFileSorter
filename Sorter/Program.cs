// See https://aka.ms/new-console-template for more information



var TestOutputDir = "output";
var unsortedFilePath = Path.Combine(TestOutputDir, "unsorted.txt");
var sortedFilePath = $"{unsortedFilePath}.sorted";



//args sorter.exe y 1000 1024 
//args sorter.exe n 1024 
var buff = 1024;
var createNew = args[0].Equals("y");
var size = createNew ? Int32.Parse(args[1]) : 1000;
var pool = 1;

Int32.TryParse(args[createNew ? 2 : 1], out buff);
Int32.TryParse(args[createNew ? 3 : 2], out pool);

Console.WriteLine($"Sorter.exe| File size: {size} MB. Chunks: {buff} lines. Pool: {pool} tasks");

// Console.WriteLine(@"Generate new unsorted file? (Y\n)");
// var l = Console.ReadLine();
// size = Int32.Parse(l);
if (createNew)
{
    PrepareTestDir(TestOutputDir);
    //Console.WriteLine("Enter size (Default 100MB)");
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
    Console.WriteLine(@"(default: output\unsorted.txt)");
}
var sw = new Stopwatch();
sw.Start();

using (var proc = Process.GetCurrentProcess())
{
    var fi = new FileInfo(unsortedFilePath);
    Console.WriteLine($"{DateTime.UtcNow}|Sorting file... size: {fi.Length / 1024 / 1024} MB");
    var fs = new FileSorter(unsortedFilePath, sortedFilePath);
    fs.LINES_PER_CHUNK = buff;
    fs.POOL_SIZE = pool;
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