#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Unity.Audio.Megacity
{
    [CustomEditor(typeof(ECSoundEmitterComponent)), CanEditMultipleObjects]
    public class ECSoundEmitterComponentEditor : Editor
    {
        protected virtual void OnSceneGUI()
        {
            ECSoundEmitterComponent emitter = (ECSoundEmitterComponent)target;

            if (emitter == null || emitter.definition == null)
                return;

            Vector3 p;

            float handleSize = 1.0f;

            EditorGUI.BeginChangeCheck();
            p = Handles.Slider(emitter.transform.position + emitter.transform.forward * emitter.minDist, emitter.transform.forward, handleSize, Handles.ConeHandleCap, 0.001f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(emitter, "Change min distance");
                emitter.minDist = (p - emitter.transform.position).magnitude;
            }

            EditorGUI.BeginChangeCheck();
            p = Handles.Slider(emitter.transform.position - emitter.transform.forward * emitter.maxDist, -emitter.transform.forward, handleSize, Handles.ConeHandleCap, 0.001f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(emitter, "Change max distance");
                emitter.maxDist = (p - emitter.transform.position).magnitude;
            }

            float coneAngleScale = 0.1f;
            EditorGUI.BeginChangeCheck();
            p = Handles.Slider(emitter.transform.position + emitter.transform.up * emitter.coneAngle * coneAngleScale, emitter.transform.up, handleSize, Handles.ConeHandleCap, 0.001f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(emitter, "Change cone angle");
                emitter.coneAngle = (p - emitter.transform.position).magnitude / coneAngleScale;
            }

            EditorGUI.BeginChangeCheck();
            p = Handles.Slider(emitter.transform.position - emitter.transform.up * emitter.coneTransition * coneAngleScale, -emitter.transform.up, handleSize, Handles.ConeHandleCap, 0.001f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(emitter, "Change cone angle");
                emitter.coneTransition = (p - emitter.transform.position).magnitude;
            }
        }
    }
}
#endif
