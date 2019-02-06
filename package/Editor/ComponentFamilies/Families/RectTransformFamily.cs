
using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ComponentFamily(
        requiredGuids: new []
        {
            CoreGuids.UILayout.RectTransform,
            CoreGuids.Core2D.TransformNode,
            CoreGuids.Core2D.TransformLocalPosition,
        },
        optionalGuids: new []
        {
            CoreGuids.Core2D.TransformLocalRotation,
            CoreGuids.Core2D.TransformLocalScale,
        }),
     ExtendedComponentFamily(typeof(TransformFamily)),
     ComponentFamilyHidden(new []
     {
         CoreGuids.Core2D.TransformNode,
         CoreGuids.Core2D.TransformLocalPosition,
     }),
     UsedImplicitly]
    internal class RectTransformFamily : ComponentFamily
    {
        public override string Name => "Rect Transform";

        public RectTransformFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext) { }

        protected override bool VisitFamilyComponent(ref UIVisitContext<TinyObject> context)
        {
            var editor = CustomEditors.GetEditor(context.Value.Type);
            return editor.Visit(ref context);
        }
    }
}
