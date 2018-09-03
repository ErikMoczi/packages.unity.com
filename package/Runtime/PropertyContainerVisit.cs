#if (NET_4_6 || NET_STANDARD_2_0)

namespace Unity.Properties
{
    public static partial class PropertyContainer
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

#endif // (NET_4_6 || NET_STANDARD_2_0)