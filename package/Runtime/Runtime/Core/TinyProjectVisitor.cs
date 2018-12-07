

using System;

namespace Unity.Tiny
{
    internal sealed partial class TinyProject
    {
        public abstract class Visitor
        {
            public TinyProject Project { get; set; }
            public TinyModule Module { get; set; }
            
            public virtual void BeginModule(TinyModule module) { }
            public virtual void EndModule(TinyModule module) { }
            public virtual void VisitType(TinyType type) { }
            public virtual void VisitEntityGroup(TinyEntityGroup entityGroup) { }
            public virtual void VisitEntity(TinyEntity entity) { }
            public virtual void VisitComponent(TinyObject component) { }
        }

        public void Visit(Visitor visitor)
        {
            visitor.Project = this;
            visitor.Module = Module.Dereference(Registry);
            
            foreach (var dependency in visitor.Module.EnumerateDependencies())
            {
                visitor.BeginModule(dependency);
                dependency.Visit(visitor);
                visitor.EndModule(dependency);
            }
        }
    }
}

