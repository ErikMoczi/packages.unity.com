using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// [WriteGroup] Can exclude components which are unknown at the time of creating the query that have been declared
    /// to write to the same component.
    ///
    /// This allows for extending systems of components safely without editing the previously existing systems.
    /// 
    /// The goal is to have a way for systems that expect to transform data from one set of components (inputs) to
    /// another (output[s]) be able to declare that explicit transform, and they exclusively know about one set of
    /// inputs. If there are other inputs that want to write to the same output, the query shouldn't match because it's
    /// a nonsensical/unhandled setup. It's both a way to guard against nonsensical components (having two systems write
    /// to the same output value), and a way to "turn off" existing systems/queries by putting a component with the same
    /// write lock on an entity, letting another system handle it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public class WriteGroupAttribute : Attribute
    {
        public WriteGroupAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public Type TargetType;
    }
    
    public static unsafe class TypeManager
    {
        [AttributeUsage(AttributeTargets.Struct)]
        public class ForcedMemoryOrderingAttribute : Attribute
        {
            public ForcedMemoryOrderingAttribute(ulong ordering)
            {
                MemoryOrdering = ordering;
            }

            public ulong MemoryOrdering;
        }

        [AttributeUsage(AttributeTargets.Struct)]
        public class TypeVersionAttribute : Attribute
        {
            public TypeVersionAttribute(int version)
            {
                TypeVersion = version;
            }

            public int TypeVersion;
        }

        public enum TypeCategory
        {
            ComponentData,
            BufferData,
            ISharedComponentData,
            EntityData,
            Class
        }

        public const int HasNoEntityReferencesFlag = 1 << 25; // this flag is inverted to ensure the type id of Entity can still be 1
        public const int BufferComponentTypeFlag = 1 << 26;
        public const int SystemStateTypeFlag = 1 << 27;
        public const int SharedComponentTypeFlag = 1 << 28;
        public const int ChunkComponentTypeFlag = 1<<29;
        public const int ZeroSizeTypeFlag = 1<<30;

        public const int ClearFlagsMask = 0x00FFFFFF;
        public const int SystemStateSharedComponentTypeFlag = SystemStateTypeFlag | SharedComponentTypeFlag;


        public const int MaximumTypesCount = 1024 * 10;
        private static volatile int s_Count;
#if !UNITY_CSHARP_TINY
        private static SpinLock s_CreateTypeLock;
#endif
        public static int ObjectOffset;

#if !UNITY_CSHARP_TINY
        public static IEnumerable<TypeInfo> AllTypes { get { return Enumerable.Take(s_Types, s_Count); } }
        private static Dictionary<ulong, int> s_StableTypeHashToTypeIndex;
#endif
        static TypeInfo[] s_Types;
        
#if !UNITY_ZEROPLAYER
        internal static Type UnityEngineComponentType;

        public static void RegisterUnityEngineComponentType(Type type)
        {
            if (type == null || !type.IsClass || type.IsInterface || type.FullName != "UnityEngine.Component")
                throw new ArgumentException($"{type} must be typeof(UnityEngine.Component).");
            UnityEngineComponentType = type;
        }
#endif
        private struct StaticTypeLookup<T>
        {
            public static int typeIndex;
        }

        public struct EntityOffsetInfo
        {
            public int Offset;
        }

        public struct EqualityHelper<T>
        {
            public delegate bool EqualsFn(T left, T right);
            public delegate int HashFn(T value);

            public static new EqualsFn Equals;
            public static HashFn Hash;
        }

#if !UNITY_CSHARP_TINY
        // https://stackoverflow.com/a/27851610
        static bool IsZeroSizeStruct(Type t)
        {
            return t.IsValueType && !t.IsPrimitive &&
                   t.GetFields((BindingFlags)0x34).All(fi => IsZeroSizeStruct(fi.FieldType));
        }
#endif

        // NOTE: This type will be moved into Unity.Entities.StaticTypeRegistry once Static Type Registry generation is hooked into #!UNITY_CSHARP_TINY builds
        public readonly struct TypeInfo
        {
#if !UNITY_CSHARP_TINY
            public TypeInfo(Type type, int typeIndex, int size, TypeCategory category, FastEquality.TypeInfo typeInfo, EntityOffsetInfo[] entityOffsets, ulong memoryOrdering, int bufferCapacity, int elementSize, int alignmentInBytes, ulong stableTypeHash, int* writeGroups, int writeGroupCount)
            {
                Type = type;
                TypeIndex = typeIndex;
                SizeInChunk = size;
                Category = category;
                FastEqualityTypeInfo = typeInfo;
                EntityOffsetCount = entityOffsets != null ? entityOffsets.Length : 0;
                EntityOffsets = entityOffsets;
                MemoryOrdering = memoryOrdering;
                BufferCapacity = bufferCapacity;
                ElementSize = elementSize;
                AlignmentInBytes = alignmentInBytes;
                StableTypeHash = stableTypeHash;
                WriteGroups = writeGroups;
                WriteGroupCount = writeGroupCount;

                // System state shared components are also considered system state components
                bool isSystemStateSharedComponent = typeof(ISystemStateSharedComponentData).IsAssignableFrom(type);
                bool isSystemStateComponent = isSystemStateSharedComponent || typeof(ISystemStateComponentData).IsAssignableFrom(type);

                if (typeIndex != 0)
                {
                    if (SizeInChunk == 0)
                        TypeIndex |= ZeroSizeTypeFlag;

                    if(Category == TypeCategory.ISharedComponentData)
                        TypeIndex |= SharedComponentTypeFlag;

                    if (isSystemStateComponent)
                        TypeIndex |= SystemStateTypeFlag;

                    if (isSystemStateSharedComponent)
                        TypeIndex |= SystemStateSharedComponentTypeFlag;

                    if (BufferCapacity >= 0)
                        TypeIndex |= BufferComponentTypeFlag;

                    if (EntityOffsetCount == 0)
                        TypeIndex |= HasNoEntityReferencesFlag;
                }
            }

                public readonly Type Type;
                public readonly int TypeIndex;
                // Note that this includes internal capacity and header overhead for buffers.
                public readonly int SizeInChunk;
                // Normally the same as SizeInChunk (for components), but for buffers means size of an individual element.
                public readonly int ElementSize;
                // Sometimes we need to know not only the size, but the alignment.
                public readonly int AlignmentInBytes;
                public readonly int BufferCapacity;
                public readonly FastEquality.TypeInfo FastEqualityTypeInfo;
                public readonly TypeCategory Category;
                // While this information is available in the Array for EntityOffsets this field allows us to keep Tiny vs non-Tiny code paths the same
                public readonly int EntityOffsetCount;
                public readonly EntityOffsetInfo[] EntityOffsets;
                public readonly ulong MemoryOrdering;
                public readonly ulong StableTypeHash;
                public readonly int* WriteGroups;
                public readonly int WriteGroupCount;

                public bool IsZeroSized => SizeInChunk == 0;
                public bool HasWriteGroups => WriteGroupCount > 0;
#else
            public TypeInfo(int typeIndex, TypeCategory category, int entityOffsetCount, int entityOffsetStartIndex, ulong memoryOrdering, ulong stableTypeHash, int bufferCapacity, int typeSize, int elementSize, int alignmentInBytes, bool isSystemStateComponent, bool isSystemStateSharedComponent)
            {
                TypeIndex = typeIndex;
                Category = category;
                EntityOffsetCount = entityOffsetCount;
                EntityOffsetStartIndex = entityOffsetStartIndex;
                MemoryOrdering = memoryOrdering;
                StableTypeHash = stableTypeHash;
                BufferCapacity = bufferCapacity;
                SizeInChunk = typeSize;
                AlignmentInBytes = alignmentInBytes;
                ElementSize = elementSize;

                if (typeIndex != 0)
                {
                    if (SizeInChunk == 0)
                        TypeIndex |= ZeroSizeTypeFlag;

                    if(Category == TypeCategory.ISharedComponentData)
                        TypeIndex |= SharedComponentTypeFlag;

                    //System state shared components are also considered system state components
                    if (isSystemStateComponent || isSystemStateSharedComponent)
                        TypeIndex |= SystemStateTypeFlag;

                    if (isSystemStateSharedComponent)
                        TypeIndex |= SystemStateSharedComponentTypeFlag;

                    if (Category == TypeCategory.BufferData)
                        TypeIndex |= BufferComponentTypeFlag;

                    if (EntityOffsetCount == 0)
                        TypeIndex |= HasNoEntityReferencesFlag;
                }
            }

            public readonly int TypeIndex;
            // Note that this includes internal capacity and header overhead for buffers.
            public readonly int SizeInChunk;
            // Sometimes we need to know not only the size, but the alignment.
            public readonly int AlignmentInBytes;
            // Normally the same as SizeInChunk (for components), but for buffers means size of an individual element.
            public readonly int ElementSize;
            public readonly int BufferCapacity;
            public readonly TypeCategory Category;
            public readonly ulong MemoryOrdering;
            public readonly ulong StableTypeHash;
            public readonly int EntityOffsetCount;
            public readonly int EntityOffsetStartIndex;

            public bool IsZeroSized => SizeInChunk == 0;
            public EntityOffsetInfo* EntityOffsets => EntityOffsetCount > 0 ? ((EntityOffsetInfo*) UnsafeUtility.AddressOf(ref StaticTypeRegistry.StaticTypeRegistry.EntityOffsets[0])) + EntityOffsetStartIndex : null;
#endif
        }

        public static unsafe TypeInfo GetTypeInfo(int typeIndex)
        {
            return s_Types[typeIndex & ClearFlagsMask];
        }

        public static TypeInfo GetTypeInfo<T>() where T : struct
        {
            return s_Types[GetTypeIndex<T>() & ClearFlagsMask];
        }

        public static Type GetType(int typeIndex)
        {
            #if !UNITY_CSHARP_TINY
                return s_Types[typeIndex & ClearFlagsMask].Type;
            #else
                return StaticTypeRegistry.StaticTypeRegistry.Types[typeIndex & ClearFlagsMask];
            #endif
        }

        public static int GetTypeCount()
        {
            return s_Count;
        }

        public static bool IsBuffer(int typeIndex) => (typeIndex & TypeManager.BufferComponentTypeFlag) != 0;
        public static bool IsSystemStateComponent(int typeIndex) => (typeIndex & TypeManager.SystemStateTypeFlag) != 0;
        public static bool IsSystemStateSharedComponent(int typeIndex) => (typeIndex & TypeManager.SystemStateSharedComponentTypeFlag) == TypeManager.SystemStateSharedComponentTypeFlag;
        public static bool IsSharedComponent(int typeIndex) => (typeIndex & TypeManager.SharedComponentTypeFlag) != 0;
        public static bool IsZeroSized(int typeIndex) => (typeIndex & TypeManager.ZeroSizeTypeFlag) != 0;
        public static bool IsChunkComponent(int typeIndex) => (typeIndex & TypeManager.ChunkComponentTypeFlag) != 0;
        public static bool HasEntityReferences(int typeIndex) => (typeIndex & TypeManager.HasNoEntityReferencesFlag) == 0;


        // TODO: this creates a dependency on UnityEngine, but makes splitting code in separate assemblies easier. We need to remove it during the biggere refactor.
        private struct ObjectOffsetType
        {
            private void* v0;
            private void* v1;
        }

#if !UNITY_CSHARP_TINY
        private static void AddTypeInfoToTables(TypeInfo typeInfo)
        {
            s_Types[typeInfo.TypeIndex & ClearFlagsMask] = typeInfo;
            s_StableTypeHashToTypeIndex.Add(typeInfo.StableTypeHash, typeInfo.TypeIndex);
            ++s_Count;
        }
#endif

        public static void Initialize()
        {
            if (s_Types != null)
                return;

            ObjectOffset = UnsafeUtility.SizeOf<ObjectOffsetType>();

#if !UNITY_CSHARP_TINY
            s_CreateTypeLock = new SpinLock();
#endif
            s_Types = new TypeInfo[MaximumTypesCount];

            #if !UNITY_CSHARP_TINY
                s_StableTypeHashToTypeIndex = new Dictionary<ulong, int>();
            #endif

            s_Count = 0;

            #if !UNITY_CSHARP_TINY
                AddTypeInfoToTables(new TypeInfo(null, 0, 0, TypeCategory.ComponentData, FastEquality.TypeInfo.Null, null, 0, -1, 0, 1, 0, null, 0));

                // This must always be first so that Entity is always index 0 in the archetype
                AddTypeInfoToTables(new TypeInfo(typeof(Entity), 1, sizeof(Entity), TypeCategory.EntityData,
                    FastEquality.CreateTypeInfo<Entity>(), EntityRemapUtility.CalculateEntityOffsets<Entity>(), 0, -1, sizeof(Entity), UnsafeUtility.AlignOf<Entity>(), CalculateStableTypeHash(typeof(Entity)), null, 0));

                InitializeAllComponentTypes();
            #else
                StaticTypeRegistry.StaticTypeRegistry.RegisterStaticTypes();
            #endif
        }

#if UNITY_CSHARP_TINY
        // Called by the StaticTypeRegistry
        internal static void AddStaticTypesFromRegistry(ref TypeInfo[] typeArray, int count)
        {
            if (count >= MaximumTypesCount)
                throw new Exception("More types detected than MaximumTypesCount. Increase the static buffer size.");

            s_Count = 0;
            for (int i = 0; i < count; ++i)
            {
                s_Types[s_Count++] = typeArray[i];
            }
        }
#endif

#if !UNITY_CSHARP_TINY
        
        static void InitializeAllComponentTypes()
        {
            var componentTypeSet = new HashSet<Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsAssemblyReferencingEntities(assembly))
                    continue;

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || !type.IsValueType || !UnsafeUtility.IsBlittable(type))
                        continue;

                    if (typeof(IComponentData).IsAssignableFrom(type) ||
                        typeof(ISharedComponentData).IsAssignableFrom(type) ||
                        typeof(IBufferElementData).IsAssignableFrom(type))
                    {
                        componentTypeSet.Add(type);
                    }
                }
            }
            
            var lockTaken = false;
            try
            {
                s_CreateTypeLock.Enter(ref lockTaken);

                var componentTypeCount = componentTypeSet.Count;
                var componentTypes = new Type[componentTypeCount];
                componentTypeSet.CopyTo(componentTypes);

                var typeIndexByType = new Dictionary<Type, int>();
                var writeGroupByType = new Dictionary<int, HashSet<int>>();
                var startTypeIndex = s_Count;

                for (int i = 0; i < componentTypes.Length; i++)
                    typeIndexByType[componentTypes[i]] = startTypeIndex + i;
                
                GatherWriteGroups(componentTypes, startTypeIndex, typeIndexByType, writeGroupByType);
                AddAllComponentTypes(componentTypes, startTypeIndex, writeGroupByType);
            }
            finally
            {
                if (lockTaken)
                {
                    s_CreateTypeLock.Exit(true);
                }
            }
        }

        private static void AddAllComponentTypes(Type[] componentTypes, int startTypeIndex, Dictionary<int, HashSet<int>> writeGroupByType)
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                var type = componentTypes[i];
                var expectedTypeIndex = startTypeIndex + i;
                var index = FindTypeIndex(type);
                if (index != -1)
                    throw new InvalidOperationException("ComponentType cannot be initialized more than once.");

                TypeInfo typeInfo;
                if (writeGroupByType.ContainsKey(expectedTypeIndex))
                {
                    var writeGroupSet = writeGroupByType[expectedTypeIndex];
                    var writeGroupCount = writeGroupSet.Count;
                    var writeGroupArray = new int[writeGroupCount];
                    writeGroupSet.CopyTo(writeGroupArray);

                    fixed (int* writeGroups = writeGroupArray)
                    {
                        typeInfo = BuildComponentType(type, writeGroups, writeGroupCount);
                    }
                }
                else
                {
                    typeInfo = BuildComponentType(type);
                }

                var typeIndex = typeInfo.TypeIndex & TypeManager.ClearFlagsMask;
                if (expectedTypeIndex != typeIndex)
                    throw new InvalidOperationException("ComponentType.TypeIndex does not match precalculated index.");
                
                AddTypeInfoToTables(typeInfo); //c
            }
        }

        private static void GatherWriteGroups(Type[] componentTypes, int startTypeIndex, Dictionary<Type, int> typeIndexByType,
            Dictionary<int, HashSet<int>> writeGroupByType)
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                var type = componentTypes[i];
                var typeIndex = startTypeIndex + i;

                foreach (var attribute in type.GetCustomAttributes(typeof(WriteGroupAttribute)))
                {
                    var attr = (WriteGroupAttribute) attribute;
                    int targetTypeIndex = typeIndexByType[attr.TargetType];

                    if (!writeGroupByType.ContainsKey(targetTypeIndex))
                    {
                        var targetList = new HashSet<int>();
                        writeGroupByType.Add(targetTypeIndex, targetList);
                    }

                    writeGroupByType[targetTypeIndex].Add(typeIndex);
                }
            }
        }

        private static int FindTypeIndex(Type type)
        {
            for (var i = 0; i != s_Count; i++)
            {
                var c = s_Types[i];
                if (c.Type == type)
                    return c.TypeIndex;
            }

            return -1;
        }
