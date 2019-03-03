using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;

namespace UnityEditor.U2D
{
    public class SplineEditorCache : ScriptableObject
    {
        [SerializeField]
        SpriteShapeController m_Target;
        [SerializeField]
        SerializableSelection m_Selection = new SerializableSelection();
        [SerializeField]
        bool m_RightTangentChanged;
        Dictionary<int, Vector3> m_LeftTangentsBroken = new Dictionary<int, Vector3>();
        Dictionary<int, Vector3> m_RightTangentsBroken = new Dictionary<int, Vector3>();
        Dictionary<int, Vector3> m_LeftTangentsContinous = new Dictionary<int, Vector3>();
        Dictionary<int, Vector3> m_RightTangentsContinous = new Dictionary<int, Vector3>();
        
        static SplineEditorCache m_Instance;
        public static SplineEditorCache instance
        {
            get
            {
                if (!m_Instance)
                    m_Instance = FindObjectOfType<SplineEditorCache>();

                if (!m_Instance)
                {
                    m_Instance = ScriptableObject.CreateInstance<SplineEditorCache>();
                    m_Instance.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_Instance;
            }
        }

        public static bool IsValid()
        {
            return m_Instance != null;
        }

        public static void InvalidateCache()
        {
            if (m_Instance)
            {
                Undo.ClearUndo(m_Instance);
                DestroyImmediate(m_Instance);
            }
        }

        public static void RigisterUndo()
        {
            RigisterUndo(Undo.GetCurrentGroupName());
        }

        public static void RigisterUndo(string undoName)
        {
            if (m_Instance)
                Undo.RegisterCompleteObjectUndo(m_Instance, undoName);
        }

        public static SpriteShapeController GetTarget()
        {
            if (m_Instance)
                return m_Instance.m_Target;

            return null;
        }

        public static void SetTarget(SpriteShapeController target)
        {
            if (m_Instance)
                m_Instance.m_Target = target;
        }

        public static ISelection GetSelection()
        {
            if (instance)
                return instance.selection;

            return null;
        }

        public static void ClearSelection()
        {
            if (instance)
                instance.selection.Clear();
        }

        public ISelection selection
        {
            get { return m_Selection; }
        }

        public bool rightTangentChanged
        {
            get { return m_RightTangentChanged; }
            set { m_RightTangentChanged = value; }
        }

        public Vector3 GetLeftTangentBroken(int index)
        {
            var tangent = Vector3.zero;
            m_LeftTangentsBroken.TryGetValue(index, out tangent);
            return tangent;
        }
        public void SetLeftTangentBroken(int index, Vector3 tangent)
        {
            m_LeftTangentsBroken[index] = tangent;
        }

        public Vector3 GetRightTangentBroken(int index)
        {
            var tangent = Vector3.zero;
            m_RightTangentsBroken.TryGetValue(index, out tangent);
            return tangent;
        }
        public void SetRightTangentBroken(int index, Vector3 tangent)
        {
            m_RightTangentsBroken[index] = tangent;
        }

        public Vector3 GetLeftTangentContinous(int index)
        {
            var tangent = Vector3.zero;
            m_LeftTangentsContinous.TryGetValue(index, out tangent);
            return tangent;
        }
        public void SetLeftTangentContinous(int index, Vector3 vec)
        {
            m_LeftTangentsContinous[index] = vec;
        }

        public Vector3 GetRightTangentContinous(int index)
        {
            var tangent = Vector3.zero;
            m_RightTangentsContinous.TryGetValue(index, out tangent);
            return tangent;
        }
        public void SetRightTangentContinous(int index, Vector3 vec)
        {
            m_RightTangentsContinous[index] = vec;
        }
    }
}
