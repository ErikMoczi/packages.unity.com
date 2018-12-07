
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class IMGUIVisitorStateTracker : IGUIVisitorStateTracker
    {
        private readonly Stack<bool> k_EnabledStack = new Stack<bool>();
        private readonly Stack<int> k_IndentStack = new Stack<int>();

        public void CacheState()
        {
            k_EnabledStack.Push(GUI.enabled);
            k_IndentStack.Push(EditorGUI.indentLevel);
        }

        public void RestoreState()
        {
            EditorGUI.indentLevel = k_IndentStack.Pop();
            GUI.enabled = k_EnabledStack.Pop();
        }
    }
}
