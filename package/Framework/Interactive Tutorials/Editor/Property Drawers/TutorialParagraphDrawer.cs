using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [CustomPropertyDrawer(typeof(TutorialParagraph))]
    class TutorialParagraphDrawer : FlushChildrenDrawer
    {
        const string k_TypePath = "m_Type";
        const string k_TextPath = "m_Text";
        const string k_IconsPath = "m_Icons";
        const string k_CriteriaPath = "m_Criteria";
        const string k_SummaryPath = "m_Summary";
        const string k_CompletionPath = "m_CriteriaCompletion";

        protected override void DisplayChildProperty(
            Rect position, SerializedProperty parentProperty, SerializedProperty childProperty, GUIContent label
            )
        {
            ParagraphType type = (ParagraphType)parentProperty.FindPropertyRelative(k_TypePath).intValue;
            switch (childProperty.name)
            {
                case k_TextPath:
                    if (type == ParagraphType.Icons)
                        return;
                    break;
                case k_IconsPath:
                    if (type != ParagraphType.Icons)
                        return;
                    break;
                case k_CriteriaPath:
                case k_CompletionPath:
                    if (type != ParagraphType.Instruction)
                        return;
                    break;
                case k_SummaryPath:
                    if (type != ParagraphType.Instruction)
                        return;
                    break;
            }
            base.DisplayChildProperty(position, parentProperty, childProperty, label);
        }

        protected override float GetChildPropertyHeight(SerializedProperty parentProperty, SerializedProperty childProperty)
        {
            ParagraphType type = (ParagraphType)parentProperty.FindPropertyRelative(k_TypePath).intValue;
            switch (childProperty.name)
            {
                case k_TextPath:
                    if (type == ParagraphType.Icons)
                        return -EditorGUIUtility.standardVerticalSpacing;
                    break;
                case k_IconsPath:
                    if (type != ParagraphType.Icons)
                        return -EditorGUIUtility.standardVerticalSpacing;
                    break;
                case k_CriteriaPath:
                    if (type != ParagraphType.Instruction)
                        return -EditorGUIUtility.standardVerticalSpacing;
                    break;
                case k_SummaryPath:
                    if (type != ParagraphType.Instruction)
                        return -EditorGUIUtility.standardVerticalSpacing;
                    break;
            }
            return base.GetChildPropertyHeight(parentProperty, childProperty);
        }
    }
}