#else
        private static int FindTypeIndex(Type type)
        {
            for (var i = 0; i != s_Count; i++)
            {
                var c = s_Types[i];
                if (StaticTypeRegistry.StaticTypeRegistry.Types[c.TypeIndex & ClearFlagsMask] == type)
                    return c.TypeIndex;
            }

            throw new InvalidOperationException("Tried to GetTypeIndex for type that has not been set up by the static type registry.");
        }
#endif

        public static int GetTypeIndex<T>()
        {
            var typeIndex = StaticTypeLookup<T>.typeIndex;
            if (typeIndex != 0)
                return typeIndex;

            typeIndex = GetTypeIndex(typeof(T));

            StaticTypeLookup<T>.typeIndex = typeIndex;
            return typeIndex;
        }

        public static int GetTypeIndex(Type type)
        {
            var index = FindTypeIndex(type);
            return index != -1 ? index : CreateTypeIndexThreadSafe(type);
        }

        public static bool Equals<T>(ref T left, ref T right) where T : struct
        {
            #if !UNITY_CSHARP_TINY
                var typeInfo = TypeManager.GetTypeInfo<T>().FastEqualityTypeInfo;
                return FastEquality.Equals(ref left, ref right, typeInfo);
            #else
                return EqualityHelper<T>.Equals(left, right);
            #endif
        }

        public static bool Equals(void* left, void* right, int typeIndex)
        {
            #if !UNITY_CSHARP_TINY
                var typeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
                return FastEquality.Equals(left, right, typeInfo);
            #else
                return StaticTypeRegistry.StaticTypeRegistry.Equals(left, right, typeIndex & ClearFlagsMask);
            #endif
        }

        public static int GetHashCode<T>(ref T val) where T : struct
        {
            #if !UNITY_CSHARP_TINY
                var typeInfo = TypeManager.GetTypeInfo<T>().FastEqualityTypeInfo;
                return FastEquality.GetHashCode(ref val, typeInfo);
            #else
                return EqualityHelper<T>.Hash(val);
            #endif
        }

        public static int GetHashCode(void* val, int typeIndex)
        {
            #if !UNITY_CSHARP_TINY
                var typeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
                return FastEquality.GetHashCode(val, typeInfo);
            #else
                return StaticTypeRegistry.StaticTypeRegistry.GetHashCode(val, typeIndex & ClearFlagsMask);
            #endif
        }

        public static int GetTypeIndexFromStableTypeHash(ulong stableTypeHash)
        {
#if !UNITY_CSHARP_TINY
            if(s_StableTypeHashToTypeIndex.TryGetValue(stableTypeHash, out var typeIndex))
                return typeIndex;
            return -1;
#else
            throw new InvalidOperationException("Not allowed in Project Tiny");
#endif
        }

