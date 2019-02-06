

using System;
using System.Collections.Generic;

namespace Unity.Tiny.Serialization
{
    /// <summary>
    /// Helper class to identify the current scope of serialization
    /// </summary>
    internal static class SerializationContext
    {
        private const string Default = "__default__";

        public const string Persistence = "__persistence__";
        public const string UndoRedo = "__undoredo__";
        public const string Temp = "__tempcontext__";
        
        private class SerializationContextScope : IDisposable
        {
            public SerializationContextScope(string identifier)
            {
                s_SerializationContextStack.Push(identifier);
            }

            public void Dispose()
            {
                s_SerializationContextStack.Pop();
            }
        }
        
        private static readonly Stack<string> s_SerializationContextStack = new Stack<string>();
        
        public static string CurrentContext => s_SerializationContextStack.Peek();

        static SerializationContext()
        {
            s_SerializationContextStack.Push(Default);
        }

        public static IDisposable Scope(string identifier)
        {
            return new SerializationContextScope(identifier);
        }
    }
}

