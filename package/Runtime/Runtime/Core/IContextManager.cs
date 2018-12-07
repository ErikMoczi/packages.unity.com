

namespace Unity.Tiny
{
    internal interface IContextManager
    {
        IRegistry Registry { get; }

        void Load();
        void Unload();
    }

    internal abstract class ContextManager : IContextManager
    {
        public TinyContext Context { get; }
        public IRegistry Registry { get; }

        protected ContextManager(TinyContext context)
        {
            Context = context;
            Registry = Context.Registry;
        }

        public virtual void Load() {}

        public virtual void Unload() {}

    }
}
