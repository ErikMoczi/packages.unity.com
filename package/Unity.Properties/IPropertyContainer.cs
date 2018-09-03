#if (NET_4_6 || NET_STANDARD_2_0)

namespace Unity.Properties
{
    public interface IPropertyContainer
    {
        IVersionStorage VersionStorage { get; }
        IPropertyBag PropertyBag { get; }
    }
    
    public delegate void ByRef<TContainer, in TContext>(ref TContainer container, TContext context)
        where TContainer : struct, IPropertyContainer;
    
    public delegate TReturn ByRef<TContainer, in TContext, out TReturn>(ref TContainer container, TContext context)
        where TContainer : struct, IPropertyContainer;

    public interface IStructPropertyContainer : IPropertyContainer
    {
        
    }
    
    public interface IStructPropertyContainer<TContainer> : IStructPropertyContainer
        where TContainer : struct, IPropertyContainer
    {
        void MakeRef<TContext>(ByRef<TContainer, TContext> method, TContext context);
        TReturn MakeRef<TContext, TReturn>(ByRef<TContainer, TContext, TReturn> method, TContext context);
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)