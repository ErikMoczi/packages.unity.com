using System;
using System.Reflection;

namespace Unity.Burst.Editor
{
    internal class BurstCompileTarget
    {
        public BurstCompileTarget(MethodInfo method, Type jobType)
        {
            Method = method;
            JobType = jobType;
            SupportsBurst = BurstReflection.ExtractBurstCompilerOptions(JobType, out DefaultOptions);
        }

        /// <summary>
        /// The Execute method of the target's producer type.
        /// </summary>
        public readonly MethodInfo Method;

        /// <summary>
        /// The type of the actual job (i.e. BoidsSimulationJob).
        /// </summary>
        public readonly Type JobType;

        /// <summary>
        /// The default compiler options
        /// </summary>
        public readonly string DefaultOptions;

        /// <summary>
        /// Set to true if burst compilation is possible.
        /// </summary>
        public readonly bool SupportsBurst;

        /// <summary>
        /// Generated disassembly, or null if disassembly failed
        /// </summary>
        public string[] Disassembly;

        public string GetDisplayName()
        {
            return JobType.ToString();
        }
    }
}
