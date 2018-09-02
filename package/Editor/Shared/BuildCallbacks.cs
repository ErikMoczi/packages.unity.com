using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Basic implementation of IDependencyCallback, IPackingCallback, IWritingCallback, & IScriptsCallback.
    /// Uses Func implementation for callbacks. <seealso cref="IDependencyCallback"/>, <seealso cref="IPackingCallback"/>
    /// <seealso cref="IWritingCallback"/>, and <seealso cref="IScriptsCallback"/>
    /// </summary>
    public class BuildCallbacks : IDependencyCallback, IPackingCallback, IWritingCallback, IScriptsCallback
    {
        /// <summary>
        /// Func delegate for the callback after scripts have been compiled.
        /// </summary>
        public Func<IBuildParameters, IBuildResults, ReturnCodes> PostScriptsCallbacks { get; set; }
        
        /// <summary>
        /// Func delegate for the callback after dependency calculation has occurred.
        /// </summary>
        public Func<IBuildParameters, IDependencyData, ReturnCodes> PostDependencyCallback { get; set; }
        
        /// <summary>
        /// Func delegate for the callback after packing has occurred.
        /// </summary>
        public Func<IBuildParameters, IDependencyData, IWriteData, ReturnCodes> PostPackingCallback { get; set; }
        
        /// <summary>
        /// Func delegate for the callback after writing content has occurred.
        /// </summary>
        public Func<IBuildParameters, IDependencyData, IWriteData, IBuildResults, ReturnCodes> PostWritingCallback { get; set; }
        
        /// <inheritdoc />
        public ReturnCodes PostScripts(IBuildParameters buildParameters, IBuildResults buildResults)
        {
            if (PostScriptsCallbacks != null)
                return PostScriptsCallbacks(buildParameters, buildResults);
            return ReturnCodes.Success;
        }
        
        /// <inheritdoc />
        public ReturnCodes PostDependency(IBuildParameters buildParameters, IDependencyData dependencyData)
        {
            if (PostDependencyCallback != null)
                return PostDependencyCallback(buildParameters, dependencyData);
            return ReturnCodes.Success;
        }
        
        /// <inheritdoc />
        public ReturnCodes PostPacking(IBuildParameters buildParameters, IDependencyData dependencyData, IWriteData writeData)
        {
            if (PostPackingCallback != null)
                return PostPackingCallback(buildParameters, dependencyData, writeData);
            return ReturnCodes.Success;
        }
        
        /// <inheritdoc />
        public ReturnCodes PostWriting(IBuildParameters buildParameters, IDependencyData dependencyData, IWriteData writeData, IBuildResults buildResults)
        {
            if (PostWritingCallback != null)
                return PostWritingCallback(buildParameters, dependencyData, writeData, buildResults);
            return ReturnCodes.Success;
        }
    }
}
