namespace Unity.Properties
{
    public interface IPropertyContainer
    {
        IVersionStorage VersionStorage { get; }
        IPropertyBag PropertyBag { get; }
    }

    public static class PropertyContainer
    {
        public static void Visit<TContainer>(this TContainer container, IPropertyVisitor visitor)
            where TContainer : class, IPropertyContainer
        {
            container.PropertyBag.Visit(container, visitor);
        }

        public static void Visit<TContainer>(ref TContainer container, IPropertyVisitor visitor)
            where TContainer : struct, IPropertyContainer
        {
            container.PropertyBag.Visit(ref container, visitor);
        }
    }
}
