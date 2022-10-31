namespace SorterTest;

public class Tests
{
    private const string TestOutputDir = "testoutput";
    private const bool WIPE_ARTIFACTS = true;

    [SetUp]
    public void Setup()
    {
        var currDir = Directory.GetCurrentDirectory();
        var fullpath = Path.Combine(currDir, TestOutputDir);

        if (WIPE_ARTIFACTS && Directory.Exists(fullpath))
            Directory.Delete(fullpath, recursive: true);

        if (!Directory.Exists(fullpath))
            Directory.CreateDirectory(fullpath);
    }

    [Test]
    public void WriterDoNotRewriteFileTest()
    {
        var size = 1;//MBytes
        var path = Path.Combine(TestOutputDir, "unsorted.txt");

        var fw = new FileWriter(path, size);
        var res1 = fw.GenerateFile();
        var res2 = fw.GenerateFile();

        Assert.IsTrue(res1);
        Assert.IsFalse(res2);
    }

    [TestCase(10)]
    //[TestCase(100)]
    //[TestCase(1000)]
    //[TestCase(5000)]
    //[TestCase(10000)]
    public void WriterCreatesExactSizeTest(int prefferedMBytes)
    {
        var path = Path.Combine(TestOutputDir, $"unsorted_{prefferedMBytes}_MB.txt");
        var fw = new FileWriter(path, prefferedMBytes);
        var res = fw.GenerateFile();

        Assert.IsTrue(res); // File Created

        var fi = new FileInfo(path);
        var factMBytes = fi.Length / 1024 / 1024;

        Assert.IsTrue(Math.Abs(factMBytes - prefferedMBytes) < 1000);// +/- 1КB accuracy
    }

    [Test]
    public void WriterOutputContainsSameStringsTest()
    {
        var size = 1;//MBytes
        var path = Path.Combine(TestOutputDir, "unsorted_samestrings.txt");

        var fw = new FileWriter(path, size);
        var res = fw.GenerateFile();

        Assert.IsTrue(res); // File Created

        var lines = File.ReadLines(path);

        var dublicates = lines.Select(l => l.Split('.')[1])
            .GroupBy(str => str)
            .Where(grp => grp.Count() > 2)
            .Select(str => str);

        Assert.Greater(dublicates.Count(), 0);
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(10000)]
    [TestCase(100000)]
    public void SorterFirstNLinesTest(int linesCount)
    {
        var size = 10;//MBytes
        var path = Path.Combine(TestOutputDir, "unsorted_lines.txt");
        var resPath = Path.Combine(TestOutputDir, "sorted_lines.txt");

        var fw = new FileWriter(path, size);
        var fs = new FileSorter(new FileSorterOptions
        {
            InputPath = path,
            OutputPath = resPath
        });
        var isCreated = fw.GenerateFile();
        Assert.IsTrue(isCreated); // File Created

        var isSorted = fs.SortFile();
        Assert.IsTrue(isSorted); // File sorted

        var linesRef = File.ReadAllLines(path);
        var sortedRefLines = linesRef
            .OrderBy(l => l.Split('.')[1])
            .ThenBy(l => l.Split('.')[0])
            .ToArray();

        var linesRes = File.ReadAllLines(resPath).Take(linesCount);

        Assert.AreEqual(sortedRefLines.Take(linesCount), linesRes.Take(linesCount));
    }

    [TestCase("unsorted.txt", "sorted.txt")]
    public void SorterTest(string unsortedFileName, string sortedrefFileName)
    {
        var inputPath = Path.Combine("testdata", unsortedFileName);
        var outputRefPath = Path.Combine("testdata", sortedrefFileName);
        var outputResPath = Path.Combine(TestOutputDir, "unsorted.txt.sorted");

        var fs = new FileSorter(new FileSorterOptions
        {
            InputPath = inputPath,
            OutputPath = outputResPath
        });
        var isSorted = fs.SortFile();

        Assert.IsTrue(isSorted); // File sorted

        var linesRef = File.ReadAllBytes(outputRefPath).ToArray();
        var linesRes = File.ReadAllBytes(outputResPath).ToArray();

        Assert.AreEqual(linesRef, linesRes);
    }

    [TestCase("1.Apple", "415.Apple", -1)]
    [TestCase("415.Apple", "2.Banana is yellow", -1)]
    [TestCase("2.Banana is yellow", "32.Cherry is the best", -1)]
    [TestCase("32.Cherry is the best", "30432.Something something something", -1)]
    [TestCase("415.Apple", "1.Apple", 1)]
    [TestCase("2.Banana is yellow", "415.Apple", 1)]
    [TestCase("32.Cherry is the best", "2.Banana is yellow", 1)]
    [TestCase("30432.Something something something", "32.Cherry is the best", 1)]
    public void SorterCompareLinesTest(string left, string right, int res)
    {
        var comparer = new LineComparer();
        Assert.AreEqual(res, comparer.Compare(left, right));
    }

    [TearDown]
    public void Dispose()
    {
        if (!WIPE_ARTIFACTS)
            return;

        var currDir = Directory.GetCurrentDirectory();
        var fullpath = Path.Combine(currDir, TestOutputDir);

        if (Directory.Exists(fullpath))
        {
            Directory.Move(fullpath, $"{fullpath}_del"); //faster rename, then delete
            Directory.Delete($"{fullpath}_del", recursive: true);
        }
    }
}