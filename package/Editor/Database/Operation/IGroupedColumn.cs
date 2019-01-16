namespace Unity.MemoryProfiler.Editor.Database.Operation
{
    internal interface IGroupedColumn
    {
        void Initialize(GroupedTable table, Column column, int columnIndex, Grouping.IMergeAlgorithm algo, bool isGroup);
    }
}
