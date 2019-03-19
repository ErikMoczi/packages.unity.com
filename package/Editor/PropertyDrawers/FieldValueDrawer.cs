using System;
using System.Linq;
using Unity.AI.Planner;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using UnityEngine;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    // We're not actually creating a property drawer for FieldValue, but this is used to draw a FieldValue
    class FieldValueDrawer
    {
        public static void PropertyField(SerializedProperty property, Type fieldType, GUIContent label = null)
        {
            var fieldLabel = property.FindPropertyRelative("m_Name").stringValue;
            if (label != null)
                label.text = fieldLabel;
            else
                label = new GUIContent(fieldLabel);

            switch (Type.GetTypeCode(fieldType))
            {
                case TypeCode.Boolean:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("m_BoolValue"), label);
                    break;

                case TypeCode.Single:
                case TypeCode.Double:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("m_FloatValue"), label);
                    break;

                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    if (fieldType.IsEnum)
                    {
                        var enumVal = property.FindPropertyRelative("m_IntValue");
                        enumVal.intValue = EditorGUILayout.Popup(enumVal.intValue,
                            Enum.GetNames(fieldType).Select(e => $"{fieldType.Name}.{e}").ToArray());
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("m_IntValue"), label);
                    }

                    break;

                case TypeCode.String:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("m_StringValue"), label);
                    break;

                case TypeCode.Object:
                    if (typeof(Bool).IsAssignableFrom(fieldType))
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("m_BoolValue"), label);
                    else if (typeof(DomainObjectID).IsAssignableFrom(fieldType)) // store DomainObject references by name
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("m_StringValue"), label);
                    else
                        EditorGUILayout.ObjectField(property.FindPropertyRelative("m_ObjectValue"), fieldType, label);
                    break;

                default:
                    EditorGUILayout.ObjectField(property.FindPropertyRelative("m_ObjectValue"), fieldType, label);
                    break;
            }
        }
    }
}
