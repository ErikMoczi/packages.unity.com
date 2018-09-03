using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Diagnostics;

namespace Unity.Collections
{
	unsafe struct NativeListData
	{
		public void*                            buffer;
		public int								length;
		public int								capacity;

		public unsafe static void DeallocateList(void* buffer, Allocator allocation)
		{
			NativeListData* data = (NativeListData*)buffer;
			UnsafeUtility.Free (data->buffer, allocation);
			data->buffer = null;
			UnsafeUtility.Free (buffer, allocation);
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	[NativeContainer]
	[DebuggerDisplay("Length = {Length}")]
	[DebuggerTypeProxy(typeof(NativeListDebugView < >))]
	unsafe public struct NativeList<T> : IDisposable where T : struct
	{
		[NativeDisableUnsafePtrRestriction]
		internal NativeListData*        m_ListData;
		#if ENABLE_UNITY_COLLECTIONS_CHECKS
		internal AtomicSafetyHandle 	m_Safety;
		[NativeSetClassTypeToNullOnSchedule]
		DisposeSentinel					m_DisposeSentinel;
		#endif
	    Allocator 						m_AllocatorLabel;

		unsafe public T this [int index]
		{
			get
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                if ((uint)index >= (uint)m_ListData->length)
                    throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range in NativeList of '{1}' Length.", index, m_ListData->length));
#endif

                return UnsafeUtility.ReadArrayElement<T>(m_ListData->buffer, index);
			}

			set
			{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                if ((uint)index >= (uint)m_ListData->length)
                    throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range in NativeList of '{1}' Length.", index, m_ListData->length));
#endif

                UnsafeUtility.WriteArrayElement<T>(m_ListData->buffer, index, value);
			}
		}

		unsafe public int Length
		{
			get
			{
				#if ENABLE_UNITY_COLLECTIONS_CHECKS
				AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
				#endif

				return m_ListData->length;
			}
		}

		unsafe public int Capacity
		{
			get
			{
				#if ENABLE_UNITY_COLLECTIONS_CHECKS
				AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
				#endif

				return m_ListData->capacity;
			}

			set
			{
				#if ENABLE_UNITY_COLLECTIONS_CHECKS
				AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);

			    if (value < m_ListData->length)
			        throw new System.ArgumentException("Capacity must be larger than the length of the NativeList.");
				#endif

				if (m_ListData->capacity == value)
					return;

				void* newData = UnsafeUtility.Malloc (value * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), m_AllocatorLabel);
				UnsafeUtility.MemCpy (newData, m_ListData->buffer, m_ListData->length * UnsafeUtility.SizeOf<T>());
				UnsafeUtility.Free (m_ListData->buffer, m_AllocatorLabel);
			    m_ListData->buffer = newData;
			    m_ListData->capacity = value;
			}
		}

		unsafe public NativeList(Allocator i_label) : this (1, i_label, 1) { }
		unsafe public NativeList(int capacity, Allocator i_label) : this (capacity, i_label, 1) { }

		unsafe private NativeList(int capacity, Allocator i_label, int stackDepth)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<T>())
                throw new ArgumentException(string.Format("{0} used in NativeList<{0}> must be blittable", typeof(T)));
#endif

            NativeListData* data  = (NativeListData*)UnsafeUtility.Malloc (sizeof(NativeListData), UnsafeUtility.AlignOf<NativeListData>(), i_label);

			int elementSize = UnsafeUtility.SizeOf<T> ();

            //@TODO: Find out why this is needed?
            capacity = Math.Max(1, capacity);
			data->buffer = UnsafeUtility.Malloc (capacity * elementSize, UnsafeUtility.AlignOf<T>(), i_label);

			data->length = 0;
			data->capacity = capacity;

		    m_ListData = data;
			m_AllocatorLabel = i_label;

#if ENABLE_UNITY_COLLECTIONS_CHECKS

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, stackDepth);
#endif
		}

		unsafe public void Add(T element)
		{
			NativeListData* data = m_ListData;
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
			#endif

			if (data->length >= data->capacity)
				Capacity = data->length + data->capacity * 2;

			int length = data->length;
			data->length = length + 1;
			this[length] = element;
		}

        //@TODO: Test for AddRange
        unsafe public void AddRange(NativeArray<T> elements)
        {
            NativeListData* data = m_ListData;
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
            #endif

            if (data->length + elements.Length > data->capacity)
                Capacity = data->length + elements.Length * 2;

            int sizeOf = UnsafeUtility.SizeOf<T> ();
            UnsafeUtility.MemCpy((byte*)data->buffer + data->length * sizeOf, elements.GetUnsafePtr(), sizeOf * elements.Length);

            data->length += elements.Length;
        }

		unsafe public void RemoveAtSwapBack(int index)
		{
			NativeListData* data = m_ListData;
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
			#endif

			int newLength = Length - 1;
			this[index] = this[newLength];
			data->length = newLength;
		}

		public bool IsCreated
		{
			get { return m_ListData != null; }
		}

		unsafe public void Dispose()
		{
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(m_Safety, ref m_DisposeSentinel);
			#endif

			NativeListData.DeallocateList(m_ListData, m_AllocatorLabel);
			m_ListData = null;
		}

		public void Clear()
		{
			ResizeUninitialized (0);
		}

		unsafe public static implicit operator NativeArray<T> (NativeList<T> nativeList)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle arraySafety = new AtomicSafetyHandle();
			AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(nativeList.m_Safety);
			arraySafety = nativeList.m_Safety;
			AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif

			NativeListData* data = (NativeListData*)nativeList.m_ListData;
			var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T> (data->buffer, data->length, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif

            return array;
		}

	    public NativeArray<T> ToDeferredJobArray()
	    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	        AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
	        AtomicSafetyHandle arraySafety = m_Safety;
	        AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif

	        byte* buffer = (byte*)m_ListData;
	        buffer += 1;
	        var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T> (buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
	        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif

	        return array;
	    }
	    
		unsafe public T[] ToArray()
		{
			NativeArray<T> nativeArray = this;
			return nativeArray.ToArray();
		}

		public void CopyFrom(T[] array)
		{
			//@TODO: Thats not right... This doesn't perform a resize
			Capacity = array.Length;
			NativeArray<T> nativeArray = this;
			nativeArray.CopyFrom(array);
		}

		public unsafe void ResizeUninitialized(int length)
		{
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
			AtomicSafetyHandle.CheckWriteAndThrow (m_Safety);
			#endif

			Capacity = math.max(length, Capacity);
			NativeListData* data = (NativeListData*)m_ListData;
			data->length = length;
		}
	}
    
    internal sealed class NativeListDebugView<T> where T : struct
    {
        private NativeList<T> m_Array;

        public NativeListDebugView(NativeList<T> array)
        {
            m_Array = array;
        }

        public T[] Items
        {
            get { return m_Array.ToArray(); }
        }
    }
}
namespace Unity.Collections.LowLevel.Unsafe
{
	public static class NativeListUnsafeUtility
	{
        public static unsafe void* GetUnsafePtr<T>(this NativeList<T> nativeList) where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(nativeList.m_Safety);
#endif
			NativeListData* data = (NativeListData*)nativeList.m_ListData;
			return data->buffer;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
		public static AtomicSafetyHandle GetAtomicSafetyHandle<T>(ref NativeList<T> nativeList) where T : struct
        {
			return nativeList.m_Safety;
        }
#endif	    

		public static unsafe void* GetInternalListDataPtrUnchecked<T>(ref NativeList<T> nativeList) where T : struct
        {
			return nativeList.m_ListData;
        }
	}
}
