using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.AI.Planner.UI
{
    class BoardResources : ScriptableSingleton<BoardResources>
    {
        public VisualTreeAsset BoardUxml => m_BoardUxml;

        public VisualTreeAsset BoardSectionUxml => m_BoardSectionUxml;

        public VisualTreeAsset BoardRowUxml => m_BoardRowUxml;

        public VisualTreeAsset BoardFieldUxml => m_BoardFieldUxml;

        public StyleSheet BoardStyleSheet => m_BoardStyleSheet;

        // These fields are assigned in the editor, so ignore the warning that they are never assigned to
        #pragma warning disable 0649

        [SerializeField]
        VisualTreeAsset m_BoardUxml;

        [SerializeField]
        VisualTreeAsset m_BoardSectionUxml;

        [SerializeField]
        VisualTreeAsset m_BoardRowUxml;

        [SerializeField]
        VisualTreeAsset m_BoardFieldUxml;

        [SerializeField]
        StyleSheet m_BoardStyleSheet;

    #pragma warning restore 0649
    }
}
