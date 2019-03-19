using System;
using UnityEngine;
using UnityEngine.AI.Planner.Agent;

namespace UnityEditor.AI.Planner.Agent
{
    // EditorGUILayout isn't supported in PropertyDrawers, but works with some modifications (e.g. GetPropertyHeight)
    // However, inspectors that show fields that have PropertyDrawers AND use EditorGUILayout don't display correctly.
    // As a workaround, simply defining a custom editor for the MonoBehaviour that has the field does the trick.
    [CustomEditor(typeof(BaseAgent<>), true)]
    class BaseAgentInspector : Editor
    {
    }
}
