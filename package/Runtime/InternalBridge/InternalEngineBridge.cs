namespace UnityEngine.Experimental.U2D.Common
{
    internal static class InternalEngineBridge
    {
        public static void SetLocalAABB(SpriteRenderer spriteRenderer, Bounds aabb)
        {
            spriteRenderer.SetLocalAABB(aabb);
        }

        public static Vector2 GUIUnclip(Vector2 v)
        {
            return GUIClip.Unclip(v);
        }

        public static Rect GetGUIClipTopMostRect()
        {
            return GUIClip.topmostRect;
        }

        public static Rect GetGUIClipTopRect()
        {
            return GUIClip.GetTopRect();
        }

#if UNITY_EDITOR
        public static void SetLocalEulerHint(Transform t)
        {
            t.SetLocalEulerHint(t.GetLocalEulerAngles(t.rotationOrder));
        }
#endif
    }
}