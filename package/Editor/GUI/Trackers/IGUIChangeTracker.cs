
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal interface IGUIChangeTracker
    {
        IReadOnlyList<List<IPropertyContainer>> Resolvers { get; }
        
        bool HasChanges { get; }
        void PushChange(IPropertyContainer container, IProperty property);
        void PropagateChanges();
        void ClearChanges();

        void ClearResolvers();
        void PushResolvers(List<IPropertyContainer> resolvers);
        void PopResolvers();

        bool HasMixedValues<TValue>(IPropertyContainer container, IProperty property);
        bool ValuesAreDifferent<TValue>(TValue left, TValue right);
    }
}
