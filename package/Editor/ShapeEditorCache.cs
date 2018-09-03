using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.U2D
{
    public class ShapeEditorCache : ScriptableObject
    {
        static ShapeEditorCache m_Instance;
        public static ShapeEditorCache instance
        {
            get
            {
                if (!m_Instance)
                    m_Instance = FindObjectOfType<ShapeEditorCache>();

                if (!m_Instance)
                {
                    m_Instance = ScriptableObject.CreateInstance<ShapeEditorCache>();
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

        public static void RecordUndo()
        {
            RecordUndo(Undo.GetCurrentGroupName());
        }

        public static void RecordUndo(string undoName)
        {
            if (m_Instance)
                Undo.RegisterCompleteObjectUndo(m_Instance, undoName);
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

        public Vector3 leftTangent
        {
            get { return m_LeftTangent; }
            set { m_LeftTangent = value; }
        }

        public Vector3 rightTangent
        {
            get { return m_RightTangent; }
            set { m_RightTangent = value; }
        }

        [SerializeField]
        SerializableSelection m_Selection = new SerializableSelection();
        [SerializeField]
        bool m_RightTangentChanged;
        [SerializeField]
        Vector3 m_LeftTangent;
        [SerializeField]
        Vector3 m_RightTangent;
    }
}
