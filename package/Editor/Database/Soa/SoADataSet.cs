namespace Unity.MemoryProfiler.Editor.Database.Soa
{
    public class SoaDataSet
    {
        public SoaDataSet(long adataCount, long achunkSize)
        {
            m_dataCount = adataCount;
            m_chunkSize = achunkSize;
        }

        public long m_dataCount;
        public long m_chunkSize = 16;
    }
}
