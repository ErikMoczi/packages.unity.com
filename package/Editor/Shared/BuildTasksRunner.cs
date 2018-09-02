using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Basic static class containing default implementations for BuildTask validation and running.
    /// </summary>
    public static class BuildTasksRunner
    {
        /// <summary>
        /// Basic run implementation that takes a set of tasks, a context, and runs returning the build results.
        /// <seealso cref="IBuildTask"/>, <seealso cref="IBuildContext"/>, and <seealso cref="ReturnCodes"/>
        /// </summary>
        /// <param name="pipeline">The set of build tasks to run.</param>
        /// <param name="context">The build context to use for this run.</param>
        /// <returns>Return code with status information about success or failure causes.</returns>
        public static ReturnCodes Run(IList<IBuildTask> pipeline, IBuildContext context)
        {
            IProgressTracker tracker;
            if (context.TryGetContextObject(out tracker))
                tracker.TaskCount = pipeline.Count;

            foreach (IBuildTask task in pipeline)
            {
                try
                {
                    if (!tracker.UpdateTaskUnchecked(task.GetType().Name.HumanReadable()))
                        return ReturnCodes.Canceled;

                    var result = task.Run(context);
                    if (result < ReturnCodes.Success)
                        return result;
                }
                catch (System.Exception e)
                {
                    BuildLogger.LogException(e);
                    return ReturnCodes.Exception;
                }
            }
            return ReturnCodes.Success;
        }

        /// <summary>
        /// Basic validate implementation that takes a set of tasks, a context, and does checks to ensure the task requirements are all satisfied.
        /// <seealso cref="IBuildTask"/>, <seealso cref="IBuildContext"/>, and <seealso cref="ReturnCodes"/>
        /// </summary>
        /// <param name="pipeline">The set of build tasks to run.</param>
        /// <param name="context">The build context to use for this run.</param>
        /// <returns>Return code with status information about success or failure causes.</returns>
        public static ReturnCodes Validate(IList<IBuildTask> pipeline, IBuildContext context)
        {
            var requiredTypes = new HashSet<Type>();
            foreach (IBuildTask task in pipeline)
                requiredTypes.UnionWith(task.RequiredContextTypes);

            var missingTypes = new List<string>();
            foreach (Type requiredType in requiredTypes)
            {
                if (!context.ContainsContextObject(requiredType))
                    missingTypes.Add(requiredType.Name);
            }

            if (missingTypes.Count > 0)
            {
                BuildLogger.LogError("Missing required object types to run build pipeline:\n{0}", string.Join(", ", missingTypes.ToArray()));
                return ReturnCodes.MissingRequiredObjects;
            }
            return ReturnCodes.Success;
        }
    }
}