using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Planner;
using UnityEngine;

namespace UnityEngine.AI.Planner.DomainLanguage.TraitBased
{
    [Serializable]
    class DomainObjectData : INamedData, IEquatable<DomainObjectData>
    {
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public GameObject SourceObject
        {
            get => m_SourceObject;
            set => m_SourceObject = value;
        }

        public IEnumerable<TraitObjectData> TraitData
        {
            get => m_TraitData;
            set => m_TraitData = value.ToList();
        }

        [SerializeField]
        string m_Name;

        [SerializeField]
        GameObject m_SourceObject;

        [SerializeField]
        List<TraitObjectData> m_TraitData;


        public bool Equals(DomainObjectData other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null || m_TraitData.Count != other.m_TraitData.Count)
                return false;

            foreach (var traitObjectData in m_TraitData)
            {
                var foundMatch = false;
                foreach (var otherTraitObjectData in other.m_TraitData)
                {
                    if (traitObjectData.Equals(otherTraitObjectData))
                    {
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch)
                    return false;
            }

            return true;
        }
    }
}
