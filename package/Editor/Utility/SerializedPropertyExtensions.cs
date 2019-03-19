using System;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.AI.Planner.Utility
{
    static class SerializedPropertyExtensions
    {
        public static void ForEachArrayElement(this SerializedProperty property, Action<SerializedProperty> callback,
            bool showSizeField = false)
        {
            property = property.Copy();
            var endProperty = property.GetEndProperty();
            property.NextVisible(true); // Enter into the collection

            if (showSizeField)
                EditorGUILayout.PropertyField(property);

            property.NextVisible(false); // Step past the size field

            while (!SerializedProperty.EqualContents(property, endProperty))
            {
                callback(property);

                if (!property.NextVisible(false))
                    break;
            }
        }

        public static void DrawArrayProperty(this SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property);

            if (!property.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                property.ForEachArrayElement(domainObjectData =>
                {
                    EditorGUILayout.PropertyField(domainObjectData, true);
                }, true);
            }
        }

        public static T FindObjectOfType<T>(this SerializedProperty property) where T : UnityObject
        {
            var found = property.serializedObject.targetObject as T;

            // It's possible that the object is located within a member field, so look for it there
            if (!found)
            {
                var searchProperty = property.serializedObject.GetIterator();
                while (searchProperty.NextVisible(true))
                {
                    if (searchProperty.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        found = searchProperty.objectReferenceValue as T;
                        if (found)
                            break;
                    }
                }
            }

            return found;
        }
    }
}
