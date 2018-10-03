﻿using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [NativeContainer]
    public unsafe struct ComponentDataFromEntity<T> where T : struct, IComponentData
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        readonly AtomicSafetyHandle      m_Safety;
#endif
        [NativeDisableUnsafePtrRestriction]
        readonly EntityDataManager*      m_Entities;
        readonly int                     m_TypeIndex;
        readonly uint                    m_GlobalSystemVersion;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        readonly bool                    m_IsZeroSized;          // cache of whether T is zero-sized
#endif
        int                              m_TypeLookupCache;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal ComponentDataFromEntity(int typeIndex, EntityDataManager* entityData, AtomicSafetyHandle safety)
        {
            m_Safety = safety;
            m_TypeIndex = typeIndex;
            m_Entities = entityData;
            m_TypeLookupCache = 0;
            m_GlobalSystemVersion = entityData->GlobalSystemVersion;
            m_IsZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized;
        }
#else
        internal ComponentDataFromEntity(int typeIndex, EntityDataManager* entityData)
        {
            m_TypeIndex = typeIndex;
            m_Entities = entityData;
            m_TypeLookupCache = 0;
            m_GlobalSystemVersion = entityData->GlobalSystemVersion;
        }
#endif

        public bool Exists(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            //@TODO: out of bounds index checks...

            return m_Entities->HasComponent(entity, m_TypeIndex);
        }

        public T this[Entity entity]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                m_Entities->AssertEntityHasComponent(entity, m_TypeIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (m_IsZeroSized)
                   throw new System.ArgumentException($"ComponentDataFromEntity<{typeof(T)}> indexer can not get the component because it is zero sized, you can use Exists instead.");
#endif
                
                T data;
                void* ptr = m_Entities->GetComponentDataWithTypeRO(entity, m_TypeIndex, ref m_TypeLookupCache);
                UnsafeUtility.CopyPtrToStructure(ptr, out data);

                return data;
            }
			set
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                m_Entities->AssertEntityHasComponent(entity, m_TypeIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
			    if (m_IsZeroSized)
			        throw new System.ArgumentException($"ComponentDataFromEntity<{typeof(T)}> indexer can not set the component because it is zero sized, you can use Exists instead.");
#endif

                void* ptr = m_Entities->GetComponentDataWithTypeRW(entity, m_TypeIndex, m_GlobalSystemVersion, ref m_TypeLookupCache);
                UnsafeUtility.CopyStructureToPtr(ref value, ptr);
			}
		}
	}
}
