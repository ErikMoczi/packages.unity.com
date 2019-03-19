using System;
using UnityEngine;

namespace UnityEngine.AI.Planner.DomainLanguage.TraitBased
{
    [Serializable]
    class TraitDefinitionField : IEquatable<TraitDefinitionField>
    {
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public string Type
        {
            get => m_Type;
            set => m_Type = value;
        }

        public Type FieldType
        {
            get
            {
                if (m_FieldType == null)
                    m_FieldType = System.Type.GetType(Type);

                return m_FieldType;
            }
            set
            {
                m_FieldType = value;
                Type = value.FullName;
            }
        }

        [SerializeField]
        string m_Name;

        [SerializeField]
        string m_Type;

        Type m_FieldType;

        public bool Equals(TraitDefinitionField other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(m_Name, other.m_Name) && string.Equals(m_Type, other.m_Type) && m_FieldType == other.m_FieldType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TraitDefinitionField)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (m_Name != null ? m_Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Type != null ? m_Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_FieldType != null ? m_FieldType.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
