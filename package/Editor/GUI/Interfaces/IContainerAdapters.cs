
using Unity.Properties;

namespace Unity.Tiny
{
    internal interface IContainerAdapter<TContainer, TValue> : ICustomUIAdapter
        where TContainer : IPropertyContainer
    {
        bool BeginContainer(ref TContainer container, ref UIVisitContext<TValue> context);
        void EndContainer(ref TContainer container, ref UIVisitContext<TValue> context);
    }

    internal interface IContainerAdapter<TContainer> : ICustomUIAdapter
        where TContainer : IPropertyContainer
    {
        bool BeginContainer<TValue>(ref TContainer container, ref UIVisitContext<TValue> context);
        void EndContainer<TValue>(ref TContainer container, ref UIVisitContext<TValue> context);
    }

    internal interface IClassContainerAdapter<TValue> : ICustomUIAdapter
    {
        bool BeginClassContainer<TContainer>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;

        void EndClassContainer<TContainer>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
    }

    internal interface IStructContainerAdapter<TValue> : ICustomUIAdapter
    {
        bool BeginStructContainer<TContainer>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;

        void EndStructContainer<TContainer>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
    }

    internal interface IContainerValueAdapter<TValue> : IClassContainerAdapter<TValue>, IStructContainerAdapter<TValue>
    {
    }

    internal interface IClassContainerAdapter : ICustomUIAdapter
    {
        bool BeginClassContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
            where TValue : IPropertyContainer;

        void EndClassContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
            where TValue : IPropertyContainer;
    }

    internal interface IStructContainerAdapter : ICustomUIAdapter
    {
        bool BeginStructContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
            where TValue : IPropertyContainer;

        void EndStructContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
            where TValue : IPropertyContainer;
    }

    internal interface IClassStructContainerGenericUIAdapter : IClassContainerAdapter, IStructContainerAdapter
    {
    }
}
