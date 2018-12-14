using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Core2D.TransformLocalPosition)]
    [UsedImplicitly]
    internal class TransformLocalPositionEditor : ComponentEditor
    {
        public TransformLocalPositionEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var isRoot = target.Parent().Equals(TinyEntity.Reference.None);
            var hasCanvas = target.HasComponent(TypeRefs.UILayout.UICanvas);
            var hasRectTransform = target.HasComponent(TypeRefs.UILayout.RectTransform);

            if (isRoot && hasCanvas && hasRectTransform)
            {
                EditorGUILayout.HelpBox("Some values are driven by the UICanvas", MessageType.None);
                GUI.enabled = false;
            }

            if (hasRectTransform && !hasCanvas)
            {
                EditorGUILayout.HelpBox("Some values are driven by the RectTransform", MessageType.None);
                GUI.enabled = false;
            }
            return base.Visit(ref context);
        }

    }

    [TinyCustomEditor(CoreGuids.Core2D.TransformLocalRotation)]
    [UsedImplicitly]
    internal class TransformLocalRotationEditor : ComponentEditor
    {
        public TransformLocalRotationEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var isRoot = target.Parent().Equals(TinyEntity.Reference.None);
            var hasCanvas = target.HasComponent(TypeRefs.UILayout.UICanvas);
            var hasRectTransform = target.HasComponent(TypeRefs.UILayout.RectTransform);

            if (isRoot && hasCanvas && hasRectTransform)
            {
                EditorGUILayout.HelpBox("Some values are driven by the UICanvas", MessageType.None);
                GUI.enabled = false;
            }
            return base.Visit(ref context);
        }

    }

    [TinyCustomEditor(CoreGuids.Core2D.TransformLocalScale)]
    [UsedImplicitly]
    internal class TransformLocalScaleEditor : ComponentEditor
    {
        public TransformLocalScaleEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var isRoot = target.Parent().Equals(TinyEntity.Reference.None);
            var hasCanvas = target.HasComponent(TypeRefs.UILayout.UICanvas);
            var hasRectTransform = target.HasComponent(TypeRefs.UILayout.RectTransform);

            if (isRoot && hasCanvas && hasRectTransform)
            {
                EditorGUILayout.HelpBox("Some values are driven by the UICanvas", MessageType.None);
                GUI.enabled = false;
            }
            return base.Visit(ref context);
        }

    }
}

