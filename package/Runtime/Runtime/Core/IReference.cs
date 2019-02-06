
namespace Unity.Tiny
{
    internal interface IReference : IIdentified<TinyId>, INamed
    {
    }

    internal interface IReference<out T> : IReference where T : class
    {
        T Dereference(IRegistry registry);
    }
    
}
