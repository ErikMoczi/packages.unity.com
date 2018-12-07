

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    internal interface IFamilyManager : IContextManager
    {

    }

    internal interface IFamilyManagerInternal : IFamilyManager
    {
        IEnumerable<ComponentFamily> AllFamilies { get; }
        void GetFamilies(TinyEntity entity, List<ComponentFamily> families);
    }

    [ContextManager(ContextUsage.Edit), UsedImplicitly]
    internal class FamilyManager : ContextManager, IFamilyManagerInternal
    {
        private readonly List<ComponentFamily> m_AllFamilies = new List<ComponentFamily>();
        private readonly Dictionary<Type, ComponentFamily> m_FamilyMap = new Dictionary<Type, ComponentFamily>();
        private readonly Dictionary<Type, ComponentFamily> m_ExtendedBy = new Dictionary<Type, ComponentFamily>();

        public FamilyManager(TinyContext context)
            : base(context) { }

        public IEnumerable<ComponentFamily> AllFamilies => m_AllFamilies;

        public override void Load()
        {
            // Create instances
            foreach (var typeAttribute in TinyAttributeScanner.GetTypeAttributes<ComponentFamilyAttribute>())
            {
                var type = typeAttribute.Type;
                var attribute = typeAttribute.Attribute;

                var definition = attribute.CreateFamilyDefinition(Context.Registry);
                if (definition.Required.Length == 0)
                {
                    Debug.LogError($"{TinyConstants.ApplicationName}: A component family must have at least one required component.");
                    continue;
                }
                var family = (ComponentFamily)Activator.CreateInstance(type, definition, Context);
                m_AllFamilies.Add(family);
                m_FamilyMap.Add(type, family);
            }

            // Set extensions
            foreach (var typeAttribute in TinyAttributeScanner.GetTypeAttributes<ExtendedComponentFamilyAttribute>())
            {
                var type = typeAttribute.Type;
                var extended = typeAttribute.Attribute.Extends;

                if (!m_FamilyMap.TryGetValue(extended, out var extendedFamily))
                {
                    Debug.LogError($"{TinyConstants.ApplicationName}: Could not extend family of type {extended.Name} using {type.Name}, since {extended.Name} has not been registered.");
                    continue;
                }

                if (!m_FamilyMap.TryGetValue(type, out var family))
                {
                    Debug.LogError($"{TinyConstants.ApplicationName}: Cannot create extension of family {extended.Name}, since {type.Name} has not been registered.");
                    continue;
                }

                if (extendedFamily.Definition.Required.Intersect(family.Definition.Required).Count() != extendedFamily.Definition.Required.Length)
                {
                    Debug.LogError($"{TinyConstants.ApplicationName}: Cannot create extension of family {extended.Name}, since {type.Name} is not a superset of {extended.Name}.");
                    continue;
                }

                m_ExtendedBy.Add(extended, family);
            }


            // Set which component should be skipped for drawing.
            foreach (var typeAttribute in TinyAttributeScanner.GetTypeAttributes<ComponentFamilyHiddenAttribute>())
            {
                var type = typeAttribute.Type;
                if (!m_FamilyMap.TryGetValue(type, out var family))
                {
                    Debug.LogError($"{TinyConstants.ApplicationName}: Cannot set skipped component drawing for type {type.Name} because it wasn't registered.");
                    continue;
                }
                family.MarkAsHidden(typeAttribute.Attribute.Hidden);
            }
        }

        public void GetFamilies(TinyEntity entity, List<ComponentFamily> families)
        {
            families.Clear();
            foreach (var family in m_AllFamilies)
            {
                if (family.Refresh(entity))
                {
                    families.Add(family);
                }
            }
            families.Sort((lhs, rhs) => entity.GetComponentIndex(lhs.Definition.Required[0]).CompareTo(entity.GetComponentIndex(rhs.Definition.Required[0])));
            var list = ListPool<ComponentFamily>.Get();
            try
            {
                list.AddRange(families);
                families.RemoveAll(cf => m_ExtendedBy.TryGetValue(cf.GetType(), out var extended) && list.Contains(extended));
            }
            finally
            {
                ListPool<ComponentFamily>.Release(list);
            }
        }
    }
}
