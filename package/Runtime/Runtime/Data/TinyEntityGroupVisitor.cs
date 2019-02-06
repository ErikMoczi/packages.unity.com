

namespace Unity.Tiny
{
    internal sealed partial class TinyEntityGroup
    {
        public void Visit(TinyProject.Visitor visitor)
        {
            visitor.VisitEntityGroup(this);
			
            foreach (var reference in Entities)
            {
                var entity = reference.Dereference(Registry);
				
                visitor.VisitEntity(entity);
                foreach (var component in entity.Components)
                {
                    visitor.VisitComponent(component);
                }
            }
        }
    }
}

