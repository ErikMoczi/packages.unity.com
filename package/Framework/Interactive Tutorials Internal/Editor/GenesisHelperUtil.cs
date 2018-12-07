using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    public class GenesisHelperUtils
    {
        [MenuItem("Tutorials/Genesis/Print all statuses")]
        public static void PrintAllStatuses()
        {
            GenesisHelper.PrintAllTutorials();
        }

        [MenuItem("Tutorials/Genesis/Clear all statuses")]
        public static void ClearAllStatuses()
        {
            if (EditorUtility.DisplayDialog("", "Do you want to clear progress of every tutorial?", "Clear", "Cancel"))
            {
                GenesisHelper.GetAllTutorials((r) =>
                    {
                        var ids = r.Select(a => a.lessonId);
                        foreach (var id in ids)
                        {
                            GenesisHelper.LogTutorialStatusUpdate(id, " ");
                        }
                        Debug.Log("Lesson statuses cleared");
                    });
            }
        }
    }
}
