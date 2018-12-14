using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization
{
    class LocalesCollectionListView : TreeView
    {
        SerializedProperty m_Locales;

        enum Column
        {
            Reference,
            Name,
            Code,
            Id,
            Fallback
        }

        public LocalesCollectionListView(SerializedProperty locales) :
            base(new TreeViewState())
        {
            m_Locales = locales;

            showBorder = true;
            showAlternatingRowBackgrounds = true;

            var columns = new[]
            {
                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Locale"),
                    minWidth = 45,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false
                },

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
                    minWidth = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Id"),
                    minWidth = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true
                },

                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Fallback"),
                    minWidth = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false
                },
            };

            var multiColState = new MultiColumnHeaderState(columns);
            multiColState.visibleColumns = new[] { (int)Column.Reference, (int)Column.Name, (int)Column.Fallback };
            multiColumnHeader = new MultiColumnHeader(multiColState);
            multiColumnHeader.ResizeToFit();
            multiColumnHeader.sortingChanged += (multiColumnHeader) => Reload();;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem(-1, -1, "root");
            var items = new List<TreeViewItem>();

            for (int i = 0; i < m_Locales.arraySize; ++i)
            {
                var element = m_Locales.GetArrayElementAtIndex(i);
                items.Add(new SerializedLocaleItem(element){ id = i });
            }

            if (multiColumnHeader.sortedColumnIndex >= 0)
            {
                bool acend = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);
                switch ((Column)multiColumnHeader.sortedColumnIndex)
                {
                    case Column.Name:
                        items.Sort((x, y) =>
                        {
                            var a = (SerializedLocaleItem)x;
                            var b = (SerializedLocaleItem)y;
                            return acend ? string.Compare(b.name, a.name) : string.Compare(a.name, b.name);
                        });
                        break;
                    case Column.Code:
                        items.Sort((x, y) =>
                        {
                            var a = (SerializedLocaleItem)x;
                            var b = (SerializedLocaleItem)y;
                            return acend ? string.Compare(b.identifierCode, a.identifierCode) : string.Compare(a.identifierCode, b.identifierCode);
                        });
                        break;
                    case Column.Id:
                        items.Sort((x, y) =>
                        {
                            var a = (SerializedLocaleItem)x;
                            var b = (SerializedLocaleItem)y;
                            return acend ? b.identifierId - a.identifierId : a.identifierId - b.identifierId;
                        });
                        break;
                }
            }

            for (int i = 0; i < items.Count; ++i)
            {
                items[i].id = i;
            }

            SetupParentsAndChildrenFromDepths(root, items);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var sli = args.item as SerializedLocaleItem;
            if (sli.serializedObject != null)
                sli.serializedObject.Update();

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), sli, (Column)args.GetColumn(i));
            }

            if (sli.serializedObject != null)
                sli.serializedObject.ApplyModifiedProperties();
        }

        void CellGUI(Rect cellRect, SerializedLocaleItem item, Column col)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (col)
            {
                case Column.Reference:
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.BeginProperty(cellRect, GUIContent.none, item.property);
                    var newSelection = EditorGUI.ObjectField(cellRect, item.reference, typeof(Locale), false) as Locale;
                    if (EditorGUI.EndChangeCheck())
                        item.reference = newSelection;
                    EditorGUI.EndProperty();
                    break;

                case Column.Name:
                    if(item.nameProp != null)
                        EditorGUI.PropertyField(cellRect, item.nameProp, GUIContent.none);
                    break;

                case Column.Code:
                    if (item.identifierCodeProp != null)
                        EditorGUI.PropertyField(cellRect, item.identifierCodeProp, GUIContent.none);
                    break;

                case Column.Id:
                    if (item.identifierIdProp != null)
                        EditorGUI.PropertyField(cellRect, item.identifierIdProp, GUIContent.none);
                    break;

                case Column.Fallback:
                    if (item.fallbackProp != null)
                        EditorGUI.PropertyField(cellRect, item.fallbackProp, GUIContent.none);
                    break;
            }
        }
    }
}

