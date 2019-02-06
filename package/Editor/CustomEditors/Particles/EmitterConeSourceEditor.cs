using JetBrains.Annotations;
using Unity.Properties;
using Unity.Tiny.Runtime.Particles;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Particles.EmitterConeSource)]
    [UsedImplicitly]
    internal class EmitterConeSourceEditor : ParticleComponentEditor
    {
        public EmitterConeSourceEditor(TinyContext context) : base(context)
        {
        }
        
        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            ShowParticleEmitterWarning(ref context);
            
            VisitField(ref context, nameof(TinyEmitterConeSource.radius));
            DrawAsDegrees(tinyObject.Properties, tinyObject.Properties.PropertyBag.FindProperty(nameof(TinyEmitterConeSource.angle)) as
                IValueClassProperty<TinyObject.PropertiesContainer, float>, context.Visitor.ChangeTracker);
            VisitField(ref context, nameof(TinyEmitterConeSource.speed));
            return context.Visitor.StopVisit;
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