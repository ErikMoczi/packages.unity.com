namespace Unity.MemoryProfiler.Editor.Database.Soa
{
    internal struct SoaDataSet
    {
        public SoaDataSet(long dataCount, long chunkSize)
        {
            DataCount = dataCount;
            ChunkSize = chunkSize;
        }

        public readonly long DataCount;
        public readonly long ChunkSize;
    }
}
