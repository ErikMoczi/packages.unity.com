#if (NET_4_6 || NET_STANDARD_2_0)

using System.IO;
using Unity.Properties.Codegen.CSharp;
using Unity.Properties.Serialization;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Properties.Codegen
{
    [ScriptedImporter(1, new[] {".properties"})]
    public class SchemaImporter : ScriptedImporter
    {
        private static int CurrentVersion { get; } = 1;
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Generate a dummy object to satisfy the asset pipeline
            var asset = ScriptableObject.CreateInstance<SchemaObject>();
            ctx.AddObjectToAsset("asset", asset);

            // Read the contents of the file
            var json = File.ReadAllText(ctx.assetPath);

            // Deserialize to a generic object tree
            var obj = JsonSerializer.Deserialize(json);

            // Perform any migration on the schema object
            var migration = MigrateSchema(obj);

            // Unpack the fully migrated object to the current schema version
            var schema = new Schema();
            PropertyContainer.Transfer(migration, schema);
            
            // Construct the destination path
            var directory = Path.GetDirectoryName(ctx.assetPath);
            var fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);
            var path = Path.Combine(directory, $"{fileName}.Properties.cs");
            
            // Generate the code and write to the file
            var builder = new CSharpSchemaBuilder();
            builder.Build(path, schema);
            
            asset.JsonSchema = JsonSerializer.Serialize(schema);
            
            // Asset importer expects ONE and ONLY ONE call to `SetMainObject`
            ctx.SetMainObject(asset);
            
            // Re-import the file to trigger re-compilation
            AssetDatabase.ImportAsset(path);
        }

        private static IPropertyContainer MigrateSchema(IPropertyContainer schema)
        {
            var migration = new MigrationContainer(schema);
            
            // Unpack version information
            // var version = migration.GetValueOrDefault<int>(Schema.Property.Version.Name);
            
            // <migration>
            
            // </migration>

            migration.SetValue(Schema.Property.Version.Name, CurrentVersion);
            return migration;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)