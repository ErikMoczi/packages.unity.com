using UnityEditor.IMGUI.Controls;

namespace Unity.Tiny
{
    internal static class TreeViewUtilityBridge
    {
        internal static TreeViewItem FindItem(int id, TreeViewItem searchFromThisItem)
        {
            return TreeViewUtility.FindItem(id, searchFromThisItem);
        }
    }
}