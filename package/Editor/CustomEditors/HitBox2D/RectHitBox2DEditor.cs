using JetBrains.Annotations;
using UnityEditor;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.HitBox2D.RectHitBox2D)]
    [UsedImplicitly]
    internal class RectHitBox2DEditor : ComponentEditor
    {
        public RectHitBox2DEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();

            if (!target.HasComponent<Runtime.Core2D.TinySprite2DRenderer>())
            {
                EditorGUILayout.HelpBox("A Sprite2DRenderer component (with an alpha color of more than 0) is needed with the RectHitBox2D.", MessageType.Warning);
                AddComponentToTargetButton(context, TypeRefs.Core2D.Sprite2DRenderer);
            }

            return base.Visit(ref context);
        }
    }
}

