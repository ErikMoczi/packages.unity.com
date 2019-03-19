using System;
using UnityEngine;

namespace UnityEngine.AI.Planner.DomainLanguage.TraitBased
{
    class UsesTraitDefinitionAttribute : PropertyAttribute
    {
        public bool ShowLabel => m_ShowLabel;

        bool m_ShowLabel;

        public UsesTraitDefinitionAttribute(bool showLabel = false)
        {
            m_ShowLabel = showLabel;
        }
    }
}