#if !UNITY_CSHARP_TINY
        public static bool IsAssemblyReferencingEntities(Assembly assembly)
        {
            const string entitiesAssemblyName = "Unity.Entities";
            if (assembly.GetName().Name.Contains(entitiesAssemblyName))
                return true;

            var referencedAssemblies = assembly.GetReferencedAssemblies();
            foreach(var referenced in referencedAssemblies)
                if (referenced.Name.Contains(entitiesAssemblyName))
                    return true;
            return false;
        }
#endif


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly Type[] s_SingularInterfaces =
        {
            typeof(IComponentData),
            typeof(IBufferElementData),
            typeof(ISharedComponentData),
        };

        internal static void CheckComponentType(Type type)
        {
            int typeCount = 0;
            foreach (Type t in s_SingularInterfaces)
            {
                if (t.IsAssignableFrom(type))
                    ++typeCount;
            }

            if (typeCount > 1)
                throw new ArgumentException($"Component {type} can only implement one of IComponentData, ISharedComponentData and IBufferElementData");
        }
#endif
        
        public static NativeArray<int> GetWriteGroupTypes(int typeIndex)
        {
#if UNITY_CSHARP_TINY
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(null, 0, Allocator.None);
#else
            var type = GetTypeInfo(typeIndex);
            var writeGroups = type.WriteGroups;
            var writeGroupCount = type.WriteGroupCount;
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(writeGroups, writeGroupCount, Allocator.None);
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, AtomicSafetyHandle.Create());
#endif
            return arr;
        }

