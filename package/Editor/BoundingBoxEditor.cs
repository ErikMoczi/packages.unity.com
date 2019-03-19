#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Unity.Audio.Megacity
{
    [CustomEditor(typeof(BoundingBoxComponent)), CanEditMultipleObjects]
    public class BoundingBoxComponentEditor : Editor
    {
        protected virtual void OnSceneGUI()
        {
            BoundingBoxComponent bb = (BoundingBoxComponent)target;

            EditorGUI.BeginChangeCheck();
            Vector3 p = Handles.PositionHandle(bb.size + bb.transform.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(bb, "Change extent");
                bb.size = p - bb.transform.position;
                var v = bb.Value;
                v.center = bb.transform.position;
                v.size = bb.size;
                bb.Value = v;
            }
        }
    }
}
#endif
