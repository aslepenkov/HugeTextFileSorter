public class FileSorterOptions
{
    public bool CreateNew { get; set; } = true;
    public int MaxLinesPerChunk { get; set; } = 100000;//1line ~ 20Bytes
    public int FileSizeMByte { get; set; } = 1000;
    public int PoolSize { get; set; } = 100;
    public string? OutputPath { get; set; }
    public string? InputPath { get; set; } 
    public string TempDir => ".sortchunks";
    public string ChunkBaseName => "chunk_";
}