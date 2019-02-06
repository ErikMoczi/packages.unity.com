
using Unity.Properties;

namespace Unity.Tiny
{
    internal interface IExcludeAdapter<TContainer, TValue> : ICustomUIAdapter
        where TContainer : IPropertyContainer
    {
        bool ExcludeVisit(ref TContainer container, ref UIVisitContext<TValue> context);
    }

    internal interface IExcludeAdapter<TContainer> : ICustomUIAdapter
        where TContainer : IPropertyContainer
    {
        bool ExcludeVisit<TValue>(ref TContainer container, ref UIVisitContext<TValue> context);
    }

    internal interface IClassExcludeAdapter<TValue> : ICustomUIAdapter
    {
        bool ExcludeClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
    }

    internal interface IStructExcludeAdapter<TValue> : ICustomUIAdapter
    {
        bool ExcludeStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
    }

    internal interface IExcludeValueAdapter<TValue> : IClassExcludeAdapter<TValue>, IStructExcludeAdapter<TValue>
    {
    }

    internal interface IClassExcludeAdapter : ICustomUIAdapter
    {
        bool ExcludeClassVisit<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
    }

    internal interface IStructExcludeAdapter : ICustomUIAdapter
    {
        bool ExcludeStructVisit<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
    }

    internal interface IGenericExcludeAdapter : IClassExcludeAdapter, IStructExcludeAdapter
    {
    }
}
