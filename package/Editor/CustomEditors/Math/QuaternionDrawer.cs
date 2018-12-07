using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Unity.Properties;

namespace Unity.Tiny
{
    [TinyCustomDrawer(CoreGuids.Math.Quaternion)]
    [UsedImplicitly]
    internal class QuaternionDrawer : StructDrawer
    {
        private static readonly string[] k_QuaternionMembers = {"x", "y", "z", "w"};
        private static readonly string[] k_VectorMembers = {"x", "y", "z"};
        
        public QuaternionDrawer(TinyContext context)
            : base(context)
        {
        }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            var properties = tinyObject.Properties;

            //For the rotation, we will offer Euler angles to the user.
            var tinyEuler = GetEulerAnglesObject(tinyObject, ref context);
            tinyEuler.Refresh();

            if (Screen.width < 400)
            {
                EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth - (400 - Screen.width), 70);
            }

            var mixed = EditorGUI.showMixedValue;
            var indent = EditorGUI.indentLevel;

            var changeTracker = context.Visitor.ChangeTracker;
            TinyEditorUtility.SetEditorBoldDefault(tinyObject.IsOverridden);
            try
            {
                EditorGUI.showMixedValue = HasMixedValues(changeTracker, properties);

                EditorGUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(context.Label))
                {
                    EditorGUILayout.PrefixLabel(context.Label);
                }

                EditorGUIUtility.labelWidth = 15;
                EditorGUIUtility.fieldWidth = 30;
                EditorGUI.indentLevel = 0;

                EditorGUI.BeginChangeCheck();

                DrawMembers(tinyEuler.Properties);
                
                if (EditorGUI.EndChangeCheck())
                {
                    SetQuaternionFromEuler(tinyObject, tinyEuler, ref context);
                    PushChanges(changeTracker, properties);
                }
            }
            finally
            {
                TinyEditorUtility.SetEditorBoldDefault(false);
                EditorGUILayout.EndHorizontal();
                EditorGUI.showMixedValue = mixed;
                EditorGUI.indentLevel = indent;
                EditorGUIUtility.fieldWidth = 0;
                EditorGUIUtility.labelWidth = 0;
            }

            return true;
        }

        private static TinyObject GetEulerAnglesObject(TinyObject quaternion, ref UIVisitContext<TinyObject> context)
        {
            var entity = context.MainTarget<TinyEntity>();
            var euler = Vector3.zero;
            if (null == entity)
            {
                var localQuat = quaternion.As<Quaternion>();
                euler = localQuat.eulerAngles;
            }
            else
            {
                euler = entity.View.transform.GetLocalEulerAngles();
            }
            return new TinyObject(quaternion.Registry, TypeRefs.Math.Vector3).AssignFrom(euler);    
        }

        private static void SetQuaternionFromEuler(TinyObject quaternion, TinyObject euler, ref UIVisitContext<TinyObject> context)
        {
            var entity = context.MainTarget<TinyEntity>();
            var e = euler.As<Vector3>();
            if (null == entity)
            {
                quaternion.AssignFrom(new Quaternion { eulerAngles = e });
            }
            else
            {
                entity.View.transform.SetLocalEulerAngles(e);
                quaternion.AssignFrom(entity.View.transform.rotation);
            }
        }

        private static void Draw(TinyObject.PropertiesContainer container,
            IValueClassProperty<TinyObject.PropertiesContainer, float> property)
        {
            var current = property.GetValue(container);
            var value = EditorGUILayout.FloatField(property.Name, current);
            if (!value.Equals(current))
            {
                property.SetValue(container, value);
            }
        }
        
        private static bool HasMixedValues(IGUIChangeTracker changeTracker, TinyObject.PropertiesContainer properties)
        {
            foreach (var member in k_QuaternionMembers)
            {
                if (changeTracker.HasMixedValues<float>(properties, properties.PropertyBag.FindProperty(member)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void PushChanges(IGUIChangeTracker changeTracker, TinyObject.PropertiesContainer properties)
        {
            foreach (var member in k_QuaternionMembers)
            {
                changeTracker.PushChange(properties, properties.PropertyBag.FindProperty(member));
            }
        }
        
        private static void DrawMembers(TinyObject.PropertiesContainer properties)
        {
            foreach (var member in k_VectorMembers)
            {
                Draw(properties, properties.PropertyBag.FindProperty(member) as
                        IValueClassProperty<TinyObject.PropertiesContainer, float>);
            }
        }
    }
}
