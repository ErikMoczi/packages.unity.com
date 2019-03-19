using System;
using System.Linq;
using Unity.AI.Planner;
using UnityEditor.AI.Planner.Utility;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    [CustomPropertyDrawer(typeof(UsesTraitDefinitionAttribute))]
    class UsesTraitDefinitionDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DomainDefinition domainDefinition = null;
            var planDefinition = property.FindObjectOfType<PlanDefinition>();
            if (planDefinition != null)
                domainDefinition = planDefinition.DomainDefinition;

            if (domainDefinition == null)
                domainDefinition = property.FindObjectOfType<DomainDefinition>();

            var traits = domainDefinition.TraitDefinitions.Select(t => t.Name).ToArray();

            var singleProperty = property.name != "data";

            if (!((UsesTraitDefinitionAttribute)attribute).ShowLabel)
                label.text = " ";

            var endProperty = property.GetEndProperty();
            while (singleProperty || !SerializedProperty.EqualContents(property, endProperty))
            {
                var index = Array.IndexOf(traits, property.stringValue);
                EditorGUI.BeginChangeCheck();

                index = EditorGUI.Popup(position, label.text, index, traits);

                if (EditorGUI.EndChangeCheck())
                    property.stringValue = traits[index];

                if (singleProperty || !property.NextVisible(false))
                    break;
            }
        }
    }
}