#if !UNITY_CSHARP_TINY
        //
        // The reflection-based type registration path that we can't support with tiny csharp profile.
        // A generics compile-time path is temporarily used (see later in the file) until we have
        // full static type info generation working.
        //
        static ulong CalculateMemoryOrdering(Type type)
        {
            if (type == typeof(Entity))
                return 0;

            var forcedOrdering = type.GetCustomAttribute<ForcedMemoryOrderingAttribute>();
            if (forcedOrdering != null)
                return forcedOrdering.MemoryOrdering;

            var result = CalculateStableTypeHash(type);
            result = result != 0 ? result : 1;
            return result;
        }
        
        private static int CreateTypeIndexThreadSafe(Type type)
        {
            var lockTaken = false;
            try
            {
                s_CreateTypeLock.Enter(ref lockTaken);

                // After taking the lock, make sure the type hasn't been created
                // after doing the non-atomic FindTypeIndex
                var index = FindTypeIndex(type);
                if (index != -1)
                    return index;

                var componentType = BuildComponentType(type);

                AddTypeInfoToTables(componentType);
                return componentType.TypeIndex;
            }
            finally
            {
                if (lockTaken)
                {
                    s_CreateTypeLock.Exit(true);
                }
            }
        }

        private static ulong HashStringWithFNV1A64(string text)
        {
            // Using http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            // with basis and prime:
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            ulong result = offsetBasis;
            foreach (var c in text)
            {
                result = prime * (result ^ (byte)(c & 255));
                result = prime * (result ^ (byte)(c >> 8));
            }
            return result;
        }

        static ulong CalculateStableTypeHash(Type type)
        {
            return HashStringWithFNV1A64(type.AssemblyQualifiedName);
        }

        internal static TypeInfo BuildComponentType(Type type)
        {
            return BuildComponentType(type, null, 0);
        }

        internal static TypeInfo BuildComponentType(Type type, int* writeGroups, int writeGroupCount)
        {
            var componentSize = 0;
            TypeCategory category;
            var typeInfo = FastEquality.TypeInfo.Null;
            EntityOffsetInfo[] entityOffsets = null;
            int bufferCapacity = -1;
            var memoryOrdering = CalculateMemoryOrdering(type);
            var stableTypeHash = CalculateStableTypeHash(type);
            
            int elementSize = 0;
            int alignmentInBytes = 0;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (type.IsInterface)
                throw new ArgumentException($"{type} is an interface. It must be a concrete type.");
#endif
            if (typeof(IComponentData).IsAssignableFrom(type))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!type.IsValueType)
                    throw new ArgumentException($"{type} is an IComponentData, and thus must be a struct.");
                if (!UnsafeUtility.IsBlittable(type))
                    throw new ArgumentException(
                        $"{type} is an IComponentData, and thus must be blittable (No managed object is allowed on the struct).");
