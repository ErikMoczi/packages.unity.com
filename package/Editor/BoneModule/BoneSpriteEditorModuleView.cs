using UnityEngine;
using UnityEngine.Experimental.U2D.Common;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IBoneSpriteEditorModuleView
    {
        Rect toolbarWindowRect { get; }
        Rect infoWindowRect { get; }

        void DrawSpriteRectBorder();
        bool HandleSpriteSelection();
    }

    internal class BoneSpriteEditorModuleView : IBoneSpriteEditorModuleView
    {
        ISpriteEditor spriteEditor { get; set; }

        const float kToolbarHeight = 16f;
        const float kInspectorWindowMargin = 8f;
        const float kInspectorWidth = 200f;
        const float kInspectorHeight = 45f;
        const float kInfoWindowHeight = 45f;

        public Rect toolbarWindowRect
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

        public Rect infoWindowRect
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

        public void DrawSpriteRectBorder()
        {
            if (spriteEditor.selectedSpriteRect != null
                && Event.current != null && Event.current.type == EventType.Repaint)
            {
                CommonDrawingUtility.BeginLines(Color.green);
                CommonDrawingUtility.DrawBox(spriteEditor.selectedSpriteRect.rect);
                CommonDrawingUtility.EndLines();
            }
        }

        public bool HandleSpriteSelection()
        {
            return (Event.current != null && Event.current.type == EventType.MouseDown && Event.current.clickCount == 2
                    && !MouseOnTopOfInspector()
                    && spriteEditor.HandleSpriteSelection());
        }

        public BoneSpriteEditorModuleView(ISpriteEditor spriteEditor)
        {
            this.spriteEditor = spriteEditor;
        }
        
        private bool MouseOnTopOfInspector()
        {
            if (Event.current == null || Event.current.type != EventType.MouseDown)
                return false;

            // GUIClip.Unclip sets the mouse position to include the windows tab.
            Vector2 mousePosition = InternalEngineBridge.GUIUnclip(Event.current.mousePosition) - (InternalEngineBridge.GetGUIClipTopMostRect().position - InternalEngineBridge.GetGUIClipTopRect().position);
            return toolbarWindowRect.Contains(mousePosition) || infoWindowRect.Contains(mousePosition);
        }
    }
}