﻿using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Localization;

namespace UnityEditor.Localization
{
    class LocaleGeneratorListView : TreeView
    {
        const float k_ToggleWidth = 16;

        class LocaleTreeViewItem : TreeViewItem
        {
            public bool enabled;
            public LocaleIdentifier identifier;
        }

        enum Column
        {
            Name,
            Code
        }

        List<LocaleTreeViewItem> m_Items = new List<LocaleTreeViewItem>();

        /// <summary>
        /// Full list of Locales available to select from.
        /// </summary>
        public List<LocaleIdentifier> Items
        {
            set
            {
                m_Items.Clear();
                for (int i = 0; i < value.Count; ++i)
                {
                    var item = new LocaleTreeViewItem() { id = i, identifier = value[i] };
                    item.displayName = item.identifier.CultureInfo.EnglishName + " " + item.identifier.Code;
                    m_Items.Add(item);
                }

                Reload();
            }
        }

        /// <summary>
        /// How many Locales are selected?
        /// </summary>
        public int SelectedCount { get; set; }

        public LocaleGeneratorListView() :
            base(new TreeViewState())
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;

            var columns = new[]
            {
                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Name"),
                    minWidth = 100,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Code"),
                    minWidth = 25,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true
                }
            };

            var multiColState = new MultiColumnHeaderState(columns);
            multiColumnHeader = new MultiColumnHeader(multiColState);
            multiColumnHeader.ResizeToFit();
            multiColumnHeader.sortingChanged += (multiColumnHeader) => Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem(-1, -1, "root");
            if (m_Items == null)
                return root;

            if (multiColumnHeader.sortedColumnIndex >= 0)
            {
                bool acend = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);
                switch ((Column)multiColumnHeader.sortedColumnIndex)
                {
                    case Column.Name:
                        m_Items.Sort((x, y) => acend ? string.Compare(y.displayName, x.displayName) : string.Compare(x.displayName, y.displayName));
                        break;
                    case Column.Code:
                        m_Items.Sort((x, y) => acend ? string.Compare(y.identifier.Code, x.identifier.Code) : string.Compare(x.identifier.Code, y.identifier.Code));
                        break;
               }
            }

            SetupParentsAndChildrenFromDepths(root, m_Items.ConvertAll(item => (TreeViewItem)item));
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var ltci = args.item as LocaleTreeViewItem;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), ltci, (Column)args.GetColumn(i));
            }
        }

        void CellGUI(Rect cellRect, LocaleTreeViewItem item, Column col)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (col)
            {
                case Column.Name:
                    cellRect.xMin += GetContentIndent(item);
                    var toggleRect = new Rect(cellRect.x, cellRect.y, k_ToggleWidth, cellRect.height);
                    EditorGUI.BeginChangeCheck();
                    item.enabled = EditorGUI.Toggle(toggleRect, item.enabled);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (item.enabled)
                            SelectedCount++;
                        else
                            SelectedCount--;
                    }
                    cellRect.xMin += k_ToggleWidth;
                    GUI.Label(cellRect, item.identifier.CultureInfo.EnglishName);
                    break;

                case Column.Code:
                    GUI.Label(cellRect, item.identifier.Code);
                    break;
            }
        }

        public void SelectLocales(bool enable)
        {
            foreach (var treeViewItem in GetRows())
            {
                var row = (LocaleTreeViewItem)treeViewItem;
                row.enabled = enable;
            }
        }

        public List<LocaleIdentifier> GetSelectedLocales()
        {
            var selectedLocales = new List<LocaleIdentifier>();
            foreach (var item in m_Items)
            {
                if (item.enabled)
                    selectedLocales.Add(item.identifier);
            }

            return selectedLocales;
        }
    }
}