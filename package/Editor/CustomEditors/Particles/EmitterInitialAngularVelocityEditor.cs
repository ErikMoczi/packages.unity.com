using JetBrains.Annotations;
using Unity.Properties;
using Unity.Tiny.Runtime.Particles;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.Particles.EmitterInitialAngularVelocity)]
    [UsedImplicitly]
    internal class EmitterInitialAngularVelocityEditor : ParticleComponentEditor
    {
        public EmitterInitialAngularVelocityEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            ShowParticleEmitterWarning(ref context);
            DrawAngleField(tinyObject[nameof(TinyEmitterInitialAngularVelocity.angularVelocity)] as TinyObject, ref context);
            return context.Visitor.StopVisit;
        }

        private static void DrawAngleField(TinyObject direction, ref UIVisitContext<TinyObject> parentContext)
        {
            var folderCache = parentContext.Visitor.FolderCache;
            if (!folderCache.TryGetValue(direction, out var showProperties))
            {
                showProperties = true;
            }

            TinyEditorUtility.SetEditorBoldDefault(direction.IsOverridden);
            showProperties = folderCache[direction] =
                EditorGUILayout.Foldout(showProperties, nameof(TinyEmitterInitialAngularVelocity.angularVelocity), true);
            if (!showProperties)
            {
                return;
            }
            
            var members = new[] {nameof(Range.start), nameof(Range.end)};
            ++EditorGUI.indentLevel;
            try
            {
                var properties = direction.Properties;
                foreach (var member in members)
                {
                    DrawAsDegrees(properties, properties.PropertyBag.FindProperty(member) as
                        IValueClassProperty<TinyObject.PropertiesContainer, float>, parentContext.Visitor.ChangeTracker);
                }
            }
            finally
            {
                --EditorGUI.indentLevel;
                TinyEditorUtility.SetEditorBoldDefault(false);
            }
        }

        private static void DrawAsDegrees(TinyObject.PropertiesContainer container,
            IValueClassProperty<TinyObject.PropertiesContainer, float> property, IGUIChangeTracker changeTracker)
        {
            var current = property.GetValue(container);
            EditorGUI.BeginChangeCheck();
            TinyEditorUtility.SetEditorBoldDefault((property as ITinyValueProperty)?.IsOverridden(container) ?? true);
            EditorGUI.showMixedValue = changeTracker.HasMixedValues<float>(container, property);
            
            var value = EditorGUILayout.FloatField(property.Name, current * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (EditorGUI.EndChangeCheck())
            {
                if (!value.Equals(current))
                {
                    property.SetValue(container, value);
                }
                changeTracker.PushChange(container, property);
            }

            EditorGUI.showMixedValue = false;
        }
    }
}

