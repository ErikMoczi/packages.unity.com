using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityEditor.AI.Planner.Utility
{
    static class PropertyDrawerExtensions
    {
        public static T GetValue<T>(this PropertyDrawer @this, SerializedProperty property)
        {
            var serializedObject = property.serializedObject;
            var targetObject = (object)serializedObject.targetObject;

            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var propertyPath = property.propertyPath;
            FieldInfo fieldInfo = null;
            while (!string.IsNullOrEmpty(propertyPath))
            {
                var dotIndex = propertyPath.IndexOf('.');
                var field = propertyPath;
                if (dotIndex >= 0)
                {
                    field = propertyPath.Substring(0, dotIndex);
                    propertyPath = propertyPath.Substring(dotIndex + 1);
                }

                if (field == "Array")
                {
                    if (targetObject is IList list)
                    {
                        var match = Regex.Match(propertyPath, @"\d+");
                        if (match.Success)
                        {
                            if (int.TryParse(match.Value, out var index))
                            {
                                targetObject = list[index];
                                dotIndex = propertyPath.IndexOf('.');
                                if (dotIndex >= 0)
                                    propertyPath = propertyPath.Substring(dotIndex + 1);
                                else
                                    propertyPath = string.Empty;
                                fieldInfo = null;
                            }
                        }
                    }

                }
                else
                {
                    if (fieldInfo == null)
                    {
                        var mainType = targetObject.GetType();
                        fieldInfo = mainType.GetField(field, bindingFlags);

                        var baseType = mainType.BaseType;
                        if (fieldInfo == null && baseType != null)
                            fieldInfo = baseType.GetField(field, bindingFlags);
                    }
                    else
                    {
                        fieldInfo = fieldInfo.FieldType.GetField(field, bindingFlags);
                    }

                    targetObject = fieldInfo.GetValue(targetObject);
                }
            }

            return (T)targetObject;
        }
    }
}
