namespace SorterCore;

public class FileSorter
{
    private readonly object _lock = new();
    private readonly FileSorterOptions _options;
    private readonly ILineSorter _lineSorter;
    private readonly ILineComparer _lineComparer;
    private volatile bool _isSplitDone;
    private volatile bool _isSortDone;
    private readonly ConcurrentQueue<string> _unsortedChunks = new();
    private readonly ConcurrentQueue<string> _unmergedChunks = new();

    public FileSorter(FileSorterOptions options, ILineSorter lineSorter, ILineComparer lineComparer)
    {
        _options = options;
        _lineSorter = lineSorter;
        _lineComparer = lineComparer;
        PrepareTempDirectory();
    }

    public bool SortFile()
    {
        if (string.IsNullOrEmpty(_options.InputPath) || string.IsNullOrEmpty(_options.OutputPath))
            return false;

        if (!File.Exists(_options.InputPath))
            return false;

        var cts = new CancellationTokenSource();
        var monitorTask = Task.Run(() => MonitorQueue(cts.Token));
        var splitTask = Task.Run(SplitInputFile);

        var sortTasks = Enumerable.Range(0, _options.PoolSize)
            .Select(_ => Task.Run(SortChunks))
            .ToArray();

        var mergeTasks = Enumerable.Range(0, _options.PoolSize)
            .Select(_ => Task.Run(() => SortMerge()))
            .ToArray();

        Task.WaitAll(splitTask);
        Task.WaitAll(sortTasks);
        Task.WaitAll(mergeTasks);

        SortMerge();
        cts.Cancel();
        return true;
    }

    private void MonitorQueue(CancellationToken token)
    {
        var previousCount = _unsortedChunks.Count + _unmergedChunks.Count;
        while (!token.IsCancellationRequested)
        {
            var currentCount = _unsortedChunks.Count + _unmergedChunks.Count;
            if (previousCount != currentCount)
            {
                Console.Write($"QUEUE sort/merge: {_unsortedChunks.Count}/{_unmergedChunks.Count}        \r");
                previousCount = currentCount;
            }
            Thread.Sleep(100); // Reduce CPU usage
        }
    }

    private void SplitInputFile()
    {
        Thread.CurrentThread.Name = "SplitInputFile";
        var lineCounter = 0;
        var chunkIndex = 0;
        var stringBuilder = new StringBuilder();

        foreach (var line in File.ReadLines(_options.InputPath))
        {
            if (!string.IsNullOrEmpty(line))
                stringBuilder.AppendLine(line);

            if (++lineCounter % _options.MaxLinesPerChunk == 0)
            {
                SaveChunk(stringBuilder, chunkIndex++);
                lineCounter = 0;
            }
        }

        if (stringBuilder.Length > 0)
            SaveChunk(stringBuilder);

        lock (_lock)
        {
            _isSplitDone = true;
        }
    }

    private void SaveChunk(StringBuilder stringBuilder, int index = -1)
    {
        var chunkFilePath = Path.Combine(_options.TempDir, $"{_options.ChunkBaseName}{(index == -1 ? Guid.NewGuid().ToString() : index.ToString())}.chunk");
        File.WriteAllText(chunkFilePath, stringBuilder.ToString());
        _unsortedChunks.Enqueue(chunkFilePath);
        stringBuilder.Clear();
    }

    private void SortChunks()
    {
        while (_unsortedChunks.TryDequeue(out var chunkFilePath) || !_isSplitDone)
        {
            if (string.IsNullOrEmpty(chunkFilePath))
            {
                lock (_lock)
                {
                    if (_isSplitDone)
                        break;
                }
                continue;
            }

            var lines = File.ReadAllLines(chunkFilePath);
            var sortedLines = _lineSorter.Sort(lines, 0, lines.Length - 1, _lineComparer);
            File.WriteAllLines(chunkFilePath, sortedLines);
            _unmergedChunks.Enqueue(chunkFilePath);
        }

        lock (_lock)
        {
            _isSortDone = true;
        }
    }

    private void SortMerge()
    {
        while (_unmergedChunks.TryDequeue(out var chunk1) || !_isSortDone)
        {
            lock (_lock)
            {
                if (_isSortDone && string.IsNullOrEmpty(chunk1))
                    break;
            }

            if (!_unmergedChunks.TryDequeue(out var chunk2))
            {
                if (_isSortDone)
                {
                    File.Move(chunk1, _options.OutputPath);
                    break;
                }

                _unmergedChunks.Enqueue(chunk1);
                continue;
            }

            var mergedChunkPath = Path.Combine(_options.TempDir, $"{Guid.NewGuid()}.chunkmerge");
            MergeChunks(chunk1, chunk2, mergedChunkPath, _lineComparer);

            _unmergedChunks.Enqueue(mergedChunkPath);
            File.Delete(chunk1);
            File.Delete(chunk2);
        }
    }

    private void MergeChunks(string chunk1, string chunk2, string outputPath, ILineComparer comparer)
    {
        using var reader1 = new StreamReader(chunk1);
        using var reader2 = new StreamReader(chunk2);
        using var writer = new StreamWriter(outputPath);

        var line1 = reader1.ReadLine();
        var line2 = reader2.ReadLine();

        while (line1 != null || line2 != null)
        {
            if (line2 == null || (line1 != null && comparer.Compare(line1, line2) <= 0))
            {
                writer.WriteLine(line1);
                line1 = reader1.ReadLine();
            }
            else
            {
                writer.WriteLine(line2);
                line2 = reader2.ReadLine();
            }
        }
    }

    private void PrepareTempDirectory()
    {
        if (Directory.Exists(_options.TempDir))
        {
            Directory.Delete(_options.TempDir, recursive: true);
        }
        Directory.CreateDirectory(_options.TempDir);
    }
}