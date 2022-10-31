namespace SorterCore;

public class FileSorter
{
    object _lock = new();
    private FileSorterOptions opt { get; }
    private volatile bool IsSplitDone = false;
    private volatile bool IsSortDone = false;
    private ConcurrentQueue<string> unsortedChunks = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> unmergedChunks = new ConcurrentQueue<string>();

    public FileSorter(FileSorterOptions sorterOptions)
    {
        opt = sorterOptions;
        PrepareTempDir();
    }

    public bool SortFile()
    {
        if (string.IsNullOrEmpty(opt.InputPath) || string.IsNullOrEmpty(opt.OutputPath))
            return false;

        if (!File.Exists(opt.InputPath))
            return false;

        //Queue monitor unsortedChunks/unmergedChunks count
        var monitorTask = Task.Run(MonitorQueue);

        //Task to split stream by chunks
        var splitTask = Task.Run(SplitInputFile);

        //Task Pool to preSort chunks
        var sortTaskPool = new Task[opt.PoolSize];

        //Task Pool to preSort chunks
        var msortTaskPool = new Task[opt.PoolSize];

        for (var i = 0; i < opt.PoolSize; i++)
        {
            sortTaskPool[i] = Task.Run(SortChunks);
            msortTaskPool[i] = Task.Run(() =>
            {
                SortMerge();
            });
        }

        Task.WaitAll(splitTask);
        Task.WaitAll(sortTaskPool);
        Task.WaitAll(msortTaskPool);

        //final merge
        SortMerge(isSingleThread: true);

        return true;
    }

    private void MonitorQueue()
    {
        Console.WriteLine();
        var val = unsortedChunks.Count + unmergedChunks.Count;
        while (true)
        {
            if (val != unsortedChunks.Count + unmergedChunks.Count)
            {
                Console.Write($"QUEUE sort/merge: {unsortedChunks.Count}/{unmergedChunks.Count}        \r");
                val = unsortedChunks.Count + unmergedChunks.Count;
            }
        }
    }

    private void SplitInputFile()
    {
        Thread.CurrentThread.Name = "SplitInputFile";
        var lineCounter = 0;
        var idx = 0;
        var sb = new StringBuilder();

        foreach (var line in File.ReadLines(opt.InputPath))
        {
            if (!string.IsNullOrEmpty(line))
                sb.AppendLine(line);

            if (++lineCounter % opt.MaxLinesPerChunk == 0)
            {   //store buffer into chunk
                lineCounter = 0;
                SaveChunk(sb, idx++);
            }
        }

        //If smaller than buffer size
        if (sb.Length > 0)
            SaveChunk(sb);

        lock (_lock)
        {
            IsSplitDone = true;
        }
    }

    private void SaveChunk(StringBuilder sb, int idx = -1)
    {
        var chunkFilePath = Path.Combine(opt.TempDir, $"{opt.ChunkBaseName}{idx}.chunk");
        using (var writeStream = File.Create(chunkFilePath))
        using (var writer = new StreamWriter(writeStream))
        {
            writer.WriteLine(sb);
        }
        unsortedChunks.Enqueue(chunkFilePath);
        //Console.WriteLine($"Split-SaveChunk_{Thread.CurrentThread.ManagedThreadId} {chunkFilePath}. Total unsorted: {unsortedChunks.Count}");
        sb.Clear();
    }

    private void SortChunks()
    {
        string? chunkFilePath;
        bool isSplitDone = false;

        while (unsortedChunks.TryDequeue(out chunkFilePath) || !isSplitDone) //while queue has chunks to sort
        {
            if (string.IsNullOrEmpty(chunkFilePath))
            {
                lock (_lock)
                {
                    isSplitDone = IsSplitDone;
                }
                continue;
            }

            var unsortedLines = File.ReadLines(chunkFilePath).ToArray<string>();
            var sortedLines = LineSorter.Sort(unsortedLines, 0, unsortedLines.Length - 1);
            File.WriteAllLines(chunkFilePath, sortedLines);

            // Console.WriteLine($"SortChunks_{Thread.CurrentThread.ManagedThreadId} {chunkFilePath}. Total unmerged: {unmergedChunks.Count}");
            //Console.WriteLine($"SortChunks_{Thread.CurrentThread.ManagedThreadId} s/m: {unsortedChunks.Count}/{unmergedChunks.Count}");

            unmergedChunks.Enqueue(chunkFilePath);
        }


        lock (_lock)
        {
            IsSortDone = true;
        }
    }

    private void SortMerge(bool isSingleThread = false)
    {
        var lc = new LineComparer();
        string? chunk1, chunk2;
        var isSortDone = false;

        while (unmergedChunks.TryDequeue(out chunk1) || !isSortDone)
        {
            lock (_lock)
            {
                isSortDone = IsSortDone;
            }

            //Queue is empty&clean
            if (!isSortDone && string.IsNullOrEmpty(chunk1))
                continue;

            //Queue has 1 element. To be filled
            if (!isSortDone && !string.IsNullOrEmpty(chunk1) && !unmergedChunks.TryPeek(out chunk2))
            {
                unmergedChunks.Enqueue(chunk1);
                continue;
            }

            //Queue has 1 last element. => Save results&exit
            if (isSortDone && !string.IsNullOrEmpty(chunk1) && !unmergedChunks.TryPeek(out chunk2))
            {
                if (isSingleThread)
                {
                    Console.WriteLine($"Sort complete! Saving file...");
                    File.Move(chunk1, opt.OutputPath);
                }
                else
                {
                    unmergedChunks.Enqueue(chunk1);
                }
                break;
            }

            //Queue has > 1 elements to be merged
            if (unmergedChunks.TryDequeue(out chunk2) && !string.IsNullOrEmpty(chunk1))
            {
                var chunkMergeName = Path.Combine(opt.TempDir, $"{Guid.NewGuid()}.chunkmerge");

                // Console.WriteLine($"Merge_{Thread.CurrentThread.ManagedThreadId} 1={chunk1} 2={chunk2}. Total unmerged: {unmergedChunks.Count}");

                using (var chunk1sr = new StreamReader(chunk1))
                using (var chunk2sr = new StreamReader(chunk2))
                {
                    var lineL = chunk1sr.ReadLine();
                    var lineR = chunk2sr.ReadLine();

                    using (var mergedChunksr = new StreamWriter(chunkMergeName))
                    {
                        while (!string.IsNullOrEmpty(lineL) || !string.IsNullOrEmpty(lineR))
                        {
                            if (lc.Compare(lineL, lineR) < 0)
                            {
                                mergedChunksr.WriteLine(lineL);
                                lineL = chunk1sr.ReadLine();
                            }
                            else if (!string.IsNullOrEmpty(lineR))
                            {
                                mergedChunksr.WriteLine(lineR);
                                lineR = chunk2sr.ReadLine();
                            }

                        }
                    }
                    unmergedChunks.Enqueue(chunkMergeName);
                }
                //delete merged chunks 
                File.Delete(chunk1);
                File.Delete(chunk2);
            }
        }
    }

    private void PrepareTempDir()
    {
        if (Directory.Exists(opt.TempDir))
        {
            Directory.Move(opt.TempDir, $"{opt.TempDir}_del");
            Directory.Delete($"{opt.TempDir}_del", recursive: true);
            Directory.CreateDirectory(opt.TempDir);
        }
        else
        {
            Directory.CreateDirectory(opt.TempDir);
        }
    }
}