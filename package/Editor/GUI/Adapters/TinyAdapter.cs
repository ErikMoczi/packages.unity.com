
namespace Unity.Tiny
{
    internal class TinyAdapter : ITinyAdapter
    {
        protected TinyAdapter(TinyContext tinyContext)
        {
            TinyContext = tinyContext;
        }

        public TinyContext TinyContext { get; }
    }
}
