

using System;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal class TinyBuildReportTreeViewItem : TreeViewItem
    {
        private TinyBuildReport.TreeNode Node { get; set; }

        public string Name
        {
            get { return Node.Item.Name; }
        }

        public long RawSize
        {
            get { return Node.Item.Size; }
        }

        public long TotalRawSize
        {
            get { return Node.Root.Item.Size; }
        }

        public float TotalRawPercent
        {
            get { return TotalRawSize != 0 ? (float)RawSize / TotalRawSize : 0f; }
        }

        public string TotalRawLabel
        {
            get { return string.Format("{0:F1}% ({1})", TotalRawPercent * 100f, HumanReadableSize(RawSize)); }
        }

        public long CompressedSize
        {
            get { return Node.Item.CompressedSize; }
        }

        public long TotalCompressedSize
        {
            get { return Node.Root.Item.CompressedSize; }
        }

        public float TotalCompressedPercent
        {
            get { return TotalCompressedSize != 0 ? (float)CompressedSize / TotalCompressedSize : 0f; }
        }

        public string TotalCompressedLabel
        {
            get { return string.Format("{0:F1}% ({1})", TotalCompressedPercent * 100f, HumanReadableSize(CompressedSize)); }
        }

        private static string HumanReadableSize(long size)
        {
            const int unit = 1024;
            if (size < unit)
            {
                return size + " B";
            }
            int exp = (int)(Math.Log(size) / Math.Log(unit));
            return string.Format("{0:F1} {1}B", size / Math.Pow(unit, exp), "KMGTPE"[exp - 1]);
        }

        public Object Object
        {
            get { return Node.Item.Object; }
        }

        public TinyBuildReportTreeViewItem(TinyBuildReport.TreeNode node)
        {
            Node = node;
        }
    }
}

