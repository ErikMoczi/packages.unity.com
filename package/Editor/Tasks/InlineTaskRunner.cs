using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class InlineTaskRunner : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        Func<IBuildContext, ReturnCode> m_Function;

        public InlineTaskRunner(Func<IBuildContext, ReturnCode> function)
        {
            m_Function = function;
        }

        public ReturnCode Run(IBuildContext context)
        {
            return m_Function(context);
        }
    }
}
