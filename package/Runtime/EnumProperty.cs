#if (NET_4_6 || NET_STANDARD_2_0)

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
            var context = new VisitContext<TValue> { Property = this, Value = GetValue(container), Index = -1 };
            if (false == visitor.ExcludeVisit(container, context))
            {
                visitor.VisitEnum(container, context);
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
            var context = new VisitContext<TValue> { Property = this, Value = GetValue(ref container), Index = -1 };
            if (false == visitor.ExcludeVisit(ref container, context))
            {
                visitor.VisitEnum(ref container, context);
            }
        }
    }
    
    public class EnumListProperty<TContainer, TValue, TItem> : ListProperty<TContainer, TValue, TItem>
        where TContainer : class, IPropertyContainer
        where TValue : class, IList<TItem>
        where TItem : struct, IComparable, IFormattable, IConvertible
    {
        public EnumListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstance = null) : base(name, getValue, setValue, createInstance)
        {
            Assert.IsTrue(typeof(TItem).IsEnum);
        }
        
        public override void Accept(TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(container);
            
            if (false == visitor.ExcludeVisit(container,
                new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                var listContext =
                    new VisitContext<TValue> { Property = this, Value = value, Index = -1 };

                if (visitor.BeginList(container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetItemAt(container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(container, itemVisitContext))
                        {
                            visitor.VisitEnum(container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(container, listContext);
            }
        }
    }
    
    public class StructEnumListProperty<TContainer, TValue, TItem> : StructListProperty<TContainer, TValue, TItem>
        where TContainer : struct, IPropertyContainer
        where TValue : class, IList<TItem>
        where TItem : struct, IComparable, IFormattable, IConvertible
    {
        public StructEnumListProperty(string name, GetValueMethod getValue, SetValueMethod setValue, CreateInstanceMethod createInstance = null) : base(name, getValue, setValue, createInstance)
        {
            Assert.IsTrue(typeof(TItem).IsEnum);
        }
        
        public override void Accept(ref TContainer container, IPropertyVisitor visitor)
        {
            var value = GetValue(ref container);
            
            if (false == visitor.ExcludeVisit(ref container,
                new VisitContext<TValue> {Property = this, Value = value, Index = -1}))
            {
                var listContext =
                    new VisitContext<TValue> { Property = this, Value = value, Index = -1 };

                if (visitor.BeginList(ref container, listContext))
                {
                    var itemVisitContext = new VisitContext<TItem>
                    {
                        Property = this
                    };

                    var count = Count(ref container);
                    for (var i = 0; i < count; i++)
                    {
                        var item = GetItemAt(ref container, i);
                        itemVisitContext.Value = item;
                        itemVisitContext.Index = i;

                        if (false == visitor.ExcludeVisit(ref container, itemVisitContext))
                        {
                            visitor.VisitEnum(ref container, itemVisitContext);
                        }
                    }
                }
                visitor.EndList(ref container, listContext);
            }
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
