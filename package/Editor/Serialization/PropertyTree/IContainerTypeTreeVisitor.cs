#if (NET_4_6 || NET_STANDARD_2_0)

namespace Unity.Properties.Editor.Serialization
{
    public interface IContainerTypeTreeVisitor
    {
        void VisitNestedContainer(ContainerTypeTreePath path, PropertyTypeNode container);

        void VisitContainer(ContainerTypeTreePath path, PropertyTypeNode container);
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
