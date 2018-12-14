

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ContextManager(ContextUsage.Edit | ContextUsage.LiveLink)]
    internal class ComponentRequirementsManager : ContextManager
    {
        #region Static
        private static readonly Dictionary<TinyId, TinyComponentRequirement> k_IdToRequirements = new Dictionary<TinyId, TinyComponentRequirement>();

        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Init()
        {
            foreach (var typeAttribute in TinyAttributeScanner.GetTypeAttributes<TinyRequiredComponentAttribute>())
            {
                k_IdToRequirements.Add(typeAttribute.Attribute.Id, (TinyComponentRequirement)Activator.CreateInstance(typeAttribute.Type));
            }
        }
        #endregion

        #region API
        public ComponentRequirementsManager(TinyContext context)
            : base(context)
        {
        }

        public void AddRequiredComponent(TinyEntity entity, TinyType.Reference addedComponent)
        {
            if (k_IdToRequirements.TryGetValue(addedComponent.Id, out var requirements))
            {
                requirements.AddRequiredComponents(entity);
            }
        }
        #endregion
    }
}

