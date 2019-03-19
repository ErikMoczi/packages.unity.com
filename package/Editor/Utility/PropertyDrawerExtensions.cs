using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityEditor.AI.Planner.Utility
{
    static class PropertyDrawerExtensions
    {
        public static T GetValue<T>(this PropertyDrawer @this, SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var value = @this.fieldInfo.GetValue(targetObject);
            if (value is IList<T> list && property.name == "data")
            {
                var match = Regex.Match(property.propertyPath, @"\d+");
                if (match.Success)
                {
                    if (int.TryParse(match.Value, out var index))
                        return list[index];
                }
            }

            return (T)value;
        }
    }
}
