using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.WriteTypes
{
    public interface IWriteOperation
    {
        WriteCommand command { get; set; }

        BuildUsageTagSet usageSet { get; set; }

        BuildReferenceMap referenceMap { get; set; }

        WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage);
    }
}
