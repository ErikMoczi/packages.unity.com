using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.U2D;

namespace UnityEditor.Experimental.U2D.Animation
{
    [RequireSpriteDataProvider(typeof(ISpriteBoneDataProvider))]
    internal class BoneSpriteEditorModule : SpriteEditorModuleBase
    {
        BonePresenter m_BonePresenter;

        GUID m_CurrentSpriteRectGUID;

        Dictionary<GUID, List<SpriteBone>> m_SpriteBoneCache;

        const float kToolbarHeight = 16f;
        const float kInspectorWindowMargin = 8f;
        const float kInspectorWidth = 200f;
        const float kInspectorHeight = 45f;
        const float kInfoWindowHeight = 45f;

        private Rect toolbarWindowRect
        {
            get
            {
                Rect position = spriteEditor.windowDimension;
                return new Rect(
                    position.width - kInspectorWidth - kInspectorWindowMargin,
                    position.height - kInspectorHeight - kInspectorWindowMargin + kToolbarHeight,
                    kInspectorWidth,
                    kInspectorHeight);
            }
        }

        private Rect infoWindowRect
        {
            get
            {
                Rect position = spriteEditor.windowDimension;
                return new Rect(
                    toolbarWindowRect.xMin,
                    toolbarWindowRect.yMin - kInspectorWindowMargin - kInfoWindowHeight,
                    kInspectorWidth,
                    kInfoWindowHeight);
            }
        }
        
        public override void DoMainGUI()
        {
            var selected = spriteEditor.selectedSpriteRect;
            if (selected != null)
            {
                try
                {
                    EditorGUI.BeginChangeCheck();
                    m_BonePresenter.DoBone(selected.rect);
                    if (EditorGUI.EndChangeCheck())
                        spriteEditor.RequestRepaint();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    CommonDrawingUtility.BeginLines(Color.green);
                    CommonDrawingUtility.DrawBox(selected.rect);
                    CommonDrawingUtility.EndLines();
                }
            }

            if (m_BonePresenter.AllowClickAway()
                && !MouseOnTopOfInspector()
                && spriteEditor.HandleSpriteSelection())
            {
                PreSelectedSpriteRectChange(m_CurrentSpriteRectGUID);

                m_CurrentSpriteRectGUID = spriteEditor.selectedSpriteRect != null ? spriteEditor.selectedSpriteRect.spriteID : new GUID();

                PostSelectedSpriteRectChange(m_CurrentSpriteRectGUID);
            }
        }

        public override void DoToolbarGUI(Rect drawArea)
        {
        }

        public override void DoPostGUI()
        {
            if (spriteEditor.selectedSpriteRect != null)
            {
                m_BonePresenter.DoTool(toolbarWindowRect);
                m_BonePresenter.DoInfoPanel(infoWindowRect);
            }
        }

        public override void OnModuleActivate()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;

            m_SpriteBoneCache = new Dictionary<GUID, List<SpriteBone>>();
            
            var model = new BoneModel(spriteEditor.SetDataModified);
            var hierarchyView = new BoneHierarchyView();
            var toolView = new BoneToolView();
            var infoView = new BoneInfoView();

            m_BonePresenter = new BonePresenter(model, hierarchyView, toolView, infoView);
            
            m_CurrentSpriteRectGUID = spriteEditor.selectedSpriteRect != null ? spriteEditor.selectedSpriteRect.spriteID : new GUID();
            PostSelectedSpriteRectChange(m_CurrentSpriteRectGUID);
        }

        public override void OnModuleDeactivate()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            m_SpriteBoneCache.Clear();
            m_SpriteBoneCache = null;
            m_BonePresenter = null;
        }

        public override bool CanBeActivated()
        {
            return true;
        }
        
        public override bool ApplyRevert(bool apply)
        {
            if (apply)
            {
                if (spriteEditor.selectedSpriteRect != null)
                    PreSelectedSpriteRectChange(spriteEditor.selectedSpriteRect.spriteID);

                Apply();
            }
            return true;
        }

        public override string moduleName
        {
            get
            {
                return "Bone";
            }
        }

        private void UndoRedoPerformed()
        {
            if (spriteEditor.selectedSpriteRect != null && m_CurrentSpriteRectGUID != spriteEditor.selectedSpriteRect.spriteID)
            {
                m_CurrentSpriteRectGUID = spriteEditor.selectedSpriteRect.spriteID;
                PostSelectedSpriteRectChange(m_CurrentSpriteRectGUID);
            }
        }

        private List<SpriteBone> LoadBoneFromDataProvider(GUID spriteID)
        {
            var boneProvider = spriteEditor.GetDataProvider<UnityEditor.Experimental.U2D.ISpriteBoneDataProvider>();

            return boneProvider.GetBones(spriteID);
        }

        private void PreSelectedSpriteRectChange(GUID spriteID)
        {
            if (spriteID.Empty() || m_SpriteBoneCache == null)
                return;

            List<SpriteBone> bones = m_BonePresenter.GetRawData();
            m_SpriteBoneCache[spriteID] = bones;
        }

        private void PostSelectedSpriteRectChange(GUID spriteID)
        {
            if (spriteID.Empty())
                return;

            List<SpriteBone> bones;
            if (!m_SpriteBoneCache.TryGetValue(spriteID, out bones))
            {
                bones = LoadBoneFromDataProvider(spriteID);
                m_SpriteBoneCache.Add(spriteID, bones);
            }

            m_BonePresenter.SetRawData(bones, spriteEditor.selectedSpriteRect.rect.position);
        }

        private void Apply()
        {
            var dataProvider = spriteEditor.GetDataProvider<ISpriteBoneDataProvider>();
            foreach (var p in m_SpriteBoneCache)
                dataProvider.SetBones(p.Key, p.Value);
        }

        private bool MouseOnTopOfInspector()
        {
            if (Event.current == null || Event.current.type != EventType.MouseDown)
                return false;

            // GUIClip.Unclip sets the mouse position to include the windows tab.
            Vector2 mousePosition = GUIClip.Unclip(Event.current.mousePosition) - (GUIClip.topmostRect.position - GUIClip.GetTopRect().position);
            return toolbarWindowRect.Contains(mousePosition) || infoWindowRect.Contains(mousePosition);
        }
    }
}
