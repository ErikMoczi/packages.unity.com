namespace Unity.MemoryProfiler.Editor.Database.Operation
{
    public enum SortOrder
    {
        None,
        Ascending,
        Descending,
    }
    public class SortOrderString
    {
        public static SortOrder StringToSortOrder(string s, SortOrder defaultOrder = SortOrder.None)
        {
            if (s == null) return defaultOrder;
            switch (s.ToLower())
            {
                case "ascending":
                case "asc":
                    return SortOrder.Ascending;
                case "descending":
                case "des":
                    return SortOrder.Descending;
            }
            return defaultOrder;
        }
    }
}
