using System;
using System.Linq;

namespace Unity.Tiny
{
    internal static class TinyModuleExtensions
    {
        private static TinyType CreateType(this TinyModule module, TinyId id, string name, TinyTypeCode typeCode)
        {
            var registry = module.Registry;
            var type = registry.CreateType(id, name, typeCode);
            switch (typeCode)
            {
                    case TinyTypeCode.Component:
                        module.AddComponentReference(type.Ref);
                        break;
                    case TinyTypeCode.Enum:
                        module.AddEnumReference(type.Ref);
                        break;
                    case TinyTypeCode.Struct:
                        module.AddStructReference(type.Ref);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, $"{TinyConstants.ApplicationName}: Only components|structs|enums can be created on a module.");
            }

            return type;
        }

        public static TinyType CreateComponent(this TinyModule module, TinyId id, string name)
        {
            return module.CreateType(id, name, TinyTypeCode.Component);
        }
        
        public static TinyType CreateStruct(this TinyModule module, TinyId id, string name)
        {
            return module.CreateType(id, name, TinyTypeCode.Struct);
        }
        
        public static TinyType CreateEnum(this TinyModule module, TinyId id, string name)
        {
            return module.CreateType(id, name, TinyTypeCode.Enum);
        }

        public static void AddExplicitModuleDependencies(this TinyModule module, params string[] moduleNames)
        {
            var registry = module.Registry;
            module.AddExplicitModuleDependencies(moduleNames.Select(name => registry.FindByName<TinyModule>(name)).NotNull().Ref().ToArray());
        }
        
        public static void AddExplicitModuleDependencies(this TinyModule module, params TinyModule.Reference[] modules)
        {
            foreach (var dependency in modules)
            {
                module.AddExplicitModuleDependency(dependency);
            }
        }
    }

    namespace Internal
    {
        internal static class TinyModuleExtensions
        {
            internal static TinyType CreateStruct(this TinyModule module, string name)
            {
                return module.CreateStruct(TinyId.Generate(module.Name + "." + name), name);
            }
            
            internal static TinyType CreateEnum(this TinyModule module, string name)
            {
                return module.CreateEnum(TinyId.Generate(module.Name + "." + name), name);
            }
            
            internal static TinyType CreateComponent(this TinyModule module, string name)
            {
                return module.CreateComponent(TinyId.Generate(module.Name + "." + name), name);
            }
        }
    }
}