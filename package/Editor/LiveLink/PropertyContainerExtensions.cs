using Unity.Properties;

namespace Unity.Tiny
{
    internal static class PropertyContainerExtensions
    {
        public static PropertyList<TContainer, TValue> GetList<TContainer, TValue>(this TContainer container, string name)
            where TContainer : class, IPropertyContainer
        {
            var property = (container.PropertyBag.FindProperty(name) as IListClassProperty<TContainer, TValue>);
            return new PropertyList<TContainer, TValue>(property, container);
        }
    }
}