namespace SorterCore;

public class FileSorter
{
    object _lock = new();
    private const string TEMP_DIR = ".sortchunks";
    private const string CHUNK_BASE_NAME = "chunk_";
    public int LINES_PER_CHUNK = 1 * (128 * 1024 / 20);//1line ~ 20Bytes. N (MBytes)
    public int POOL_SIZE = 15;
    private volatile bool IsSplitDone = false;
    private volatile bool IsSortDone = false;
    private ConcurrentQueue<string> unsortedChunks = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> unmergedChunks = new ConcurrentQueue<string>();

    public string OutputPath { get; }
    public string InputPath { get; }

    public FileSorter(string inputPath, string outputPath)
    {
        InputPath = inputPath;
        OutputPath = outputPath;
        PrepareTempDir();
    }

    public bool SortFile()
    {
        if (!File.Exists(InputPath))
            return false;

        //Task to split stream by chunks
        var splitTask = Task.Run(SplitInputFile);

        //Task Pool to preSort chunks
        var sortTaskPool = new Task[POOL_SIZE];
        //Task Pool to preSort chunks
        var msortTaskPool = new Task[POOL_SIZE];

        for (var i = 0; i < POOL_SIZE; i++)
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
        SortMerge(true);

        return true;
    }
    private void SplitInputFile()
    {
        Thread.CurrentThread.Name = "SplitInputFile";
        var lineCounter = 0;
        var idx = 0;
        var sb = new StringBuilder();

        foreach (var line in File.ReadLines(InputPath))
        {
            if (!string.IsNullOrEmpty(line))
                sb.AppendLine(line);

            if (++lineCounter % LINES_PER_CHUNK == 0)
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
        var chunkFilePath = Path.Combine(TEMP_DIR, $"{CHUNK_BASE_NAME}{idx}.chunk");
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
                    File.Move(chunk1, OutputPath);
                }
                else
                {
                    unmergedChunks.Enqueue(chunk1);
                }
                break;
            }

            //Queue has > 1 elements to be merged
            if (unmergedChunks.TryDequeue(out chunk2))
            {
                var chunkMergeName = Path.Combine(TEMP_DIR, $"{Guid.NewGuid()}.chunkmerge");

                Console.WriteLine($"Merge_{Thread.CurrentThread.ManagedThreadId} {chunk1} {chunk2}. Total unmerged: {unmergedChunks.Count}");

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
                chunk1 = null;
                chunk2 = null;
            }
        }
    }


    private void PrepareTempDir()
    {
        if (Directory.Exists(TEMP_DIR))
        {
            Directory.Move(TEMP_DIR, $"{TEMP_DIR}_del");
            Directory.Delete($"{TEMP_DIR}_del", recursive: true);
            Directory.CreateDirectory(TEMP_DIR);
        }
        else
        {
            Directory.CreateDirectory(TEMP_DIR);
        }
    }
}