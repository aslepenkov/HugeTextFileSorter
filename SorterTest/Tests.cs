namespace SorterTest;

public class Tests
{
    private const string TestOutputDir = "testoutput";
    private const bool WIPE_ARTIFACTS = false;

    [SetUp]
    public void Setup()
    {
        var currDir = Directory.GetCurrentDirectory();
        var fullpath = Path.Combine(currDir, TestOutputDir);

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
    [TestCase(100)]
    [TestCase(1000)]
    [TestCase(5000)]
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

    [TearDown]
    public void Dispose()
    {
        if (!WIPE_ARTIFACTS)
            return;

        var currDir = Directory.GetCurrentDirectory();
        var fullpath = Path.Combine(currDir, TestOutputDir);

        if (Directory.Exists(fullpath))
            Directory.Delete(fullpath, recursive: true);
    }
}