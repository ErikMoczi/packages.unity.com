
namespace Unity.Tiny
{
    internal class ContextManagerAttribute : TinyAttribute
    {
        public readonly ContextUsage Usage;

        public ContextManagerAttribute(ContextUsage usage, int order = 0)
            : base(order)
        {
            Usage = usage;
        }
    }
}
