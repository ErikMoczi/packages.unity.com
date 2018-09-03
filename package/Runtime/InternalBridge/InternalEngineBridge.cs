using UnityEngine;
using UnityEngine.Experimental.U2D;

namespace UnityEngine.Experimental.U2D.Common
{
    public static class InternalEngineBridge
    {
        public static void SetLocalAABB(SpriteRenderer spriteRenderer, Bounds aabb)
        {
            spriteRenderer.SetLocalAABB(aabb);
        }
    }
}