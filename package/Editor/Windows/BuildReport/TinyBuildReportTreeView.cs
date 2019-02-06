

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyBuildReportTreeView : TinyTreeView<TinyTreeState, TinyBuildReportTreeModel>
    {
        public enum ColumnType
        {
            Name,
            TotalCompressed,
            TotalRaw
        }

        public TinyBuildReportTreeView(TinyTreeState state, TinyBuildReportTreeModel model) : base(state, model)
        {
            extraSpaceBeforeIconAndLabel = 18f;
            columnIndexForTreeFoldouts = 0;
        }

        public static MultiColumnHeaderState CreateMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 500,
                    minWidth = 140,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Est. Compressed Size", "Estimated Compressed Size: represents each item gzip compressed size, before conversion and/or minification."),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 140,
                    minWidth = 140,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Raw Size", "Raw Size: represents each item uncompressed size, before conversion and/or minification."),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 140,
                    minWidth = 140,
                    autoResize = true,
                    allowToggleVisibility = false
                },
            };

            UnityEngine.Assertions.Assert.AreEqual(columns.Length, Enum.GetValues(typeof(ColumnType)).Length,
                "Number of columns should match number of enum values: You probably forgot to update one of them.");

            return new MultiColumnHeaderState(columns)
            {
                sortedColumnIndex = 1 // Default is sort by name
            };
        }

        protected override TreeViewItem BuildRoot()
        {
            var treeRoot = new TreeViewItem { id = 0, depth = -1 };
            var buildReport = Model.GetBuildReport();
            if (buildReport != null)
            {
                AddChildrenRecursive(treeRoot, buildReport.Root, 0);
            }
            return treeRoot;
        }

        private void AddChildrenRecursive(TreeViewItem treeNode, TinyBuildReport.TreeNode buildReportNode, int depth)
        {
            if (treeNode == null || buildReportNode == null || buildReportNode.Item.Name == null)
            {
                return;
            }

            var treeItem = new TinyBuildReportTreeViewItem(buildReportNode)
            {
                depth = depth,
                id = Model.GetNewId
            };
            if (buildReportNode.Children.Count > 0)
            {
                foreach (var child in buildReportNode.Children)
                {
                    AddChildrenRecursive(treeItem, child, depth + 1);
                }
            }
            treeNode.AddChild(treeItem);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var buildReportTreeViewItem = args.item as TinyBuildReportTreeViewItem;
            if (buildReportTreeViewItem != null)
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), buildReportTreeViewItem, (ColumnType)args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, TinyBuildReportTreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (columnType)
            {
                case ColumnType.Name:
                    var rect = cellRect;
                    var indent = GetContentIndent(item);
                    rect.x += indent;
                    rect.width -= indent;
                    GUI.Label(rect, item.Name);
                    break;

                case ColumnType.TotalCompressed:
                    GUI.Label(cellRect, item.TotalCompressedLabel);
                    break;

                case ColumnType.TotalRaw:
                    GUI.Label(cellRect, item.TotalRawLabel);
                    break;
            }
        }

        protected override List<TreeViewItem> SortRows(List<TreeViewItem> rows)
        {
            var items = rows.Cast<TinyBuildReportTreeViewItem>();

            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);
            switch ((ColumnType)multiColumnHeader.sortedColumnIndex)
            {
                case ColumnType.Name:
                    items = ascending ? items.OrderBy(i => i.Name) : items.OrderByDescending(i => i.Name);
                    break;

                case ColumnType.TotalCompressed:
                    items = ascending ? items.OrderBy(i => i.TotalCompressedPercent) : items.OrderByDescending(i => i.TotalCompressedPercent);
                    break;

                case ColumnType.TotalRaw:
                    items = ascending ? items.OrderBy(i => i.TotalRawPercent) : items.OrderByDescending(i => i.TotalRawPercent);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return items.Cast<TreeViewItem>().ToList();
        }

        protected override void ContextClicked()
        {
            // No implementation to hide the context menu
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SingleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as TinyBuildReportTreeViewItem;
            if (item != null && item.Object != null)
            {
                EditorGUIUtility.PingObject(item.Object);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as TinyBuildReportTreeViewItem;
            if (item.Object)
            {
                Selection.objects = new[] { item.Object };
            }
        }
    }
}

