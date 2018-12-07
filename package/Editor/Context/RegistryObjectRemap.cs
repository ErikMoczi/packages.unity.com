

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal static class RegistryObjectRemap
    {
        /// <summary>
        /// Remaps the tiny object ids for all of the given assets
        /// </summary>
        /// <param name="assetPaths">Asset paths</param>
        /// <param name="guids">Optional specific guids to remap</param>
        public static void Remap(IEnumerable<string> assetPaths, HashSet<TinyId> guids = null)
        {
            var registry = new TinyRegistry();

            // @HACK We need to cache this...
            // This is awful for performance, although this operation is rare
            Persistence.LoadAllModules(registry);
            
            const string identifier = "__remap__";

            foreach (var path in assetPaths)
            {
                using (var transaction = new Persistence.LoadTransaction())
                {
                    transaction.LoadJson(path, identifier, AssetDatabase.AssetPathToGUID(path));
                    transaction.Commit(registry);
                }
            }

            //
            // Step 1) Re-generate guids for top level objects with invalid ids
            //
            
            var remap = new Dictionary<TinyId, TinyId>();

            using (registry.SourceIdentifierScope(identifier))
            {
                // @TODO Fixme
                // Calling `Unregister` and `Register` with a new ID will make `FindAllBySource` behave incorrectly
                // This is an internal issue in the Registry
                
                // As a workaround we pull out all objects and unregister by source
                // We then re-add all objects to the registry using the correct ids
                var objects = registry.FindAllBySource(identifier).ToList();
                registry.UnregisterAllBySource(identifier);
                registry.ClearUnregisteredObjects();
                
                // For each registry object
                //    - Remap the ID; if needed
                //    - Re-register the object
                foreach (var o in objects)
                {
                    var obj = (TinyRegistryObjectBase) o;
                    var srcId = obj.Id;

                    if (null != guids && !guids.Contains(srcId))
                    {
                        // Re-register our object under the new id
                        registry.Register(obj);
                        continue;
                    }
                    
                    // Unregister the object
                    registry.Unregister(srcId);
                    
                    var dstId = TinyId.New();
                    obj.Id = dstId;
                    Assert.IsFalse(remap.ContainsKey(srcId));
                    remap.Add(srcId, dstId);
                        
                    // Re-register our object under the new id
                    registry.Register(obj);
                }
            }
            
            //
            // Step 2) Perform a fixup on any references to these top level objects
            //

            var visitor = new RemapVisitor(remap);
            
            foreach (var obj in registry.FindAllBySource(identifier))
            {
                var container = obj as IPropertyContainer;
                visitor.PushContainer(container);
                container.Visit(visitor);
                visitor.PopContainer();
            }
            
            //
            // Step 3) Re-write these assets to disc and re-import (asset importer will rebuild the database mapping)
            //
            
            var persistentObjects = registry.FindAllBySource(identifier).OfType<IPersistentObject>().ToList();
            
            // @TODO fixme
            // Workaround to handle a case where we are performing a remap and we need to split the files
            // We expect either (project AND module) OR (module)
            var project = registry.FindAllByType<TinyProject>().FirstOrDefault();
            var module = registry.FindAllByType<TinyModule>().FirstOrDefault();

            // If we have a project
            // The persistence system will bundle the module in the same file as a sub asset
            if (null != project)
            {
                if (project.LastSerializedVersion < TinyProject.CurrentSerializedVersion)
                {
                    project.LastSerializedVersion = TinyProject.CurrentSerializedVersion;
                }

                persistentObjects.Remove(module);
            }
            
            // Manually re-save the assets in the same way we read them
            // We don't want the persistent system to save it in a new format by accident
            foreach (var path in assetPaths)
            {
                // Re-build a list of all containers in this asset
                var containers = new List<IPropertyContainer>();

                // Find the all Tiny object that existed in this asset
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as TinyScriptableObject;
                
                Assert.IsNotNull(asset);

                // The asset still uses the old guids since it has not been reimported yet
                foreach (var obj in asset.Objects)
                {
                    var srcId = new TinyId(obj);
                    TinyId dstId;
                    if (!remap.TryGetValue(srcId, out dstId))
                    {
                        // Id has not been remapped, use the original
                        dstId = srcId;
                    }

                    containers.Add(registry.FindById(dstId) as IPropertyContainer);
                }

                // Write the containers to disc
                Persistence.PersistContainersAs(containers, path);
                
                // Re-import this asset to have the importer update the database
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUncompressedImport);
            }
        }

        private class RemapVisitor : PropertyVisitor,
            ICustomVisit<TinyProject.Reference>,
            ICustomVisit<TinyModule.Reference>,
            ICustomVisit<TinyType.Reference>,
            ICustomVisit<TinyEntity.Reference>,
            ICustomVisit<TinyEntityGroup.Reference>
        {
            private readonly Stack<IPropertyContainer> m_Containers = new Stack<IPropertyContainer>();
            private readonly IDictionary<TinyId, TinyId> m_Remap;
            
            public RemapVisitor(IDictionary<TinyId, TinyId> remap)
            {
                m_Remap = remap;
            }

            public void PushContainer(IPropertyContainer container)
            {
                m_Containers.Push(container);
            }

            public void PopContainer()
            {
                m_Containers.Pop();
            }
            
            protected override void Visit<TValue>(TValue value)
            {
                
            }

            public override bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                PushContainer(context.Value);
                return base.BeginContainer(container, context);
            }

            public override void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                PopContainer();
                base.EndContainer(container, context);
            }
            
            public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            {
                PushContainer(context.Value);
                return base.BeginContainer(ref container, context);
            }

            public override void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            {
                PopContainer();
                base.EndContainer(ref container, context);
            }

            public void CustomVisit(TinyProject.Reference value)
            {
                SetReferenceValue(value, (i, s) => new TinyProject.Reference(i, s));
            }
            
            public void CustomVisit(TinyModule.Reference value)
            {
                SetReferenceValue(value, (i, s) => new TinyModule.Reference(i, s));
            }

            public void CustomVisit(TinyType.Reference value)
            {
                SetReferenceValue(value, (i, s) => new TinyType.Reference(i, s));
            }

            public void CustomVisit(TinyEntity.Reference value)
            {
                SetReferenceValue(value, (i, s) => new TinyEntity.Reference(i, s));
            }

            public void CustomVisit(TinyEntityGroup.Reference value)
            {
                SetReferenceValue(value, (i, s) => new TinyEntityGroup.Reference(i, s));
            }
            
            private void SetReferenceValue<TValue>(IReference reference, Func<TinyId, string, TValue> ctor)
            {
                if (!m_Remap.ContainsKey(reference.Id))
                {
                    return;
                }

                var container = m_Containers.Peek();
                
                var value = ctor(m_Remap[reference.Id], reference.Name);
                
                if (IsListItem)
                {
                    (Property as IListClassProperty)?.SetObjectAt(container, ListIndex, value);
                    (Property as IListStructProperty)?.SetObjectAt(ref container, ListIndex, value);
                }
                else
                {
                    (Property as IValueClassProperty)?.SetObjectValue(container, value);
                    (Property as IValueStructProperty)?.SetObjectValue(ref container, value);
                }

                if (Property is IStructProperty)
                {
                    m_Containers.Pop();
                    m_Containers.Push(container);
                }
            }
        }
    }
}

