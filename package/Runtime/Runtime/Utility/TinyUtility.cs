

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal static class TinyUtility
    {
        public static bool ContainName<T>(IEnumerable<T> elements, string name) where T : INamed
        {
            return elements.Any(n => string.Compare(n.Name, name, StringComparison.Ordinal) == 0);
        }

        public static string GetUniqueName<T>(IEnumerable<T> elements, string name) where T : INamed
        {
            var current = name;
            var index = 1;

            while (true)
            {
                if (elements.All(element => !string.Equals(element.Name, current)))
                {
                    return current;
                }

                current = $"{name}{index++}";
            }
        }
        
        public static string GetUniqueName(IEnumerable<TinyField> elements, string name)
        {
            var current = name;
            var index = 1;

            while (true)
            {
                if (elements.All(element => !string.Equals(element.Name, current)))
                {
                    return current;
                }

                current = $"{name}{index++}";
            }
        }
        
        public static bool IsValidObjectName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // The typeName MUST start with a letter or underscore
            if (!(name[0] == '_' || char.IsLetter(name[0])))
            {
                return false;
            }

            // The typeName may contain letters/numbers or underscores
            for (var i = 1; i < name.Length; i++)
            {
                if (!(name[i] == '_' || char.IsLetterOrDigit(name[i])))
                {
                    return false;
                }
            }

            return true;
        }
        
        public static bool IsValidNamespaceName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // The typeName MUST start with a letter or underscore
            if (!(name[0] == '_' || char.IsLetter(name[0])))
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                if (!(name[i] == '_' || char.IsLetterOrDigit(name[i]) || name[i] == '.'))
                {
                    return false;
                }
            }

            return true;
        }
        
        public static TinyAssetExportSettings GetAssetExportSettings(TinyProject project, Object asset)
        {
            Assert.IsNotNull(asset);
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);
            return module.GetAsset(asset)?.ExportSettings ?? project.Settings.GetDefaultAssetExportSettings(asset.GetType());
        }
        
        public static IEnumerable<TinyModule> GetModules(TinyRegistryObjectBase @object)
        {
            if (@object is TinyType)
            {
                return GetModules(@object.Registry, (TinyType.Reference) (TinyType) @object);
            }
            
            if (@object is TinyEntityGroup)
            {
                return GetModules(@object.Registry, (TinyEntityGroup.Reference) (TinyEntityGroup) @object);
            }
            
            return Enumerable.Empty<TinyModule>();
        }

        public static IEnumerable<TinyModule> GetModules(IRegistry registry, IReference reference)
        {
            if (reference is TinyType.Reference)
            {
                return GetModules(registry, (TinyType.Reference) reference);
            }
            
            if (reference is TinyEntityGroup.Reference)
            {
                return GetModules(registry, (TinyEntityGroup.Reference) reference);
            }

            return Enumerable.Empty<TinyModule>();
        }
        
        public static IEnumerable<TinyModule> GetModules(IRegistry registry, TinyType.Reference reference)
        {
            var modules = registry.FindAllByType<TinyModule>();
            return modules.Where(module => module.Types.Contains(reference));
        }
        
        public static IEnumerable<TinyModule> GetModules(IRegistry registry, TinyEntityGroup.Reference reference)
        {
            var modules = registry.FindAllByType<TinyModule>();
            return modules.Where(module => module.EntityGroups.Contains(reference));
        }
    }
}

