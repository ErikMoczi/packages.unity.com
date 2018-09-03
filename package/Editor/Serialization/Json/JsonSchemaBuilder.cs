#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public class JsonSchemaBuilder
    {
        public class ContainerBuilder
        {
            public Dictionary<string, object> Schema { get; set; } = new Dictionary<string, object>();

            public ContainerBuilder(string name, bool isStruct = false)
            {
                Schema[JsonSchema.Keys.ContainerNameKey] = $"{name}".Trim();
                Schema[JsonSchema.Keys.ContainerIsStructKey] = isStruct;
            }

            public string ToJson()
            {
                return Json.SerializeObject(Schema);
            }

            public ContainerBuilder WithProperty(
                string name,
                string type,
                string defaultValue = "",
                string ofType = "",
                string backingField = "",
                bool isReadonly = false,
                bool isPublic = false,
                bool isCustom = false,
                bool dontInitializeBackingField = false)
            {
                CreatePropertiesFieldIfNecessary();

                var properties = Schema[JsonSchema.Keys.PropertiesListKey] as IList<Dictionary<string, object>>;
                properties?.Add(new Dictionary<string, object>()
                    {
                        [JsonSchema.Keys.PropertyNameKey] = name,
                        [JsonSchema.Keys.PropertyTypeKey] = type,
                        [JsonSchema.Keys.PropertyDefaultValueKey] = defaultValue,
                        [JsonSchema.Keys.PropertyItemTypeKey] = ofType,
                        [JsonSchema.Keys.PropertyDelegateMemberToKey] = backingField,
                        [JsonSchema.Keys.IsReadonlyPropertyKey] = isReadonly,
                        [JsonSchema.Keys.PropertyIsPublicKey] = isPublic,
                        [JsonSchema.Keys.IsCustomPropertyKey] = isCustom,
                        [JsonSchema.Keys.DontInitializeBackingFieldKey] = dontInitializeBackingField
                    }
                );

                return this;
            }

            public ContainerBuilder WithBaseClassOverriden(string baseClassOverride)
            {
                Schema[JsonSchema.Keys.OverrideDefaultBaseClassKey] = baseClassOverride;
                return this;
            }

            public ContainerBuilder WithNamespace(string ns)
            {
                Schema[JsonSchema.Keys.NamespaceKey] = ns;
                return this;
            }

            public ContainerBuilder WithNoDefaultImplementation(bool noDefaultImplementation)
            {
                Schema[JsonSchema.Keys.NamespaceKey] = noDefaultImplementation;
                return this;
            }

            public ContainerBuilder WithIsAbstract(bool isAbstract)
            {
                Schema[JsonSchema.Keys.IsAbstractClassKey] = isAbstract;
                return this;
            }

            public ContainerBuilder WithNestedContainer(ContainerBuilder containerBuilder)
            {
                CreateNestedContainerListIfNecessary();

                var nestedContainers = Schema[JsonSchema.Keys.NestedTypesKey] as List<object>;
                nestedContainers?.Add(containerBuilder.Schema);

                return this;
            }

            public ContainerBuilder WithNoDefaultImplementation()
            {
                Schema[JsonSchema.Keys.NoDefaultImplementationKey] = true;
                return this;
            }

            public ContainerBuilder WithEmptyPropertiesList()
            {
                CreatePropertiesFieldIfNecessary();
                return this;
            }

            public ContainerBuilder WithUserHooks(List<string> userHooks)
            {
                if (userHooks != null && userHooks.Count > 0)
                {
                    Schema[JsonSchema.Keys.GeneratedUserHooksKey] = string.Join(",", userHooks);
                }
                return this;
            }

            private void CreatePropertiesFieldIfNecessary()
            {
                if (!Schema.ContainsKey(JsonSchema.Keys.PropertiesListKey))
                {
                    Schema[JsonSchema.Keys.PropertiesListKey] = new List<Dictionary<string, object>>();
                }
            }

            private void CreateNestedContainerListIfNecessary()
            {
                if (!Schema.ContainsKey(JsonSchema.Keys.NestedTypesKey))
                {
                    Schema[JsonSchema.Keys.NestedTypesKey] = new List<object>();
                }
            }
        }

        public Dictionary<string, object> Schema { get; set; } = new Dictionary<string, object>()
        {
            [JsonSchema.Keys.VersionKey] = JsonSchema.CurrentVersion
        };

        public string ToJson()
        {
            return Json.SerializeObject(Schema);
        }

        public JsonSchemaBuilder WithVersion(string version)
        {
            Schema[JsonSchema.Keys.VersionKey] = version;
            return this;
        }

        public JsonSchemaBuilder WithNamespace(string ns)
        {
            Schema[JsonSchema.Keys.NamespaceKey] = ns;
            return this;
        }

        public JsonSchemaBuilder WithUsing(List<string> usings)
        {
            Schema[JsonSchema.Keys.UsingAssembliesKey] = usings;
            return this;
        }

        public JsonSchemaBuilder WithEmptyContainerList()
        {
            if (!Schema.ContainsKey(JsonSchema.Keys.TypesKey))
            {
                Schema[JsonSchema.Keys.TypesKey] = new List<object>();
            }
            return this;
        }

        public JsonSchemaBuilder WithRequiredAssemblies(List<string> assemblyReferences)
        {
            Schema[JsonSchema.Keys.RequiredAssembliesKey] = assemblyReferences;
            return this;
        }

        public JsonSchemaBuilder WithContainer(ContainerBuilder containerBuilder)
        {
            if ( ! Schema.ContainsKey(JsonSchema.Keys.TypesKey))
            {
                Schema[JsonSchema.Keys.TypesKey] = new List<object>();
            }

            var types = Schema[JsonSchema.Keys.TypesKey] as List<object>;
            types?.Add(containerBuilder.Schema);

            return this;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)