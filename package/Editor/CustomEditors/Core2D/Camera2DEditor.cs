using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Unity.Properties;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Core2D.Camera2D)]
    internal class Camera2DEditor : ComponentEditor
    {
        public Camera2DEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            DrawClearFlags(ref context);
            DrawCullingMask(ref context);
            VisitField(ref context, "halfVerticalSize");
            VisitField(ref context, "rect");
            VisitField(ref context, "depth");
            return true;
        }

        private void DrawClearFlags(ref UIVisitContext<TinyObject> context)
        {
            var tiny = context.Value;
            const string fieldName = "clearFlags";
            VisitField(ref context, fieldName);
            if (tiny.GetProperty<CameraClearFlags>(fieldName) == CameraClearFlags.Color)
            {
                VisitField(ref context, "backgroundColor");
            }
        }

        private void DrawCullingMask(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            var cullingMask = tinyObject.Properties.PropertyBag.FindProperty("layerMask") as IValueClassProperty<TinyObject.PropertiesContainer, int>;
            EditorGUI.BeginChangeCheck();
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = context.Visitor.ChangeTracker.HasMixedValues<int>(tinyObject.Properties, cullingMask);
            var isOverriden = (cullingMask as ITinyValueProperty)?.IsOverridden(tinyObject.Properties) ?? true;
            TinyEditorUtility.SetEditorBoldDefault(isOverriden);
            try
            {
                var container = tinyObject.Properties;
                var layerNames = GetLayerNames();
                var newLayer = EditorGUILayout.MaskField("cullingMask", GetCurrentEditorLayer(layerNames, cullingMask.GetValue(container)), layerNames.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    cullingMask.SetValue(container, GetLayers(layerNames, newLayer));
                    context.Visitor.ChangeTracker.PushChange(container, cullingMask);
                }
            }
            finally
            {
                TinyEditorUtility.SetEditorBoldDefault(false);
                EditorGUI.showMixedValue = mixed;
            }
        }

        public static List<string> GetLayerNames()
        {
            var names = new List<string>();
            for (var i = 0; i < 32; ++i)
            {
                var name = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                names.Add(name);
            }
            return names;
        }

        private static int GetLayers(List<string> layerNames, int editorMask)
        {
            if (editorMask < 0)
            {
                return editorMask;
            }

            var layer = 0;

            if (editorMask != 0)
            {
                for (var i = 0; i < 32; ++i)
                {
                    if ((editorMask & 1 << i) == (1 << i))
                    {
                        layer |= 1 << LayerMask.NameToLayer(layerNames[i]);
                    }
                }
            }
            return layer;
        }

        private static int GetCurrentEditorLayer(List<string> layerNames, int cullingMask)
        {
            if (cullingMask < 0)
            {
                return cullingMask;
            }
            var layer = 0;
            for (var i = 0; i < 32; ++i)
            {
                if ((cullingMask & 1 << i) == 1 << i)
                {
                    var index = layerNames.FindIndex(s => s == LayerMask.LayerToName(i));
                    layer |= 1 << index;
                }
            }
            return layer;
        }
    }
}

