using System;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.U2D.Animation
{
    public class BoneInspectorPanel : VisualElement
    {
        public class BoneInspectorPanelFactory : UxmlFactory<BoneInspectorPanel, BoneInspectorPanelUxmlTraits> {}
        public class BoneInspectorPanelUxmlTraits : VisualElement.UxmlTraits {}
        public event Action<int> onBoneDepthChanged = (depth) => {};
        public event Action<string> onBoneNameChanged = (name) => {};

        private TextField m_BoneNameField;
        private IntegerField m_BoneDepthField;

        public string boneName
        {
            get { return m_BoneNameField.value; }
            set { m_BoneNameField.value = value; }
        }

        public int boneDepth
        {
            get { return m_BoneDepthField.value; }
            set { m_BoneDepthField.value = value; }
        }

        public BoneInspectorPanel()
        {
            AddStyleSheetPath("BoneInspectorPanelStyle");

            RegisterCallback<MouseDownEvent>((e) => { e.StopPropagation(); });
            RegisterCallback<MouseUpEvent>((e) => { e.StopPropagation(); });
        }

        public void BindElements()
        {
            m_BoneNameField = this.Q<TextField>("BoneNameField");
            m_BoneDepthField = this.Q<IntegerField>("BoneDepthField");
            m_BoneNameField.OnValueChanged(BoneNameChanged);
            m_BoneDepthField.OnValueChanged(BoneDepthChanged);
        }

        private void BoneNameChanged(ChangeEvent<string> evt)
        {
            onBoneNameChanged(evt.newValue);
        }

        private void BoneDepthChanged(ChangeEvent<int> evt)
        {
            onBoneDepthChanged(evt.newValue);
        }

        public static BoneInspectorPanel GenerateFromUXML()
        {
            var visualTree = Resources.Load("BoneInspectorPanel") as VisualTreeAsset;
            var clone = visualTree.CloneTree(null).Q<BoneInspectorPanel>("BoneInspectorPanel");
            clone.BindElements();
            return clone;
        }
    }
}
