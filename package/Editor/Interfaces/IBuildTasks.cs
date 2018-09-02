using System;

namespace UnityEditor.Build.Interfaces
{
    public interface IBuildTask
    {
        int Version { get; }

        Type[] RequiredContextTypes { get; }

        ReturnCodes Run(IBuildContext context);
    }
}
