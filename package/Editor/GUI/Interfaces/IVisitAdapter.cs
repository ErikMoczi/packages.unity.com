
using Unity.Properties;

namespace Unity.Tiny
{
    internal interface IVisitAdapter<TContainer, TValue> : ICustomUIAdapter
        where TContainer : IPropertyContainer
    {
        bool CustomVisit(ref TContainer container, ref UIVisitContext<TValue> context);
    }

    internal interface IVisitAdapter<TContainer> : ICustomUIAdapter
        where TContainer : IPropertyContainer
    {
        bool CustomVisit<TValue>(ref TContainer container, ref UIVisitContext<TValue> context);
    }

    internal interface IClassVisitAdapter<TValue> : ICustomUIAdapter
    {
        bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
    }

    internal interface IStructVisitAdapter<TValue> : ICustomUIAdapter
    {
        bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
    }

    internal interface IVisitValueAdapter<TValue> : IClassVisitAdapter<TValue>, IStructVisitAdapter<TValue>
    {
    }

    internal interface IClassVisitAdapter : ICustomUIAdapter
    {
        bool CustomClassVisit<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
    }

    internal interface IStructVisitAdapter : ICustomUIAdapter
    {
        bool CustomStructVisit<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
    }

    internal interface IGenericVisitAdapter : IClassVisitAdapter, IStructVisitAdapter
    {
    }

    internal interface IClassUnsupportedAdapter : ICustomUIAdapter
    {
        bool UnsupportedClass<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
    }

    internal interface IStructUnsupportedAdapter : ICustomUIAdapter
    {
        bool UnsupportedStruct<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
    }

    internal interface IGenericUnsupportedAdapter : IClassUnsupportedAdapter, IStructUnsupportedAdapter
    {
    }
}
