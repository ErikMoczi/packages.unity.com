using System;
using UnityEditor.Build.Interfaces;

namespace UnityEditor.Build
{
    public class BuildCallbacks : IDependencyCallback, IPackingCallback, IWritingCallback, IScriptsCallback
    {
        public Func<IBuildParameters, IBuildResults, ReturnCodes> PostScriptsCallbacks { get; set; }
        public Func<IBuildParameters, IDependencyData, ReturnCodes> PostDependencyCallback { get; set; }
        public Func<IBuildParameters, IDependencyData, IWriteData, ReturnCodes> PostPackingCallback { get; set; }
        public Func<IBuildParameters, IDependencyData, IWriteData, IBuildResults, ReturnCodes> PostWritingCallback { get; set; }

        public ReturnCodes PostScripts(IBuildParameters buildParameters, IBuildResults buildResults)
        {
            if (PostScriptsCallbacks != null)
                return PostScriptsCallbacks(buildParameters, buildResults);
            return ReturnCodes.Success;
        }

        public ReturnCodes PostDependency(IBuildParameters buildParameters, IDependencyData dependencyData)
        {
            if (PostDependencyCallback != null)
                return PostDependencyCallback(buildParameters, dependencyData);
            return ReturnCodes.Success;
        }

        public ReturnCodes PostPacking(IBuildParameters buildParameters, IDependencyData dependencyData, IWriteData writeData)
        {
            if (PostPackingCallback != null)
                return PostPackingCallback(buildParameters, dependencyData, writeData);
            return ReturnCodes.Success;
        }

        public ReturnCodes PostWriting(IBuildParameters buildParameters, IDependencyData dependencyData, IWriteData writeData, IBuildResults buildResults)
        {
            if (PostWritingCallback != null)
                return PostWritingCallback(buildParameters, dependencyData, writeData, buildResults);
            return ReturnCodes.Success;
        }
    }
}
