using System;
using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Debuging
{
    internal interface IDebugContext
    {
        string GetContextString(string separator);
        void SetParentContext(IDebugContext parent);
    }
    internal class DebugContextString : IDebugContext
    {
        public string Message;
        public IDebugContext Parent;
        public DebugContextString(string message)
        {
            Message = message;
        }

        string IDebugContext.GetContextString(string separator)
        {
            if (Parent != null)
            {
                return Message + Parent.GetContextString(separator);
            }
            return Message;
        }

        void IDebugContext.SetParentContext(IDebugContext parent)
        {
            Parent = parent;
        }
    }
    internal class DebugContextFunc : IDebugContext
    {
        public Func<string> Function;
        public IDebugContext Parent;
        public DebugContextFunc(Func<string> function)
        {
            Function = function;
        }

        string IDebugContext.GetContextString(string separator)
        {
            if (Parent != null)
            {
                return Function() + separator + Parent.GetContextString(separator);
            }
            return Function();
        }

        void IDebugContext.SetParentContext(IDebugContext parent)
        {
            Parent = parent;
        }
    }

    internal interface IDebugContextService
    {
        void Push(IDebugContext context);
        IDebugContext Pop();
        IDebugContext GetCurrent();
    }

    internal class DebugContextService : IDebugContextService
    {
        public List<IDebugContext> Context = new List<IDebugContext>();
        void IDebugContextService.Push(IDebugContext context)
        {
            if (Context.Count > 0)
            {
                context.SetParentContext(Context[Context.Count - 1]);
            }
            Context.Add(context);
        }

        IDebugContext IDebugContextService.Pop()
        {
            var context = Context[Context.Count - 1];
            Context.RemoveAt(Context.Count - 1);
            return context;
        }

        IDebugContext IDebugContextService.GetCurrent()
        {
            if (Context.Count == 0) return null;
            return Context[Context.Count - 1];
        }
    }

    // RAII class for scoping IDebugContext. Must be instantiated into a using statement
    internal class ScopeDebugContext : IDisposable
    {
        private IDebugContext m_Context;
        public ScopeDebugContext(IDebugContext context)
        {
            m_Context = context;
            Service<IDebugContextService>.Current.Push(context);
        }

        void IDisposable.Dispose()
        {
            var context = Service<IDebugContextService>.Current.Pop();
            if (context != m_Context)
            {
                throw new InvalidOperationException("ScopeDebugContext popped wrong context, make sure all DebugContext are push/pop in a valid stack order.");
            }
        }

        // Helper method to reduce boilerplate code.
        public static ScopeDebugContext Func(Func<string> debugFunc)
        {
            return new ScopeDebugContext(new DebugContextFunc(debugFunc));
        }

        // Helper method to reduce boilerplate code.
        public static ScopeDebugContext String(string str)
        {
            return new ScopeDebugContext(new DebugContextString(str));
        }
    }
}