#endif

                category = TypeCategory.ComponentData;
                if (TypeManager.IsZeroSizeStruct(type))
                    componentSize = 0;
                else
                    componentSize = UnsafeUtility.SizeOf(type);

                typeInfo = FastEquality.CreateTypeInfo(type);
                entityOffsets = EntityRemapUtility.CalculateEntityOffsets(type);

                int sizeInBytes = UnsafeUtility.SizeOf(type);
                // TODO: Implement UnsafeUtility.AlignOf(type)
                alignmentInBytes = 16;
                if(sizeInBytes < 16 && (sizeInBytes & (sizeInBytes-1))==0)
                    alignmentInBytes = sizeInBytes;
            }
            else if (typeof(IBufferElementData).IsAssignableFrom(type))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!type.IsValueType)
                    throw new ArgumentException($"{type} is an IBufferElementData, and thus must be a struct.");
                if (!UnsafeUtility.IsBlittable(type))
                    throw new ArgumentException(
                        $"{type} is an IBufferElementData, and thus must be blittable (No managed object is allowed on the struct).");
#endif

                category = TypeCategory.BufferData;
                elementSize = UnsafeUtility.SizeOf(type);

                var capacityAttribute = (InternalBufferCapacityAttribute) type.GetCustomAttribute(typeof(InternalBufferCapacityAttribute));
                if (capacityAttribute != null)
                    bufferCapacity = capacityAttribute.Capacity;
                else
                    bufferCapacity = 128 / elementSize; // Rather than 2*cachelinesize, to make it cross platform deterministic

                componentSize = sizeof(BufferHeader) + bufferCapacity * elementSize;
                typeInfo = FastEquality.CreateTypeInfo(type);
                entityOffsets = EntityRemapUtility.CalculateEntityOffsets(type);

                int sizeInBytes = UnsafeUtility.SizeOf(type);
                // TODO: Implement UnsafeUtility.AlignOf(type)
                alignmentInBytes = 16;
                if(sizeInBytes < 16 && (sizeInBytes & (sizeInBytes-1))==0)
                    alignmentInBytes = sizeInBytes;
             }
            else if (typeof(ISharedComponentData).IsAssignableFrom(type))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!type.IsValueType)
                    throw new ArgumentException($"{type} is an ISharedComponentData, and thus must be a struct.");
