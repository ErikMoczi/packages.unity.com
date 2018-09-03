using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Properties
{
    public class EnumProperty<TContainer, TValue> : Property<TContainer, TValue>
        where TContainer : class, IPropertyContainer
        where TValue : struct, IComparable, IFormattable, IConvertible
    {
        public EnumProperty(string name, GetValueMethod getValue, SetValueMethod setValue) : base(name, getValue, setValue)
        {
        }

        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            
            var typedVisitor = visitor as IPropertyVisitor<TValue>;

            if (null != typedVisitor)
            {
                typedVisitor.Visit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
            }
            else
            {
                visitor.VisitEnum(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
            }
        }
    }
    
    public class StructEnumProperty<TContainer, TValue> : StructProperty<TContainer, TValue>
        where TContainer : struct, IPropertyContainer
        where TValue : struct, IComparable, IFormattable, IConvertible
    {
        public StructEnumProperty(string name, GetValueMethod getValue, SetValueMethod setValue) : base(name, getValue, setValue)
        {
        }

        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            
            var typedVisitor = visitor as IPropertyVisitor<TValue>;

            if (null != typedVisitor)
            {
                typedVisitor.Visit(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
            }
            else
            {
                visitor.VisitEnum(ref container, new VisitContext<TValue> {Property = this, Value = value, Index = -1});
            }
        }
    }
}