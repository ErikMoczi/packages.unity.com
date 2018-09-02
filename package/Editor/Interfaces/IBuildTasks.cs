using System;

namespace UnityEditor.Build.Pipeline.Interfaces
{
    /// <summary>
    /// Base interface of all build tasks.
    /// </summary>
    public interface IBuildTask
    {
        /// <summary>
        /// Version identifier for the build task.
        /// Primarily for caching.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// List of required data types in <see cref="IBuildContext"/> for this task to run correctly.
        /// Primarily used to early out due to not having all the required data.
        /// </summary>
        Type[] RequiredContextTypes { get; }

        /// <summary>
        /// Generic run method for build tasks
        /// </summary>
        /// <param name="context"><seealso cref="IBuildContent"/> that contains the required data types to run.</param>
        /// <returns>Return code with status information about success or failure causes.</returns>
        ReturnCode Run(IBuildContext context);
    }
}
