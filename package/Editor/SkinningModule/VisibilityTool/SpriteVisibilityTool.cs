using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal interface ISpriteVisibilityToolModel
    {
        ISpriteVisibilityToolView view { get; }
        CharacterCache character { get; }
        bool previousVisibility { get; set; }
        bool allVisibility { get; set; }
        SkinningMode mode { get; }
        bool hasCharacter { get; }
        UndoScope UndoScope(string description);
        SpriteCache selectedSprite { get; }
    }

    internal interface ISpriteVisibilityToolView
    {
        void Setup();
        void SetSelection(SpriteCache sprite);
    }

    internal class SpriteVisibilityToolData : CacheObject
    {
        [SerializeField]
        bool m_AllVisibility = true;
        bool m_PreviousVisibility = true;

        public bool allVisibility
        {
            get { return m_AllVisibility; }
            set { m_PreviousVisibility = m_AllVisibility = value; }
        }

        public bool previousVisibility
        {
            get { return m_PreviousVisibility; }
            set { m_PreviousVisibility = value; }
        }
    }

    internal class SpriteVisibilityToolController
    {
        ISpriteVisibilityToolModel m_Model;
        SkinningEvents m_Events;
        public event Action OnAvailabilityChangeListeners = () => {};

        public SpriteVisibilityToolController(ISpriteVisibilityToolModel model, SkinningEvents events)
        {
            m_Model = model;
            m_Events = events;
            m_Events.skinningModeChanged.AddListener(OnViewModeChanged);
        }

        public void Activate()
        {
            m_Events.selectedSpriteChanged.AddListener(OnSpriteSelectedChanged);
            m_Model.view.Setup();
            m_Model.view.SetSelection(m_Model.selectedSprite);
            if (m_Model.previousVisibility != m_Model.allVisibility)
            {
                SetAllCharacterSpriteVisibility();
                m_Model.previousVisibility = m_Model.allVisibility;
            }
        }

        public void Deactivate()
        {
            m_Events.selectedSpriteChanged.RemoveListener(OnSpriteSelectedChanged);
        }

        public void Dispose()
        {
            m_Events.skinningModeChanged.RemoveListener(OnViewModeChanged);
        }

        void OnViewModeChanged(SkinningMode mode)
        {
            OnAvailabilityChangeListeners();
            if (isAvailable && m_Model.previousVisibility != m_Model.allVisibility)
                SetAllCharacterSpriteVisibility();
        }

        private void OnSpriteSelectedChanged(SpriteCache sprite)
        {
            m_Model.view.SetSelection(sprite);
        }

        public bool isAvailable
        {
            get { return m_Model.mode == SkinningMode.Character; }
        }

        void SetAllCharacterSpriteVisibility()
        {
            if (m_Model.hasCharacter)
            {
                using (m_Model.UndoScope(TextContent.spriteVisibility))
                {
                    var parts = m_Model.character.parts;

                    foreach (var part in parts)
                        part.isVisible = m_Model.allVisibility;
                }
            }
        }

        public void SetAllVisibility(bool visibility)
        {
            using (m_Model.UndoScope(TextContent.spriteVisibility))
            {
                m_Model.allVisibility = visibility;
                SetAllCharacterSpriteVisibility();
            }
        }

        public bool GetAllVisibility()
        {
            return m_Model.allVisibility;
        }

        public List<TreeViewItem> BuildTreeView()
        {
            var rows = new List<TreeViewItem>();
            var character = m_Model.character;
            if (character != null)
            {
                var parts = character.parts;
                foreach (var part in parts)
                {
                    var item = CreateTreeViewItem(part);
                    rows.Add(item);
                }
            }
            return rows;
        }

        private TreeViewItem CreateTreeViewItem(CharacterPartCache part)
        {
            var name = part.sprite.name;
            return new TreeViewItemBase<CharacterPartCache>(part.GetInstanceID(), 0, name, part);
        }

        public bool GetCharacterPartVisibility(TreeViewItem item)
        {
            var i = item as TreeViewItemBase<CharacterPartCache>;
            if (i != null)
                return i.customData.isVisible;
            return false;
        }

        public void SetCharacterPartVisibility(TreeViewItem item, bool visible, bool isolate)
        {
            var i = item as TreeViewItemBase<CharacterPartCache>;
            if (i != null)
            {
                var characterPart = i.customData;
                var character = m_Model.character;
                using (m_Model.UndoScope(TextContent.spriteVisibility))
                {
                    if (isolate)
                    {
                        foreach (var cpart in character.parts)
                        {
                            cpart.isVisible = visible;
                        }
                        characterPart.isVisible = !visible;
                    }
                    else
                    {
                        characterPart.isVisible = visible;
                    }
                }
            }
        }

        public void SetSelectedSprite(IList<TreeViewItem> rows, IList<int> selectedIds)
        {
            SpriteCache newSelected = null;
            if (selectedIds.Count > 0)
            {
                var selected = rows.FirstOrDefault(x => ((TreeViewItemBase<CharacterPartCache>)x).customData.GetInstanceID() == selectedIds[0]) as TreeViewItemBase<CharacterPartCache>;
                if (selected != null)
                    newSelected = selected.customData.sprite;
            }

            if (newSelected != null)
            {
                using (m_Model.UndoScope(TextContent.selectionChange))
                {
                    m_Events.selectedSpriteChanged.Invoke(newSelected);
                }
            }
        }

        public int GetTreeViewSelectionID(IList<TreeViewItem> rows, SpriteCache sprite)
        {
            for (int i = 0; rows != null && i < rows.Count; ++i)
            {
                var r = (TreeViewItemBase<CharacterPartCache>)rows[i];
                if (r.customData.sprite == sprite)
                {
                    return r.id;
                }
            }
            return 0;
        }
    }

    internal class SpriteVisibilityTool : IVisibilityTool, ISpriteVisibilityToolModel
    {
        SpriteVisibilityToolView m_View;
        SpriteVisibilityToolController m_Controller;

        private SpriteVisibilityToolData m_Data;
        private SkinningCache m_SkinningCache;
        public SkinningCache skinningCache { get { return m_SkinningCache; } }

        public SpriteVisibilityTool(SkinningCache s)
        {
            m_SkinningCache = s;
            m_Data = skinningCache.CreateCache<SpriteVisibilityToolData>();
            m_Controller = new SpriteVisibilityToolController(this, skinningCache.events);
            m_View = new SpriteVisibilityToolView()
            {
                GetController = () => m_Controller
            };
        }

        public void Setup()
        {}

        public void Dispose()
        {
            m_Controller.Dispose();
        }

        public VisualElement view { get { return m_View; } }
        public string name { get { return L10n.Tr(TextContent.sprite); } }

        public void Activate()
        {
            m_Controller.Activate();
        }

        public void Deactivate()
        {
            m_Controller.Deactivate();
        }

        public bool isAvailable
        {
            get { return m_Controller.isAvailable; }
        }


        public void SetAvailabilityChangeCallback(Action callback)
        {
            m_Controller.OnAvailabilityChangeListeners += callback;
        }

        ISpriteVisibilityToolView ISpriteVisibilityToolModel.view { get {return m_View;} }

        bool ISpriteVisibilityToolModel.hasCharacter { get { return skinningCache.hasCharacter; } }
        SpriteCache ISpriteVisibilityToolModel.selectedSprite { get { return skinningCache.selectedSprite; } }
        CharacterCache ISpriteVisibilityToolModel.character { get { return skinningCache.character; } }
        bool ISpriteVisibilityToolModel.previousVisibility { get { return m_Data.previousVisibility; } set { m_Data.previousVisibility = value; } }
        bool ISpriteVisibilityToolModel.allVisibility { get { return m_Data.allVisibility; } set { m_Data.allVisibility = value; } }
        SkinningMode ISpriteVisibilityToolModel.mode { get { return skinningCache.mode; } }

        UndoScope ISpriteVisibilityToolModel.UndoScope(string description)
        {
            return skinningCache.UndoScope(description);
        }
    }

    internal class SpriteVisibilityToolView : VisibilityToolViewBase, ISpriteVisibilityToolView
    {
        public Func<SpriteVisibilityToolController> GetController = () => null;

        public SpriteVisibilityToolView()
        {
            var columns = new MultiColumnHeaderState.Column[2];
            columns[0] = new MultiColumnHeaderState.Column
            {
                headerContent = VisibilityTreeViewBase.VisibilityIconStyle.visibilityOnIcon,
                headerTextAlignment = TextAlignment.Center,
                width = 32,
                minWidth = 32,
                maxWidth = 32,
                autoResize = false,
                allowToggleVisibility = true
            };
            columns[1] = new MultiColumnHeaderState.Column
            {
                headerContent = EditorGUIUtility.TrTextContent(TextContent.name),
                headerTextAlignment = TextAlignment.Center,
                width = 200,
                minWidth = 130,
                autoResize = true,
                allowToggleVisibility = false
            };
            var multiColumnHeaderState = new MultiColumnHeaderState(columns);
            var multiColumnHeader = new VisibilityToolColumnHeader(multiColumnHeaderState)
            {
                GetAllVisibility = InternalGetAllVisibility,
                SetAllVisibility = InternalSetAllVisibility,
                canSort = false,
                height = 20,
                visibilityColumn = 0
            };

            m_TreeView = new SpriteTreeView(m_TreeViewState, multiColumnHeader)
            {
                GetController = InternalGetController
            };
            SetupSearchField();
        }

        SpriteVisibilityToolController InternalGetController()
        {
            return GetController();
        }

        bool InternalGetAllVisibility()
        {
            return GetController().GetAllVisibility();
        }

        void InternalSetAllVisibility(bool visibility)
        {
            GetController().SetAllVisibility(visibility);
        }

        public void Setup()
        {
            ((SpriteTreeView)m_TreeView).Setup();
        }

        public void SetSelection(SpriteCache sprite)
        {
            ((SpriteTreeView)m_TreeView).SetSelection(sprite);
        }
    }

    class SpriteTreeView : VisibilityTreeViewBase
    {
        public Func<SpriteVisibilityToolController> GetController = () => null;

        public SpriteTreeView(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader)
            : base(treeViewState, multiColumnHeader)
        {}

        public void Setup()
        {
            Reload();
        }

        void CellGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case 0:
                    DrawVisibilityCell(cellRect, item);
                    break;
                case 1:
                    DrawNameCell(cellRect, item, ref args);
                    break;
            }
        }

        void DrawVisibilityCell(Rect cellRect, TreeViewItem item)
        {
            var style = MultiColumnHeader.DefaultStyles.columnHeaderCenterAligned;
            var itemView = item as TreeViewItemBase<CharacterPartCache>;
            var characterPartVisibility = GetController().GetCharacterPartVisibility(itemView);

            EditorGUI.BeginChangeCheck();

            var visible = GUI.Toggle(cellRect, characterPartVisibility, characterPartVisibility ? VisibilityIconStyle.visibilityOnIcon : VisibilityIconStyle.visibilityOffIcon, style);

            if (EditorGUI.EndChangeCheck())
            {
                GetController().SetCharacterPartVisibility(item, visible, Event.current.alt);
            }
        }

        void DrawNameCell(Rect cellRect, TreeViewItem item, ref RowGUIArgs args)
        {
            args.rowRect = cellRect;
            base.RowGUI(args);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            var rows = GetController() != null ? GetController().BuildTreeView() : new List<TreeViewItem>();
            SetupParentsAndChildrenFromDepths(root, rows);
            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            GetController().SetSelectedSprite(GetRows(), selectedIds);
        }

        public void SetSelection(SpriteCache sprite)
        {
            var id = GetController().GetTreeViewSelectionID(GetRows(), sprite);
            SetSelection(new[] { id }, TreeViewSelectionOptions.RevealAndFrame);
        }
    }
}
