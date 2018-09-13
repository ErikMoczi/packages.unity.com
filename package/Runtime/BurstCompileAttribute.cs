using System;
using Unity.Burst;

namespace Unity.Burst
{
    public enum Support
    {
        Strict,
        Relaxed
    }

    public enum Accuracy
    {
        Std,
        Low,
        Med,
        High,
    }

    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct|AttributeTargets.Method)]
    public class BurstCompileAttribute : System.Attribute
    {
        public Support Support { get; set; }

        public Accuracy Accuracy { get; set; }

        public bool CompileSynchronously { get; set; }

        public string Backend { get; set; }

        public string[] Options { get; set; }

        public BurstCompileAttribute()
        {
            Support = Support.Strict;
            Accuracy = Accuracy.Std;
        }

        public BurstCompileAttribute(Accuracy accuracy, Support support)
        {
            Support = support;
            Accuracy = accuracy;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class NoAliasAttribute : System.Attribute
    {
    }
}

namespace Unity.Jobs
{
    [Obsolete("Use Unity.Burst.BurstCompileAttribute instead")]
    public class ComputeJobOptimizationAttribute : BurstCompileAttribute
    {
        public ComputeJobOptimizationAttribute()
        {
        }

        public ComputeJobOptimizationAttribute(Accuracy accuracy, Support support) : base(accuracy, support)
        {
        }
    }
}
