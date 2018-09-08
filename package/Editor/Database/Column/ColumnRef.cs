namespace Unity.MemoryProfiler.Editor.Database
{
    public struct ColumnRef
    {
        public string tableName;
        public string columnName;

        public static ColumnRef kInvalidRef = new ColumnRef {tableName = "InvalidName", columnName = "InvalidName" };
    }
}
