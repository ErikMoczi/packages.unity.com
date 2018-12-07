using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class EntityAdapterUtility
    {
        internal static void DrawEntityHeader(ref UIVisitContext<TinyEntity> context)
        {
            var entities = context.Targets.OfType<TinyEntity>().ToList();
            var firstEntity = entities.FirstOrDefault();
            
            var prefabSelected = entities.All(e => e.HasEntityInstanceComponent());

            var height = 18 + 2 * TinyGUIUtility.SingleLineAndSpaceHeight;

            if (prefabSelected)
            {
                height += TinyGUIUtility.SingleLineAndSpaceHeight;
            }
            
            TinyGUI.BackgroundColor(new Rect(0, 0, Screen.width, height), TinyColors.Inspector.HeaderBackground);

            using (new IMGUIPrefabEntityScope(entities))
            {    
                GUILayout.Space(10);
                
                var name = firstEntity.Name;
                var isStatic = firstEntity.Static;
                var enabled = firstEntity.Enabled;
    
                var mixedName = entities.Any(entity => entity.Name != name);
                var mixedStatic = entities.Any(entity => entity.Static!= isStatic);
                var mixedEnabled = entities.Any(tiny => tiny.Enabled != enabled);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    
                    GUILayout.Space(40);
                    
                    var mixed = EditorGUI.showMixedValue;
                    EditorGUI.showMixedValue = mixedEnabled;
                    enabled = EditorGUILayout.ToggleLeft(GUIContent.none, enabled, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                    EditorGUI.showMixedValue = mixed;
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var entity in entities)
                        {
                            entity.Enabled = enabled;
                            entity.View.gameObject.SetActive(enabled);
                        }
                    }
                    
                    EditorGUI.showMixedValue = mixedName;
                    EditorGUI.BeginChangeCheck();
                    name = EditorGUILayout.DelayedTextField(name, TinyStyles.ComponentHeaderStyle);
                    if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(name))
                    {
                        foreach (var entity in entities)
                        {
                            entity.Name = name;
                            entity.View.gameObject.name = name;
                        }
                    }
    
                    EditorGUI.showMixedValue = mixedStatic;
                    EditorGUI.BeginChangeCheck();
                    isStatic = EditorGUILayout.ToggleLeft("Static", isStatic, GUILayout.Width(50.0f));
                    if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(name))
                    {
                        foreach (var entity in entities)
                        {
                            entity.Static = isStatic;
                            entity.View.gameObject.isStatic = isStatic;
                        }
                    }
    
                    EditorGUI.showMixedValue = false;
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
                }
                
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing * 2);
    
                var layer = firstEntity.Layer;
                var sameLayer = entities.All(tiny => tiny.Layer == layer);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Layer", GUILayout.Width(50));
                    EditorGUI.BeginChangeCheck();
                    var mixed = EditorGUI.showMixedValue;
                    EditorGUI.showMixedValue = !sameLayer;
                    layer = EditorGUILayout.LayerField(layer);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var entity in entities)
                        {
                            entity.Layer = layer;
                            entity.View.gameObject.layer = layer;
                        }
                    }
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
                    EditorGUI.showMixedValue = mixed;
                }

                IMGUIPrefabUtility.ShowEntityPrefabHeader(context.Value.Registry, entities);
                
                GUILayout.Space(5);
            } 
            TinyGUILayout.Separator(TinyColors.Inspector.Separator, TinyGUIUtility.ComponentHeaderSeperatorHeight);
        }
    }
}