// See https://aka.ms/new-console-template for more information

var testOutputDir = "output";

if (args.Length == 0)
{
    Console.WriteLine(Helper.WelcomeText);
    Console.ReadKey();
    return;
}

var sorterOptions = Helper.InitOptions(args, testOutputDir);
Helper.ShowOptionsMessage(sorterOptions);

if (sorterOptions.CreateNew)
{
    Helper.PrepareTestDir(testOutputDir);
    Helper.ShowMessage();

    var fw = new FileWriter(sorterOptions.InputPath, sorterOptions.FileSizeMByte);
    var fi = new FileInfo(sorterOptions.InputPath);

    if (fw.GenerateFile())
        Helper.ShowSuccessFWMessage(sorterOptions, fi);
}


if (File.Exists(sorterOptions.OutputPath))
    File.Delete(sorterOptions.OutputPath);

var sw = new Stopwatch();
sw.Start();

using (var proc = Process.GetCurrentProcess())
{
    var fi = new FileInfo(sorterOptions.InputPath);
    Helper.ShowStartSortMessage(fi);

    var fs = new FileSorter(sorterOptions);

    if (fs.SortFile())
    {
        Helper.ShowSuccessSortMessage(proc, sw);
    }
    else
    {
        Console.WriteLine($"incomplete");
    }
}

