using System;
using System.Collections.Generic;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build
{
    public static class BuildTasksRunner
    {
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