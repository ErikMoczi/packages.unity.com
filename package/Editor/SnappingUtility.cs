using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.U2D.Common;

namespace UnityEditor.U2D
{
    public static class SnappingUtility
    {
        public static bool enabled { get; set; }
        public static Vector3 Snap(Vector3 position)
        {
            if (!enabled)
                return position;

            return new Vector3(
                Snap(position.x, InternalEditorBridge.GetSnapSettingMove().x),
                Snap(position.y, InternalEditorBridge.GetSnapSettingMove().y),
                position.z
                );
        }

        public static float Snap(float value, float snap)
        {
            if (!enabled)
                return value;

            return Mathf.Round(value / snap) * snap;
        }
    }
}
