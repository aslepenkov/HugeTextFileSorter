// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Logging;

var builder = LoggerFactory.Create(logging =>
{
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    });
    logging.SetMinimumLevel(LogLevel.Information);
});
var logger = builder.CreateLogger("Sorter");

if (args.Length == 0)
{
    logger.LogInformation(Helper.WelcomeText);
    Console.ReadKey();
    return;
}

var sorterOptions = Helper.InitOptions(args);
Helper.ShowOptionsMessage(sorterOptions, logger);

if (sorterOptions.CreateNew)
{
    Helper.PrepareTestDir(sorterOptions.OutputDir, logger);
    Helper.ShowMessage(logger);

    var fw = new FileWriter(sorterOptions.InputPath, sorterOptions.FileSizeMByte);
    var fi = new FileInfo(sorterOptions.InputPath);

    if (fw.GenerateFile())
        Helper.ShowSuccessFWMessage(sorterOptions, fi, logger);
}

if (File.Exists(sorterOptions.OutputPath))
    File.Delete(sorterOptions.OutputPath);

var sw = new Stopwatch();
sw.Start();

using (var proc = Process.GetCurrentProcess())
{
    var fi = new FileInfo(sorterOptions.InputPath);
    Helper.ShowStartSortMessage(fi, logger);

    var lineSorter = new MergeSortLineSorter();
    var lineComparer = new LineComparer();
    var fs = new FileSorter(sorterOptions, lineSorter, lineComparer);

    if (fs.SortFile())
        Helper.ShowSuccessSortMessage(proc, sw, logger);
    else
        logger.LogError("incomplete");
}

