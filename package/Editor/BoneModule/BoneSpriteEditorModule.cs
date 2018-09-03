using System;

using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    [RequireSpriteDataProvider(typeof(ISpriteBoneDataProvider))]
    internal class BoneSpriteEditorModule : SpriteEditorModuleBase
    {
        BonePresenter m_BonePresenter;

        GUID m_CurrentSpriteRectGUID;

        BoneCacheManager m_BoneCacheManager;

        IBoneSpriteEditorModuleView m_SpriteEditorModuleView;
        public IBoneSpriteEditorModuleView spriteEditorModuleView
        {
            get { return m_SpriteEditorModuleView; }
            set { m_SpriteEditorModuleView = value; }
        }

        public override void DoMainGUI()
        {
            if (spriteEditor.selectedSpriteRect != null)
            {
                try
                {
                    EditorGUI.BeginChangeCheck();
                    m_BonePresenter.DoBone(spriteEditor.selectedSpriteRect.rect);
                    if (EditorGUI.EndChangeCheck())
                        spriteEditor.RequestRepaint();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }

            spriteEditorModuleView.DrawSpriteRectBorder();

            if (spriteEditorModuleView.HandleSpriteSelection())
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
                m_BonePresenter.DoTool(spriteEditorModuleView.toolbarWindowRect);
                m_BonePresenter.DoInfoPanel(spriteEditorModuleView.infoWindowRect);
            }
        }

        public override void OnModuleActivate()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;

            m_BoneCacheManager = new BoneCacheManager(spriteEditor.GetDataProvider<ISpriteBoneDataProvider>(), spriteEditor.GetDataProvider<ISpriteMeshDataProvider>());
            
            m_SpriteEditorModuleView = new BoneSpriteEditorModuleView(spriteEditor);
            
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

            m_BoneCacheManager.CleanUp();
            m_BoneCacheManager = null;
            m_SpriteEditorModuleView = null;
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
                return "Bone Editor";
            }
        }

        private void UndoRedoPerformed()
        {
            if (spriteEditor.selectedSpriteRect != null && m_CurrentSpriteRectGUID != spriteEditor.selectedSpriteRect.spriteID)
            {
                PreSelectedSpriteRectChange(m_CurrentSpriteRectGUID);
                m_CurrentSpriteRectGUID = spriteEditor.selectedSpriteRect.spriteID;
                PostSelectedSpriteRectChange(m_CurrentSpriteRectGUID);
            }
        }

        private void PreSelectedSpriteRectChange(GUID spriteID)
        {
            if (spriteID.Empty() || m_BoneCacheManager == null)
                return;

            var bones = m_BonePresenter.GetRawData();
            m_BoneCacheManager.SetSpriteBoneRawData(spriteID, bones);
        }

        private void PostSelectedSpriteRectChange(GUID spriteID)
        {
            if (spriteID.Empty())
                return;

            var bones = m_BoneCacheManager.GetSpriteBoneRawData(spriteID);
            m_BonePresenter.SetRawData(bones, spriteEditor.selectedSpriteRect.rect.position);
        }

        private void Apply()
        {
            m_BoneCacheManager.Apply();
        }

    }
}
