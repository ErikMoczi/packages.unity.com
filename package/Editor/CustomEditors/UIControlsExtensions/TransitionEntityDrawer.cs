using System;
using JetBrains.Annotations;
using Unity.Tiny.Runtime.UIControlsExtensions;
using UnityEditor;

namespace Unity.Tiny
{
    [TinyCustomDrawer(CoreGuids.UIControlsExtensions.TransitionEntity)]
    [UsedImplicitly]
    internal class TransitionEntityDrawer : StructDrawer
    {
        public TransitionEntityDrawer(TinyContext context) : base(context)
        {
        }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            var showProperties = true;

            if (!string.IsNullOrEmpty(context.Label))
            {
                if (tinyObject.Properties.PropertyBag.PropertyCount > 0)
                {
                    var folderCache = context.Visitor.FolderCache;
                    if (!folderCache.TryGetValue(tinyObject, out showProperties))
                    {
                        showProperties = true;
                    }

                    showProperties = folderCache[tinyObject] = EditorGUILayout.Foldout(showProperties, context.Label, true);
                }
                else
                {
                    EditorGUILayout.LabelField(context.Label);
                }
            }

            if (showProperties)
            {
                ++EditorGUI.indentLevel;
                try
                {
                    var transition = new TinyTransitionEntity(context.Value);
                    VisitField(ref context, nameof(transition.type));
                    switch (transition.type)
                    {
                        case TinyTransitionType.Sprite:
                            VisitField(ref context, nameof(transition.spriteSwap));
                            break;
                        case TinyTransitionType.ColorTint:
                            VisitField(ref context, nameof(transition.colorTint));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                finally
                {
                    --EditorGUI.indentLevel;
                }
            }
            return context.Visitor.StopVisit;
        }
    }
}