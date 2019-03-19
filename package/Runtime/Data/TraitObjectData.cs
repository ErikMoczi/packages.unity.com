using System;
using System.Collections.Generic;
using System.Text;
using Unity.AI.Planner;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEngine.AI.Planner.DomainLanguage.TraitBased
{
    [Serializable]
    class TraitObjectData : IEquatable<TraitObjectData>, INamedData
    {
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public string TraitDefinitionName => m_TraitDefinitionName;
        public TraitDefinition TraitDefinition => m_TraitDefinition;

        string m_Name;

        [UsesTraitDefinition(true)]
        [SerializeField]
        string m_TraitDefinitionName;

        [SerializeField]
        List<FieldValue> m_FieldValues;

        TraitDefinition m_TraitDefinition;
        Dictionary<string, FieldValue> m_Fields = new Dictionary<string, FieldValue>();
        Dictionary<string, Type> m_FieldTypes = new Dictionary<string, Type>();

        public TraitObjectData(TraitDefinition traitDefinition, DomainDefinition domainDefinition)
        {
            InitializeFieldValues(traitDefinition, domainDefinition);
        }

        // Hide default constructor to require explicit construction
        TraitObjectData() { }

        public void ClearFieldValues()
        {
            m_FieldValues = null;
            m_FieldTypes.Clear();
            m_Fields.Clear();
        }

        public void InitializeFieldValues(TraitDefinition traitDefinition, DomainDefinition domainDefinition)
        {
            m_TraitDefinition = traitDefinition;
            m_TraitDefinitionName = m_TraitDefinition.Name;

            m_FieldTypes.Clear();
            m_Fields.Clear();

            // Field values could have been set in the editor
            var fieldValues = m_FieldValues ?? new List<FieldValue>();
            foreach (var f in m_TraitDefinition.Fields)
            {
                m_FieldTypes.Add(f.Name, domainDefinition.GetType(f.Type));

                if (m_FieldValues == null)
                    fieldValues.Add(new FieldValue { Name = f.Name });
            }


            m_FieldValues = fieldValues;
            foreach (var fv in m_FieldValues)
                m_Fields.Add(fv.Name, fv);
        }

        public T GetValue<T>(string fieldName)
        {
            return (T)GetValue(fieldName);
        }

        public bool TryGetValue<T>(string fieldName, out T value) where T: class
        {
            if (m_FieldTypes.TryGetValue(fieldName, out var fieldType))
            {
                if (fieldType == typeof(T))
                {
                    value = GetValue<T>(fieldName);
                    return true;
                }

                if (m_Fields.TryGetValue(fieldName, out var fv))
                {
                    value = (T)fv.GetValue(typeof(T));
                    return true;
                }
            }

            value = default;
            return false;
        }

        public void SetValue(string fieldName, object value)
        {
            var fieldType = m_FieldTypes[fieldName];

            if (value == null)
                return;

            if (value.GetType() != fieldType)
                throw new InvalidCastException(fieldName);

            var fieldValue = m_Fields[fieldName];
            fieldValue.Name = fieldName;
            if (fieldType.IsEnum)
                fieldValue.IntValue = (int) value;
            else if (fieldType == typeof(bool) || fieldType == typeof(Bool))
                fieldValue.BoolValue = (bool)value;
            else if (fieldType == typeof(float))
                fieldValue.FloatValue = (float)value;
            else if (fieldType == typeof(long))
                fieldValue.IntValue = (long)value;
            else if (fieldType == typeof(string))
                fieldValue.StringValue = (string)value;
            else
                fieldValue.ObjectValue = (UnityObject)value;
        }

        public static bool operator ==(TraitObjectData a, TraitObjectData b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (ReferenceEquals(a, null))
                return false;

            if (ReferenceEquals(b, null))
                return false;

            if (a.m_TraitDefinitionName != b.m_TraitDefinitionName)
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(TraitObjectData a, TraitObjectData b)
        {
            return !(a == b);
        }

        public bool Equals(TraitObjectData other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (m_FieldValues.Count != other.m_FieldValues.Count)
                return false;

            foreach (var fv in m_FieldValues)
            {
                if (!GetValue(fv.Name).Equals(other.GetValue(fv.Name)))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(TraitObjectData))
                return false;

            return this == (TraitObjectData)obj;
        }

        public override int GetHashCode()
        {
            var hashCode = 0;

            foreach (var fv in m_FieldValues)
            {
                var value = GetValue(fv.Name);
                if (value != null)
                    hashCode ^= value.GetHashCode();
            }

            if (hashCode == 0)
                hashCode = base.GetHashCode();

            return hashCode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var fv in m_FieldValues)
            {
                var value = GetValue(fv.Name);
                sb.AppendFormat("{0}: {1} ", fv.Name, value ?? string.Empty);
            }

            return sb.ToString();
        }

        public object GetValue(string fieldName)
        {
            object value = null;
            if (m_FieldTypes.TryGetValue(fieldName, out Type fieldType))
            {
                if (m_Fields.TryGetValue(fieldName, out var fieldValue))
                    value = fieldValue.GetValue(fieldType);
            }

            return value;
        }
    }
}
