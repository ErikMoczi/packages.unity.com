using System;
using Unity.Burst;

namespace Unity.Burst
{
    // FloatMode and FloatPrecision must be kept in sync with burst.h / Burst.Backend

    public enum FloatMode
    {
        Default = 0,
        Strict = 1,
        Deterministic = 2,
        Fast = 3,
    }

    public enum FloatPrecision
    {
        Standard = 0,
        High = 1,
        Medium = 2,
        Low = 3,
    }

    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct|AttributeTargets.Method)]
    public class BurstCompileAttribute : System.Attribute
    {
        public FloatMode FloatMode { get; set; }

        public FloatPrecision FloatPrecision { get; set; }

        public bool CompileSynchronously { get; set; }

        public string[] Options { get; set; }

        public BurstCompileAttribute()
        {
        }

        public BurstCompileAttribute(FloatPrecision floatPrecision, FloatMode floatMode)
        {
            FloatMode = floatMode;
            FloatPrecision = floatPrecision;
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

        public ComputeJobOptimizationAttribute(FloatPrecision floatPrecision, FloatMode floatMode) : base(floatPrecision, floatMode)
        {
        }
    }
}
