
namespace Unity.Tiny
{
    internal interface IContext
    {
        TManager GetManager<TManager>()
            where TManager : class, IContextManager;
        
        TinyCaretaker Caretaker { get; }
    }
}
