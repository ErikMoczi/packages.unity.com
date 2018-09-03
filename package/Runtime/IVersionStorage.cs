#if NET_4_6
namespace Unity.Properties
{
    public interface IVersionStorage
    {
        int GetVersion(IProperty property, IPropertyContainer container);
        void IncrementVersion(IProperty property, IPropertyContainer container);
    }
    
    public sealed class PassthroughVersionStorage : IVersionStorage
    {
        public static PassthroughVersionStorage Instance { get; } = new PassthroughVersionStorage();

        private PassthroughVersionStorage()
        {
            
        }
        
        public int GetVersion(IProperty property, IPropertyContainer container)
        {
            return 0;
        }

        public void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            
        }
    }
}
#endif // NET_4_6
