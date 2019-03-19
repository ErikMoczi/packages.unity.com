using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.AI.Planner.DomainLanguage.TraitBased
{
    [Serializable]
    class TraitDefinition : INamedData, IEquatable<TraitDefinition>
    {
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public bool Dynamic
        {
            get => m_Dynamic;
            set => m_Dynamic = value;
        }

        public IEnumerable<TraitDefinitionField> Fields
        {
            get => m_Fields;
            set => m_Fields = value.ToList();
        }

        [SerializeField]
        string m_Name;

        [SerializeField]
        [Tooltip("Declare a dynamic type (i.e. one that does not exist in a source file)")]
        bool m_Dynamic = true;

        [SerializeField]
        List<TraitDefinitionField> m_Fields = new List<TraitDefinitionField>();

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(TraitDefinition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(m_Name, other.m_Name) && m_Dynamic == other.m_Dynamic && Equals(m_Fields, other.m_Fields);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TraitDefinition)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (m_Name != null ? m_Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ m_Dynamic.GetHashCode();
                hashCode = (hashCode * 397) ^ (m_Fields != null ? m_Fields.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
