

using System;
using System.Linq;
using Unity.Properties;
using UnityEngine.Assertions;
using static Unity.Tiny.InspectorAttributes;

namespace Unity.Tiny
{
    internal static class TinyEnum
    {
        public struct Reference : IStructPropertyContainer<Reference>, IEquatable<Reference>, IReference
        {
            private static readonly ValueStructProperty<Reference, TinyTypeId> s_TypeIdProperty = new ValueStructProperty<Reference, TinyTypeId>("$TypeId",
                (ref Reference c) => TinyTypeId.EnumReference,
                null
            ).WithAttribute(HideInInspector)
             .WithAttribute(Readonly); 

            private static readonly ValueStructProperty<Reference, TinyType.Reference> s_TypeProperty = new ValueStructProperty<Reference, TinyType.Reference>("Type",
                (ref Reference c) => c.m_Type,
                (ref Reference c, TinyType.Reference v) => c.m_Type = v
            );

            private static readonly ValueStructProperty<Reference, TinyId> s_IdProperty = new ValueStructProperty<Reference, TinyId>("Id",
                (ref Reference c) => c.m_Id,
                (ref Reference c, TinyId v) => c.m_Id = v
            );
            
            private static readonly ValueStructProperty<Reference, string> s_NameProperty = new ValueStructProperty<Reference, string>("Name",
                (ref Reference c) => c.m_Name,
                (ref Reference c, string v) => c.m_Name = v
            );

            private static readonly ValueStructProperty<Reference, int> s_ValueProperty = new ValueStructProperty<Reference, int>("Value",
                (ref Reference c) => c.m_Value,
                (ref Reference c, int v) => c.m_Value = v
            );

            private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
                s_TypeIdProperty, 
                s_TypeProperty, 
                s_IdProperty, 
                s_NameProperty, 
                s_ValueProperty);
            
            public static Reference None { get; } = new Reference();

            private TinyType.Reference m_Type;
            private TinyId m_Id;
            private string m_Name;
            private int m_Value;

            public TinyType.Reference Type => s_TypeProperty.GetValue(ref this);
            
            public TinyId Id => s_IdProperty.GetValue(ref this);
            public string Name => s_NameProperty.GetValue(ref this);
            public int Value => s_ValueProperty.GetValue(ref this);

            public IPropertyBag PropertyBag => s_PropertyBag;
            public IVersionStorage VersionStorage => null;

            public Reference(TinyType.Reference type, TinyId id, string name, int value)
            {
                m_Type = type;
                m_Id = id;
                m_Name = name;
                m_Value = value;
            }

            public Reference(TinyType type, int value)
            {
                Assert.IsNotNull(type);
                Assert.IsTrue(type.IsEnum);

                var defaultValue = type.DefaultValue as TinyObject;

                TinyField field;
                if (null != defaultValue)
                {
                    var name = string.Empty;
                    var container = (IPropertyContainer) defaultValue.Properties;
                    foreach (var property in defaultValue.Properties.PropertyBag.Properties)
                    {
                        var propertyValue = (property as IValueProperty)?.GetObjectValue(container);
                        if (!value.Equals(propertyValue))
                        {
                            continue;
                        }
                        name = property.Name;
                        break;
                    }
                    field = type.FindFieldByName(name);
                }
                else
                {
                    field = type.Fields.FirstOrDefault();
                }

                m_Type = (TinyType.Reference) type;
                m_Id = field?.Id ?? TinyId.Empty;
                m_Name = field?.Name ?? string.Empty;
                m_Value = null != field ? (int?) defaultValue?[m_Name] ?? 0 : 0;
            }

            public Reference(TinyType type, string name)
            {
                Assert.IsNotNull(type);
                Assert.IsTrue(type.IsEnum);

                var defaultValue = type.DefaultValue as TinyObject;
                var field = type.FindFieldByName(name);

                m_Type = (TinyType.Reference) type;
                m_Id = field?.Id ?? TinyId.Empty;
                m_Name = field?.Name ?? string.Empty;
                m_Value = null != field ? (int?) defaultValue?[m_Name] ?? 0 : 0;
            }
            
            public Reference(TinyType type, TinyId fieldId)
            {
                Assert.IsNotNull(type);
                Assert.IsTrue(type.IsEnum);

                var defaultValue = type.DefaultValue as TinyObject;
                var field = type.FindFieldById(fieldId);

                m_Type = (TinyType.Reference) type;
                m_Id = field?.Id ?? TinyId.Empty;
                m_Name = field?.Name ?? string.Empty;
                m_Value = null != field ? (int?) defaultValue?[m_Name] ?? 0 : 0;
            }
            
            public bool Equals(Reference other)
            {
                return m_Type.Equals(other.m_Type) && m_Id.Equals(other.m_Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Reference && Equals((Reference) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (m_Type.GetHashCode() * 397) ^ m_Id.GetHashCode();
                }
            }

            public void MakeRef<TContext>(ByRef<Reference, TContext> method, TContext context)
            {
                method(ref this, context);
            }

            public TReturn MakeRef<TContext, TReturn>(ByRef<Reference, TContext, TReturn> method, TContext context)
            {
                return method(ref this, context);
            }
        }
    }
}

