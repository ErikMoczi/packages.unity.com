using JetBrains.Annotations;
using UnityEditor;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.HitBox2D.Sprite2DRendererHitBox2D)]
    [UsedImplicitly]
    internal class Sprite2DRendererHitBox2DEditor : ComponentEditor
    {
        public Sprite2DRendererHitBox2DEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();

            if (!target.HasComponent<Runtime.Core2D.TinySprite2DRenderer>())
            {
                EditorGUILayout.HelpBox("A Sprite2DRenderer component (with an alpha color of more than 0) is needed with the Sprite2DRendererHitBox2D.", MessageType.Warning);
                AddComponentToTargetButton(context, TypeRefs.Core2D.Sprite2DRenderer);
            }

            return base.Visit(ref context);
        }
    }
}

