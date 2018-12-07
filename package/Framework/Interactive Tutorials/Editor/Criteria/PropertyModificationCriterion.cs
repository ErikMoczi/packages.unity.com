using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Unity.InteractiveTutorials
{
    public class PropertyModificationCriterion : Criterion
    {
        internal string propertyPath
        {
            get { return m_PropertyPath; }
            set { m_PropertyPath = value; }
        }
        [SerializeField]
        string m_PropertyPath;

        // TODO: Make this more like TypedCriterion
        internal string targetValue
        {
            get { return m_TargetValue; }
            set { m_TargetValue = value; }
        }
        [SerializeField]
        string m_TargetValue;

        internal TargetValueType targetValueType
        {
            get { return m_TargetValueType; }
            set { m_TargetValueType = value; }
        }
        [SerializeField]
        TargetValueType m_TargetValueType;

        internal SceneObjectReference target
        {
            get { return m_Target.sceneObjectReference; }
            set { m_Target.sceneObjectReference = value; }
        }
        [SerializeField]
        ObjectReference m_Target = new ObjectReference();

        public override void StartTesting()
        {
            var target = m_Target.sceneObjectReference.ReferencedObject;
            completed = PropertyMatchesTargetValue(target, m_PropertyPath);

            Undo.postprocessModifications += PostprocessModifications;
            Undo.undoRedoPerformed += UpdateCompletion;
        }

        public override void StopTesting()
        {
            Undo.postprocessModifications -= PostprocessModifications;
            Undo.undoRedoPerformed -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            var target = m_Target.sceneObjectReference.ReferencedObject;
            return PropertyMatchesTargetValue(target, m_PropertyPath);
        }

        UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            var target = m_Target.sceneObjectReference.ReferencedObject;
            var modificationsToTest = modifications
                .Select(m => m.currentValue)
                .Where(m => m.target == target && m.propertyPath == m_PropertyPath)
                .ToArray();
            // only update completion state if property of interest has changed
            if (modificationsToTest.Length > 0)
            {
                completed = modificationsToTest
                    .Any(m => PropertyMatchesTargetValue(m.target, m.propertyPath));
            }

            return modifications;
        }

        bool PropertyMatchesTargetValue(UnityObject target, string propertyPath)
        {
            if (target == null)
                return false;

            if (m_TargetValueType != TargetValueType.Text && string.IsNullOrEmpty(m_TargetValue))
                return true;

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyPath);

            if (property == null)
                return false;

            switch (m_TargetValueType)
            {
                case TargetValueType.Integer:
                {
                    int targetValue;
                    return property.propertyType == SerializedPropertyType.Integer &&
                        int.TryParse(m_TargetValue, out targetValue) &&
                        property.intValue == targetValue;
                }

                case TargetValueType.Decimal:
                {
                    float targetValue;
                    return property.propertyType == SerializedPropertyType.Float &&
                        float.TryParse(m_TargetValue, out targetValue) &&
                        Mathf.Approximately(property.floatValue, targetValue);
                }

                case TargetValueType.Boolean:
                {
                    bool targetValue;
                    return property.propertyType == SerializedPropertyType.Boolean &&
                        bool.TryParse(m_TargetValue, out targetValue) &&
                        property.boolValue == targetValue;
                }

                case TargetValueType.Text:
                    return property.propertyType == SerializedPropertyType.String &&
                        property.stringValue == m_TargetValue;

                default:
                    throw new NotImplementedException(string.Format("Unsupported target type '{0}'", m_TargetValueType));
            }
        }

        public override bool AutoComplete()
        {
            var target = m_Target.sceneObjectReference.ReferencedObject;
            if (target == null)
                return false;

            if (m_TargetValueType != TargetValueType.Text && string.IsNullOrEmpty(m_TargetValue))
                return false;

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(m_PropertyPath);

            if (property == null)
                return false;

            switch (m_TargetValueType)
            {
                case TargetValueType.Integer:
                {
                    if (property.propertyType != SerializedPropertyType.Integer)
                        return false;

                    int targetValue;
                    if (!int.TryParse(m_TargetValue, out targetValue))
                        return false;

                    property.intValue = targetValue;
                    break;
                }

                case TargetValueType.Decimal:
                {
                    if (property.propertyType != SerializedPropertyType.Float)
                        return false;

                    float targetValue;
                    if (!float.TryParse(m_TargetValue, out targetValue))
                        return false;

                    property.floatValue = targetValue;
                    break;
                }

                case TargetValueType.Text:
                {
                    if (property.propertyType != SerializedPropertyType.String)
                        return false;

                    property.stringValue = m_TargetValue;
                    break;
                }

                case TargetValueType.Boolean:
                {
                    if (property.propertyType != SerializedPropertyType.Boolean)
                        return false;

                    bool targetValue;
                    if (!bool.TryParse(m_TargetValue, out targetValue))
                        return false;

                    property.boolValue = targetValue;
                    break;
                }
            }

            serializedObject.ApplyModifiedProperties();

            return true;
        }

        internal enum TargetValueType
        {
            Integer,
            Decimal,
            Text,
            Boolean,
        }
    }
}
