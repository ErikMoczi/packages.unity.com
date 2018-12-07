using System;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class SwitchModeTool : BaseTool
    {
        protected override void OnActivate()
        {
            skinningCache.mode = SkinningMode.Character;
            skinningCache.events.skinningModeChanged.Invoke(skinningCache.mode);
        }

        protected override void OnDeactivate()
        {
            skinningCache.mode = SkinningMode.SpriteSheet;
            skinningCache.events.skinningModeChanged.Invoke(skinningCache.mode);
        }

        public void SetActive(bool active)
        {
            using (skinningCache.UndoScope(TextContent.setMode))
            {
                if (isActive)
                    Deactivate();
                else
                    Activate();
            }
        }
    }
}