#endif
                entityOffsets = EntityRemapUtility.CalculateEntityOffsets(type);
                category = TypeCategory.ISharedComponentData;
                typeInfo = FastEquality.CreateTypeInfo(type);
            }
            else if (type.IsClass)
            {
                category = TypeCategory.Class;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (type.FullName == "Unity.Entities.GameObjectEntity")
                    throw new ArgumentException(
                        "GameObjectEntity cannot be used from EntityManager. The component is ignored when creating entities for a GameObject.");
                if (UnityEngineComponentType == null)
                    throw new ArgumentException(
                        $"{type} cannot be used from EntityManager. If it inherits UnityEngine.Component, you must first register TypeManager.UnityEngineComponentType or include the Unity.Entities.Hybrid assembly in your build.");
                if (!UnityEngineComponentType.IsAssignableFrom(type))
                    throw new ArgumentException($"{type} must inherit {UnityEngineComponentType}.");
#endif
            }
            else
            {
                throw new ArgumentException($"{type} is not a valid component.");
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckComponentType(type);
#endif
            int typeIndex = s_Count;
            return new TypeInfo(type, typeIndex, componentSize, category, typeInfo, entityOffsets, memoryOrdering, bufferCapacity, elementSize > 0 ? elementSize : componentSize, alignmentInBytes, stableTypeHash, writeGroups, writeGroupCount);
        }

        public static int CreateTypeIndexForComponent<T>() where T : struct, IComponentData
        {
            return GetTypeIndex(typeof(T));
        }

        public static int CreateTypeIndexForSharedComponent<T>() where T : struct, ISharedComponentData
        {
            return GetTypeIndex(typeof(T));
        }

        public static int CreateTypeIndexForBufferElement<T>() where T : struct, IBufferElementData
        {
            return GetTypeIndex(typeof(T));
        }
#else
        private static int CreateTypeIndexThreadSafe(Type type)
        {
            throw new InvalidOperationException("Tried to GetTypeIndex for type that has not been set up by the static registry.");
        }
#endif
    }
}
