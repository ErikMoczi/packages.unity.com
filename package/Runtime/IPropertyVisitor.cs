using System;
using UnityEngine;

namespace Unity.Properties
{
    public struct VisitContext<TValue>
    {
        public IProperty Property;
        public TValue Value;
        public int Index;
    }

    public interface IPropertyVisitor
    {
        bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
        
        bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
        
        void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer;
        
        void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer;
        
        void VisitEnum<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
            where TValue : struct;
        
        void VisitEnum<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
            where TValue : struct;

        bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : IPropertyContainer;

        void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : IPropertyContainer;

        bool BeginList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer;

        void EndList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer;
    }
}
