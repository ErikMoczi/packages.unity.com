

using Unity.Properties;

namespace Unity.Tiny
{
    /// <inheritdoc cref="IPropertyContainer" />
    internal sealed partial class TinyField : IPropertyContainer, IIdentified<TinyId>, INamed, IDocumented
    {
        static partial void InitializeCustomProperties()
        {
            IdProperty.WithAttribute(InspectorAttributes.HideInInspector);
            NameProperty.WithAttribute(InspectorAttributes.HideInInspector);
            
            DocumentationProperty = new ClassValueClassProperty<TinyField, TinyDocumentation>("Documentation",
                /* GET */ c => c.m_Documentation ?? (c.m_Documentation = new TinyDocumentation(c.VersionStorage)),
                /* SET */ null
            );
        }
        
        private int m_FieldTypeVersion;
       
        public TinyType DeclaringType { get; internal set; }
        public IVersionStorage VersionStorage { get; }
        
        public TinyField(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
        }

        public void Refresh(IRegistry registry)
        {
            var fieldtype = m_FieldType.Dereference(registry);

            if (fieldtype != null)
            {
                fieldtype.Refresh();

                if (fieldtype.Version == m_FieldTypeVersion)
                {
                    return;
                }

                // Fix up the reference
                m_FieldType = (TinyType.Reference) fieldtype;

                VersionStorage.IncrementVersion(null, this);
                m_FieldTypeVersion = fieldtype.Version;
            }
            else
            {
                if (m_FieldTypeVersion == -1)
                {
                    return;
                }
                
                VersionStorage.IncrementVersion(null, this);
                m_FieldTypeVersion = -1;
            }
        }
    }
}

