
using System.Linq;

namespace Unity.Tiny
{
    internal interface INameResolver
    {
        string Resolve<TValue>(ref UIVisitContext<TValue> context);
    }

    internal class DefaultNameResolver : INameResolver
    {
        public string Resolve<TValue>(ref UIVisitContext<TValue> context)
        {
            return context.IsListItem ? context.Index.ToString() : context.Property.Name;
        }
    }

    internal class DefaultValueNameResolver : INameResolver
    {
        private static DefaultNameResolver Default = new DefaultNameResolver();

        public string Resolve<TValue>(ref UIVisitContext<TValue> context)
        {
            var target = context.Targets.OfType<TinyObject>().FirstOrDefault();
            return context.Property == target.Properties.PropertyBag.FindProperty(context.Property.Name) ? "Default Value" : Default.Resolve(ref context);
        }
    }
}
