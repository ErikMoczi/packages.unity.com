

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace Unity.Tiny
{
    // @TODO: Implement this eventually.
    internal sealed class UIElementsBackend : InspectorBackend
    {
        public UIElementsBackend(TinyInspector inspector) : base(inspector)
        {
        }

        public override void Build()
        {
            var root = m_Inspector.GetRoot();
            root.Clear();
            var elem = new Label()
            {
                text = "The UIElements backend is not implemented yet."
            };
            elem.style.color = Color.gray;
            elem.style.fontSize = 15;
            root.Add(elem);
        }
    }
}

