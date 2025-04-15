namespace SorterTest;

[TestFixture]
public class Tests
{
    private const string TestOutputDir = "testoutput";
    private const bool WipeArtifacts = true;

    [SetUp]
    public void Setup()
    {
        PrepareTestOutputDirectory();
    }

    [Test]
    public void WriterDoesNotRewriteFile_WhenFileAlreadyExists()
    {
        const int sizeInMB = 1;
        var filePath = GetTestFilePath("unsorted.txt");

        var fileWriter = new FileWriter(filePath, sizeInMB);
        var firstWriteResult = fileWriter.GenerateFile();
        var secondWriteResult = fileWriter.GenerateFile();

        Assert.IsTrue(firstWriteResult, "File should be created on the first attempt.");
        Assert.IsFalse(secondWriteResult, "File should not be overwritten on the second attempt.");
    }

    [TestCase(10)]
    public void WriterCreatesFileWithExactSize(int preferredSizeInMB)
    {
        var filePath = GetTestFilePath($"unsorted_{preferredSizeInMB}_MB.txt");
        var fileWriter = new FileWriter(filePath, preferredSizeInMB);

        var creationResult = fileWriter.GenerateFile();
        Assert.IsTrue(creationResult, "File should be created successfully.");

        var fileInfo = new FileInfo(filePath);
        var actualSizeInMB = fileInfo.Length / 1024 / 1024;

        Assert.IsTrue(Math.Abs(actualSizeInMB - preferredSizeInMB) < 1, "File size should match the preferred size within 1 MB accuracy.");
    }

    [Test]
    public void WriterOutputContainsDuplicateStrings()
    {
        const int sizeInMB = 1;
        var filePath = GetTestFilePath("unsorted_samestrings.txt");

        var fileWriter = new FileWriter(filePath, sizeInMB);
        var creationResult = fileWriter.GenerateFile();
        Assert.IsTrue(creationResult, "File should be created successfully.");

        var lines = File.ReadLines(filePath);
        var duplicates = lines
            .Select(line => line.Split('.')[1])
            .GroupBy(str => str)
            .Where(group => group.Count() > 1);

        Assert.IsTrue(duplicates.Any(), "Output should contain duplicate strings.");
    }

    [TestCase(1)]
    [TestCase(10)]
    public void SorterSortsFirstNLinesCorrectly(int linesCount)
    {
        const int fileSizeInMB = 10;
        var unsortedFilePath = GetTestFilePath("unsorted_lines.txt");
        var sortedFilePath = GetTestFilePath("sorted_lines.txt");

        var fileWriter = new FileWriter(unsortedFilePath, fileSizeInMB);
        var fileSorter = new FileSorter(
            new FileSorterOptions
            {
                InputPath = unsortedFilePath,
                OutputPath = sortedFilePath
            },
            new MergeSortLineSorter(),
            new LineComparer()
        );

        Assert.IsTrue(fileWriter.GenerateFile(), "Unsorted file should be created successfully.");
        Assert.IsTrue(fileSorter.SortFile(), "File should be sorted successfully.");

        var referenceLines = File.ReadAllLines(unsortedFilePath)
            .OrderBy(line => line.Split('.')[1])
            .ThenBy(line => line.Split('.')[0])
            .ToArray();

        var sortedLines = File.ReadAllLines(sortedFilePath).Take(linesCount);

        CollectionAssert.AreEqual(referenceLines.Take(linesCount), sortedLines, "First N lines should be sorted correctly.");
    }

    [TestCase("unsorted.txt", "sorted.txt")]
    public void SorterProducesExpectedOutput(string unsortedFileName, string sortedReferenceFileName)
    {
        var inputPath = Path.Combine("testdata", unsortedFileName);
        var referencePath = Path.Combine("testdata", sortedReferenceFileName);
        var resultPath = GetTestFilePath("unsorted.txt.sorted");

        var fileSorter = new FileSorter(
            new FileSorterOptions
            {
                InputPath = inputPath,
                OutputPath = resultPath
            },
            new MergeSortLineSorter(),
            new LineComparer()
        );

        Assert.IsTrue(fileSorter.SortFile(), "File should be sorted successfully.");

        var referenceBytes = File.ReadAllBytes(referencePath);
        var resultBytes = File.ReadAllBytes(resultPath);

        CollectionAssert.AreEqual(referenceBytes, resultBytes, "Sorted file should match the reference output.");
    }

    [TestCase("1.Apple", "415.Apple", -1)]
    [TestCase("415.Apple", "2.Banana is yellow", -1)]
    public void LineComparerComparesLinesCorrectly(string left, string right, int expectedResult)
    {
        var comparer = new LineComparer();
        Assert.AreEqual(expectedResult, comparer.Compare(left, right), "Line comparison result should match the expected value.");
    }

    [TearDown]
    public void TearDown()
    {
        if (WipeArtifacts)
        {
            CleanupTestOutputDirectory();
        }
    }

    private void PrepareTestOutputDirectory()
    {
        var fullPath = GetTestOutputDirectoryPath();

        if (WipeArtifacts && Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }

    private void CleanupTestOutputDirectory()
    {
        var fullPath = GetTestOutputDirectoryPath();

        if (Directory.Exists(fullPath))
        {
            Directory.Move(fullPath, $"{fullPath}_del");
            Directory.Delete($"{fullPath}_del", recursive: true);
        }
    }

    private string GetTestOutputDirectoryPath() => Path.Combine(Directory.GetCurrentDirectory(), TestOutputDir);

    private string GetTestFilePath(string fileName) => Path.Combine(GetTestOutputDirectoryPath(), fileName);
}