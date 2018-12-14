
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal interface ICollectionAdapter<TContainer, TValue> : ICustomUIAdapter
        where TContainer : IPropertyContainer
    {
        bool BeginCollection(ref TContainer container, ref UIVisitContext<IList<TValue>> context);
        void EndCollection(ref TContainer container, ref UIVisitContext<IList<TValue>> context);
    }

    internal interface ICollectionAdapter<TContainer> : ICustomUIAdapter
        where TContainer : IPropertyContainer
    {
        bool BeginCollection<TValue>(ref TContainer container, ref UIVisitContext<IList<TValue>> context);
        void EndCollection<TValue>(ref TContainer container, ref UIVisitContext<IList<TValue>> context);
    }

    internal interface IClassCollectionAdapter<TValue> : ICustomUIAdapter
    {
        bool BeginClassCollection<TContainer>(ref TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : class, IPropertyContainer;
        void EndClassCollection<TContainer>(ref TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : class, IPropertyContainer;
    }

    internal interface IStructCollectionAdapter<TValue> : ICustomUIAdapter
    {
        bool BeginStructCollection<TContainer>(ref TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : struct, IPropertyContainer;
        void EndStructCollection<TContainer>(ref TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : struct, IPropertyContainer;
    }

    internal interface ICollectionValueAdapter<TValue> : IClassCollectionAdapter<TValue>, IStructCollectionAdapter<TValue>
    {
    }

    internal interface IClassCollectionAdapter : ICustomUIAdapter
    {
        bool BeginClassCollection<TContainer, TValue>(TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : class, IPropertyContainer;
        void EndClassCollection<TContainer, TValue>(TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : class, IPropertyContainer;
    }

    internal interface IStructCollectionAdapter : ICustomUIAdapter
    {
        bool BeginStructCollection<TContainer, TValue>(TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : struct, IPropertyContainer;
        void EndStructCollection<TContainer, TValue>(TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : struct, IPropertyContainer;
    }

    internal interface IGenericCollectionAdapter : IClassCollectionAdapter, IStructCollectionAdapter
    {
    }
}
