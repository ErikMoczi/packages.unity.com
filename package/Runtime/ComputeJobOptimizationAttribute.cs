namespace Unity.Jobs
{
    public enum Support
    {
        Strict,
        Relaxed
    }

    public enum Accuracy
    {
        Low,
        Med,
        High,
        Std
    }

    public class ComputeJobOptimizationAttribute : System.Attribute
    {
        public Support  Support              { get; set; }
        public Accuracy Accuracy             { get; set; }
        public bool     CompileSynchronously { get; set; }

        public ComputeJobOptimizationAttribute()
        {
            Support = Support.Strict;
            Accuracy = Accuracy.Std;
        }

        public ComputeJobOptimizationAttribute(Accuracy accuracy, Support support)
        {
            Support = support;
            Accuracy = accuracy;
        }
    }
}
