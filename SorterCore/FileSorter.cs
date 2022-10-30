namespace SorterCore;

public class FileSorter
{
    private const string TEMP_DIR = ".sortchunks";
    private const string CHUNK_BASE_NAME = "chunk_";
    private const int LINES_PER_CHUNK = 10 * (1024 * 1024 / 20);//1line ~ 20Bytes. N (MBytes)
    private volatile bool IsSplitDone = false;
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
        var sortTaskPool = new Task[100];
        for (var i = 0; i < sortTaskPool.Length; i++)
            sortTaskPool[i] = Task.Run(SortChunks);

        Task.WaitAll(splitTask);
        Task.WaitAll(sortTaskPool);

        //SortMerge chunks
        SortMerge();

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

        IsSplitDone = true;
    }

    private void SaveChunk(StringBuilder sb, int idx = -1)
    {
        var chunkPath = Path.Combine(TEMP_DIR, $"{CHUNK_BASE_NAME}{idx}.chunk");
        using (var writeStream = File.Create(chunkPath))
        using (var writer = new StreamWriter(writeStream))
        {
            writer.WriteLine(sb);
            unsortedChunks.Enqueue(chunkPath);
        }
        sb.Clear();
    }

    private void SortChunks()
    {
        Thread.CurrentThread.Name = "SortChunks";

        string? chunkFilePath;
        Thread.Sleep(2000);//TODO FIX

        while (unsortedChunks.TryDequeue(out chunkFilePath)) //while queue has chunks to sort
        {
            var unsortedLines = File.ReadLines(chunkFilePath).ToArray<string>();
            var sortedLines = LineSorter.Sort(unsortedLines, 0, unsortedLines.Length - 1);
            File.WriteAllLines(chunkFilePath, sortedLines);

            unmergedChunks.Enqueue(chunkFilePath);
        }
    }

    private void SortMerge()
    {
        var lc = new LineComparer();
        string? chunk1, chunk2;

        while (unmergedChunks.TryDequeue(out chunk1))//|| !IsSplitDone)
        {
            if (unmergedChunks.TryDequeue(out chunk2))
            {
                var chunkMergeName = Path.Combine(TEMP_DIR, $"{Guid.NewGuid()}.chunkmerge");
                unmergedChunks.Enqueue(chunkMergeName);

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
                }
                //delete merged chunks 
                File.Delete(chunk1);
                File.Delete(chunk2);
                chunk1 = null;
                chunk2 = null;
            }
            else //last chunk. Save results
            {
                File.Move(chunk1, OutputPath);
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