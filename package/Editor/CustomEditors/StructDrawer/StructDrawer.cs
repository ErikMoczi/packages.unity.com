
using UnityEditor;

namespace Unity.Tiny
{
    internal class StructDrawer : TinyCustomEditor, IStructDrawer
    {
        internal static StructDrawer CreateDefault(TinyContext context) => new StructDrawer(context);

        protected StructDrawer(TinyContext context)
            :base(context)
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
                    foreach (var field in context.Value.Type.Dereference(TinyContext.Registry).Fields)
                    {
                        VisitField(ref context, field.Name);
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
