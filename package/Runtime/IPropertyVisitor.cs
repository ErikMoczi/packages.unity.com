namespace Unity.Properties
{
    public struct VisitContext<TValue>
    {
        public IProperty Property;
        public TValue Value;
        public int Index;
    }
    
    public struct SubtreeContext<TValue>
    {
        public IProperty Property;
        public TValue Value;
        public int Index;
    }
    
    public struct ListContext<TValue>
    {
        public IListProperty Property;
        public TValue Value;
        public int Index;
        public int Count;
    }
    
    public interface IBuiltInPropertyVisitor : 
        IPropertyVisitor<bool>,
        IPropertyVisitor<char>,
        IPropertyVisitor<sbyte>,
        IPropertyVisitor<byte>,
        IPropertyVisitor<short>,
        IPropertyVisitor<int>,
        IPropertyVisitor<long>,
        IPropertyVisitor<ushort>,
        IPropertyVisitor<uint>,
        IPropertyVisitor<ulong>,
        IPropertyVisitor<float>,
        IPropertyVisitor<double>,
        IPropertyVisitor<string>
    {
    }

    public interface IPropertyVisitor<TValue> : IPropertyVisitor
    {
        void Visit<TContainer>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer;
    }
    
    public interface IPropertyVisitorValidation
    {
        bool ShouldVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer;
    }
    
    public interface IPropertyVisitorValidation<TValue>
    {
        bool ShouldVisit<TContainer>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer;
    }
    
    public interface IPropertyVisitor
    {
        void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer;
        void VisitEnum<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : struct;
        bool BeginContainer<TContainer, TValue>(ref TContainer container, SubtreeContext<TValue> context) where TContainer : IPropertyContainer;
        void EndContainer<TContainer, TValue>(ref TContainer container, SubtreeContext<TValue> context) where TContainer : IPropertyContainer;
        bool BeginList<TContainer, TValue>(ref TContainer container, ListContext<TValue> context) where TContainer : IPropertyContainer;
        void EndList<TContainer, TValue>(ref TContainer container, ListContext<TValue> context) where TContainer : IPropertyContainer;
    }
}
