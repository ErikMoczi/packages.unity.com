using System;

namespace Unity.MemoryProfiler.Editor.Database
{
    public class MetaColumn
    {
        public Type type;

        public MetaColumn(string aName, string aDisplayName, Type aType, bool aIsPrimaryKey, Operation.Grouping.IGroupAlgorithm groupAlgo, Operation.Grouping.IMergeAlgorithm mergeAlgo)
        {
            index = 0;
            name = aName;
            displayName = aDisplayName;
            type = aType;
            isPrimaryKey = aIsPrimaryKey;
            defaultMergeAlgorithm = mergeAlgo;
            defaultGroupAlgorithm = groupAlgo;
        }

        public MetaColumn(string aName, string aDisplayName, Type aType, ColumnRef aReference, Operation.Grouping.IGroupAlgorithm groupAlgo, Operation.Grouping.IMergeAlgorithm mergeAlgo)
        {
            index = 0;
            name = aName;
            displayName = aDisplayName;
            type = aType;
            isReference = true;
            reference = aReference;
            defaultMergeAlgorithm = mergeAlgo;
            defaultGroupAlgorithm = groupAlgo;
        }

        public MetaColumn(MetaColumn mc)
        {
            index = 0;
            name = mc.name;
            isPrimaryKey = mc.isPrimaryKey;
            displayName = mc.displayName;
            type = mc.type;
            isReference = mc.isReference;
            reference = mc.reference;
            defaultMergeAlgorithm = mc.defaultMergeAlgorithm;
            defaultGroupAlgorithm = mc.defaultGroupAlgorithm;
        }

        public int index;
        public string name;
        public string displayName;
        public int displayDefaultWidth = 100;

        public bool isPrimaryKey = false;
        public bool isReference = false;
        public ColumnRef reference;


        public Operation.Grouping.IGroupAlgorithm defaultGroupAlgorithm;
        public Operation.Grouping.IMergeAlgorithm defaultMergeAlgorithm;
    }
}
