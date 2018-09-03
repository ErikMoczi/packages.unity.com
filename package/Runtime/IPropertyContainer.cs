#if (NET_4_6 || NET_STANDARD_2_0)

namespace Unity.Properties
{
    public interface IPropertyContainer
    {
        IVersionStorage VersionStorage { get; }
        IPropertyBag PropertyBag { get; }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
