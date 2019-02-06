
using UnityEditor;
using Unity.Properties;

namespace Unity.Tiny
{
    internal class GenericUIElementVisitor : PropertyVisitorAdapter
    {
        public InspectorMode Mode { get; set; } = InspectorMode.Normal;

        public override void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
        }

        public override void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
        }
    }
}

