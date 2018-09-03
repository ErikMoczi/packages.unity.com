namespace Unity.Properties
{
    public interface IPropertyContainer
    {
        IVersionStorage VersionStorage { get; }
        IPropertyBag PropertyBag { get; }
    }
}
