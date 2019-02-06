

namespace Unity.Tiny
{
    internal sealed partial class TinyModule
    {
        public void Visit(TinyProject.Visitor visitor)
        {
            foreach (var reference in Components)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitType(obj);
                }
            }
            
            foreach (var reference in Structs)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitType(obj);
                }
            }
            
            foreach (var reference in Enums)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitType(obj);
                }
            }
            
            foreach (var reference in Configurations)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitType(obj);
                }
            }
            
            foreach (var reference in EntityGroups)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj)
                {
                    obj.Visit(visitor);
                }
            }
        }
    }
}

