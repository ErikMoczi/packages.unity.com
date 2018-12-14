using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Localization;

public class LocaleGeneratorListView : TreeView
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
        Code,
        Id,
    }

    List<LocaleTreeViewItem> m_Items = new List<LocaleTreeViewItem>();

    public List<LocaleIdentifier> items
    {
        set
        {
            m_Items.Clear();
            for (int i = 0; i < value.Count; ++i)
            {
                var item = new LocaleTreeViewItem() { id = i, identifier = value[i] };
                item.displayName = item.identifier.cultureInfo.EnglishName + " " + item.identifier.code + " " + item.identifier.id;                
                m_Items.Add(item);
            }
            Reload();
        }
    }

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
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Id"),
                    minWidth = 25,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true
                },
            };

        var multiColState = new MultiColumnHeaderState(columns);
        multiColumnHeader = new MultiColumnHeader(multiColState);
        multiColumnHeader.ResizeToFit();
        multiColumnHeader.sortingChanged += (multiColumnHeader) => Reload(); ;
    }

    protected override TreeViewItem BuildRoot()
    {
        TreeViewItem root = new TreeViewItem(-1, -1, "root");
        if(m_Items == null)
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
                    m_Items.Sort((x, y) => acend ? string.Compare(y.identifier.code, x.identifier.code) : string.Compare(x.identifier.code, y.identifier.code));
                    break;
                case Column.Id:
                    m_Items.Sort((x, y) => acend ? x.identifier.id - y.identifier.id : y.identifier.id - x.identifier.id);
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
                item.enabled = EditorGUI.Toggle(toggleRect, item.enabled);
                cellRect.xMin += k_ToggleWidth;
                GUI.Label(cellRect, item.identifier.cultureInfo.EnglishName);
                break;

            case Column.Code:
                GUI.Label(cellRect, item.identifier.code);
                break;

            case Column.Id:
                GUI.Label(cellRect, item.identifier.id.ToString());
                break;
        }
    }

    public void SelectLocales(bool enable)
    {
        foreach (LocaleTreeViewItem row in GetRows())
        {
            row.enabled = enable;
        }
    }

    public List<LocaleIdentifier> GetSelectedLocales()
    {
        var selectedLocales = new List<LocaleIdentifier>();
        foreach (var item in m_Items)
        {
            if(item.enabled)
                selectedLocales.Add(item.identifier);
        }
        return selectedLocales;
    }
}