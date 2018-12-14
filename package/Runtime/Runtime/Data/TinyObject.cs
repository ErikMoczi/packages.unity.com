

using System.Collections.Generic;
using Unity.Properties;
using Unity.Tiny.Attributes;
using Unity.Tiny.Serialization.Json;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Tiny.InspectorAttributes;

namespace Unity.Tiny
{
    /// <summary>
    /// Represents dynamic object instance of a TinyType
    /// </summary>
    internal sealed partial class TinyObject : IPropertyContainer, IVersionStorage, IVersioned, IAttributable
    {
        private static readonly StructValueClassProperty<TinyObject, TinyType.Reference> s_TypeProperty =
            new StructValueClassProperty<TinyObject, TinyType.Reference>("Type",
                /* GET */ c => c.m_Type,
                /* SET */ (c, v) =>
                {
                    c.m_Type = v;
                    c.m_CurrentTypeVersion = -1;
                },
                /* REF */ (m, p, c, v) => m(p, c, ref c.m_Type, v)
            ).WithAttribute(InspectorAttributes.HideInInspector);

        private static readonly ClassValueClassProperty<TinyObject, PropertiesContainer> s_PropertiesProperty =
            new ClassValueClassProperty<TinyObject, PropertiesContainer>("Properties",
                /* GET */ c => c.m_Properties,
                /* SET */ null
            );
        
        private static readonly ClassPropertyBag<TinyObject> s_PropertyBag = new ClassPropertyBag<TinyObject>(
            s_TypeProperty, 
            s_PropertiesProperty);

        private readonly IVersionStorage m_SharedVersionStorage;
        private int m_CurrentTypeVersion;
        private int m_DefaultValueVersion;
        private TinyType.Reference m_Type;
        private readonly PropertiesContainer m_Properties;

        public IRegistry Registry { get; }
        
        public TinyType.Reference Type
        {
            get { return s_TypeProperty.GetValue(this); }
            set { s_TypeProperty.SetValue(this, value); }
        }

        public string Name { get; set; }

        public PropertiesContainer Properties => s_PropertiesProperty.GetValue(this);

        /// <summary>
        /// Is this object a default value? 
        /// NOTE: This is only true on top level/root objects
        /// </summary>
        public bool IsDefaultValue { get; internal set; }

        /// <summary>
        /// Does this object have any overridden values
        /// </summary>
        public bool IsOverridden => m_Properties.IsOverridden;
        
        /// <summary>
        /// Version for this object, incremented when any field changes
        /// </summary>
        public int Version { get; private set; }
        
        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => this;

        public TinyObject(IRegistry registry, TinyType.Reference type, IVersionStorage versionStorage = null, bool refresh = true)
        {
            Registry = registry;
            m_SharedVersionStorage = versionStorage;
            m_Properties = new PropertiesContainer(this);
            m_Type = type;

            if (refresh)
            {
                Refresh();
            }
        }

        public object this[string key]
        {
            get
            {
                return m_Properties[key];
            }
            set
            {
                m_Properties[key] = value;
            }
        }

        public bool HasProperty(string key)
        {
            return m_Properties.HasProperty(key);
        }

        public void RemoveProperty(string key)
        {
            m_Properties.RemoveProperty(key);
        }
        
        public IEnumerable<KeyValuePair<string, object>> EnumerateProperties()
        {
            var container = (IPropertyContainer) m_Properties;
            foreach (var property in m_Properties.PropertyBag.Properties)
            {
                yield return new KeyValuePair<string, object>(property.Name, (property as IValueProperty)?.GetObjectValue(container));
            }
        }

        public void IncrementVersion<TContainer>(IProperty property, TContainer container)
            where TContainer : IPropertyContainer
        {
            Version++;
            
            // Is this one of our sub properties
            if (!ReferenceEquals(container, this))
            {
                // One of our properties or sub properties has been updated
                m_SharedVersionStorage?.IncrementVersion(s_PropertiesProperty, this);
            }
            else
            {
                m_SharedVersionStorage?.IncrementVersion(property, container);
            }
        }

        /// <summary>
        /// Updates the value tree based on its internal type and migrates any values
        ///
        /// @TODO This method does WAY to much
        ///     - We migrate data
        ///     - Ensure types are up to date
        ///     - Rebuild properties
        /// </summary>
        public void Refresh(TinyObject defaultValue = null, bool skipTypeCheck = false)
        {
            var type = Type.Dereference(Registry);

            if (null == type)
            {
                // m_Properties.Clear();
                return;
            }
            
            if (!IsDefaultValue)
            {
                // Force the type to be refreshed
                if (!skipTypeCheck)
                {
                    type.Refresh();
                }

                if (defaultValue == null)
                {
                    defaultValue = type.GetDefaultValue() as TinyObject;
                }
            }

            var defaultValueVersion = defaultValue?.Version ?? -1;
            
            if (m_CurrentTypeVersion == type.Version && m_DefaultValueVersion == defaultValueVersion)
            {
                return;
            }
            
            // Fix up the ref name
            m_Type = (TinyType.Reference) type;
            
            m_Properties.Refresh(type, defaultValue, skipTypeCheck);
            m_CurrentTypeVersion = type.Version;
            m_DefaultValueVersion = defaultValueVersion;
            
            if (type.TypeCode == TinyTypeCode.Component)
            {
                Name = type.Name;
            }
        }

        /// <summary>
        /// Resets all values to their initial/default state
        /// </summary>
        public void Reset(TinyObject defaultValue = null)
        {
            Refresh();
            var type = Type.Dereference(Registry);
            m_Properties.Reset(type, defaultValue);
        }
        
        /// <summary>
        /// Copies the properties from the given TinyObject to this object
        ///
        /// !!! IMPORTANT !!! Property `override` flags are copied from the source object and do NOT respect the defaults of this object
        /// </summary>
        /// <param name="other"></param>
        public void CopyFrom(TinyObject other)
        {
            Type = other.Type;
            foreach (var property in other.Properties.PropertyBag.Properties)
            {
                var vc = (IPropertyContainer) other.Properties;
                this[property.Name] = (property as IValueProperty)?.GetObjectValue(vc);
                Properties.SetOverridden(property.Name, (property as ITinyValueProperty)?.IsOverridden(vc) ?? true);
            }
        }
    }
}

