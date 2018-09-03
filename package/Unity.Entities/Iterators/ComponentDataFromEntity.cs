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
        int                     m_TypeLookupCache;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal ComponentDataFromEntity(int typeIndex, EntityDataManager* entityData, AtomicSafetyHandle safety)
        {
            m_Safety = safety;
            m_TypeIndex = typeIndex;
            m_Entities = entityData;
            m_TypeLookupCache = 0;
        }
#else
        internal ComponentDataFromEntity(int typeIndex, EntityDataManager* entityData)
        {
            m_TypeIndex = typeIndex;
            m_Entities = entityData;
            m_TypeLookupCache = 0;
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

                void* ptr = m_Entities->GetComponentDataWithType(entity, m_TypeIndex, ref m_TypeLookupCache);
                T data;
                UnsafeUtility.CopyPtrToStructure(ptr, out data);

                return data;
            }
			set
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                m_Entities->AssertEntityHasComponent(entity, m_TypeIndex);

                void* ptr = m_Entities->GetComponentDataWithType(entity, m_TypeIndex);
                UnsafeUtility.CopyStructureToPtr(ref value, ptr);
			}
		}
	}
}
