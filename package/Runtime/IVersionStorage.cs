namespace Unity.Properties
{
    public interface IVersionStorage
    {
        int GetVersion<TContainer>(IProperty property, ref TContainer container)
            where TContainer : IPropertyContainer;
        
        void IncrementVersion<TContainer>(IProperty property, ref TContainer container) 
            where TContainer : IPropertyContainer;
    }
    
    public class PassthroughVersionStorage : IVersionStorage
    {
        public static PassthroughVersionStorage Instance { get; } = new PassthroughVersionStorage();

        private PassthroughVersionStorage()
        {
            
        }
        
        public int GetVersion<TContainer>(IProperty property, ref TContainer container)
            where TContainer : IPropertyContainer
        {
            return 0;
        }

        public void IncrementVersion<TContainer>(IProperty property, ref TContainer container) 
            where TContainer : IPropertyContainer
        {
            
        }
    }
}
