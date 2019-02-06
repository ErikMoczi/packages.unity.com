

using Unity.Properties;
using Unity.Properties.Serialization;
using Unity.Tiny.Serialization.Json;

namespace Unity.Tiny
{
    internal partial class TinyEditorWorkspace : IVersionStorage, IVersioned
    {
        public int Version { get; private set; }

        public void IncrementVersion<TContainer>(IProperty property, TContainer container) where TContainer : IPropertyContainer
        {
            Version++;
        }
        
        public void AddOpenedEntityGroup(TinyEntityGroup.Reference entityGroup)
        {
            if (!OpenedEntityGroupsProperty.Contains(this, entityGroup))
            {
                OpenedEntityGroupsProperty.Add(this, entityGroup);
            }
        }

        public void ClearOpenedEntityGroups()
        {
            OpenedEntityGroupsProperty.Clear(this);
        }

        public string ToJson()
        {
            return JsonBackEnd.Persist(this);
        }

        public void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var obj = JsonSerializer.Deserialize(json);
            var migration = new MigrationContainer(obj);
            
            // <MIGRATION>
            // Add any needed migration code here
            // </MIGRATION>
            
            PropertyContainer.Transfer(migration, this);
        }
    }
}

