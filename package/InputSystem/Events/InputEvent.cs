using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: can we get rid of the timestamp offsetting in the player and leave that complication for the editor only?

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A chunk of memory signaling a data transfer in the input system.
    /// </summary>
    // NOTE: This has to be layout compatible with native events.
    [StructLayout(LayoutKind.Explicit, Size = kBaseEventSize)]
    public struct InputEvent
    {
        private const uint kHandledMask = 0x80000000;
        private const uint kIdMask = 0x7FFFFFFF;

        public const int kBaseEventSize = 20;
        public const int kInvalidId = 0;
        public const int kAlignment = 4;

        [FieldOffset(0)] private FourCC m_Type;
        [FieldOffset(4)] private ushort m_SizeInBytes;
        [FieldOffset(6)] private ushort m_DeviceId;
        [FieldOffset(8)] internal uint m_EventId;
        [FieldOffset(12)] private double m_Time;

        /// <summary>
        /// Type code for the event.
        /// </summary>
        public FourCC type
        {
            get => m_Type;
            set => m_Type = value;
        }

        /// <summary>
        /// Total size of the event in bytes.
        /// </summary>
        /// <remarks>
        /// Events are variable-size structs. This field denotes the total size of the event
        /// as stored in memory. This includes the full size of this struct and not just the
        /// "payload" of the event.
        /// </remarks>
        /// <example>
        /// Store event in private buffer:
        /// <code>
        /// unsafe byte[] CopyEventData(InputEventPtr eventPtr)
        /// {
        ///     var sizeInBytes = eventPtr.sizeInBytes;
        ///     var buffer = new byte[sizeInBytes];
        ///     fixed (byte* bufferPtr = buffer)
        ///     {
        ///         UnsafeUtility.MemCpy(new IntPtr(bufferPtr), eventPtr.data, sizeInBytes);
        ///     }
        ///     return buffer;
        /// }
        /// </code>
        /// </example>
        public uint sizeInBytes
        {
            get => m_SizeInBytes;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentException("Maximum event size is " + ushort.MaxValue, nameof(value));
                m_SizeInBytes = (ushort)value;
            }
        }

        /// <summary>
        /// Unique serial ID of the event.
        /// </summary>
        /// <remarks>
        /// Events are assigned running IDs when they are put on an event queue.
        /// </remarks>
        public int eventId
        {
            get => (int)(m_EventId & kIdMask);
            set => m_EventId = (uint)value | (m_EventId & ~kIdMask);
        }

        /// <summary>
        /// ID of the device that the event is for.
        /// </summary>
        /// <remarks>
        /// Device IDs are allocated by the <see cref="IInputRuntime">runtime</see>. No two devices
        /// will receive the same ID over an application lifecycle regardless of whether the devices
        /// existed at the same time or not.
        /// </remarks>
        /// <seealso cref="InputDevice.id"/>
        /// <seealso cref="InputSystem.GetDeviceById"/>
        public int deviceId
        {
            get => m_DeviceId;
            set => m_DeviceId = (ushort)value;
        }

        /// <summary>
        /// Time that the event was generated at.
        /// </summary>
        /// <remarks>
        /// Times are in seconds and progress linearly in real-time. The timeline is the
        /// same as for <see cref="Time.realtimeSinceStartup"/>.
        /// </remarks>
        public double time
        {
            get => m_Time - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
            set => m_Time = value + InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
        }

        /// <summary>
        /// This is the raw input timestamp without the offset to <see cref="Time.realtimeSinceStartup"/>.
        /// </summary>
        /// <remarks>
        /// Internally, we always store all timestamps in "input time" which is relative to the native
        /// function GetTimeSinceStartup(). <see cref="IInputRuntime.currentTime"/> yields the current
        /// time on this timeline.
        /// </remarks>
        internal double internalTime
        {
            get => m_Time;
            set => m_Time = value;
        }

        public InputEvent(FourCC type, int sizeInBytes, int deviceId, double time = -1)
        {
            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;

            m_Type = type;
            m_SizeInBytes = (ushort)sizeInBytes;
            m_DeviceId = (ushort)deviceId;
            m_Time = time;
            m_EventId = kInvalidId;
        }

        // We internally use bits inside m_EventId as flags. IDs are linearly counted up by the
        // native input system starting at 1 so we have plenty room.
        // NOTE: The native system assigns IDs when events are queued so if our handled flag
        //       will implicitly get overwritten. Having events go back to unhandled state
        //       when they go on the queue makes sense in itself, though, so this is fine.
        public bool handled
        {
            get => (m_EventId & kHandledMask) == kHandledMask;
            set
            {
                if (value)
                    m_EventId |= kHandledMask;
                else
                    m_EventId &= ~kHandledMask;
            }
        }

        public override string ToString()
        {
            return $"id={eventId} type={type} device={deviceId} size={sizeInBytes} time={time}";
        }

        /// <summary>
        /// Get the next event after the given one.
        /// </summary>
        /// <param name="currentPtr">A valid event pointer.</param>
        /// <returns>Pointer to the next event in memory.</returns>
        /// <remarks>
        /// This method applies no checks and must only be called if there is an event following the
        /// given one. Also, the size of the given event must be 100% as the method will simply
        /// take the size and advance the given pointer by it (and aligning it to <see cref="kAlignment"/>).
        /// </remarks>
        /// <seealso cref="GetNextInMemoryChecked"/>
        internal static unsafe InputEvent* GetNextInMemory(InputEvent* currentPtr)
        {
            Debug.Assert(currentPtr != null);
            var alignedSizeInBytes = NumberHelpers.AlignToMultiple(currentPtr->sizeInBytes, kAlignment);
            return (InputEvent*)((byte*)currentPtr + alignedSizeInBytes);
        }

        /// <summary>
        /// Get the next event after the given one. Throw if that would point to invalid memory as indicated
        /// by the given memory buffer.
        /// </summary>
        /// <param name="currentPtr">A valid event pointer to an event inside <paramref name="buffer"/>.</param>
        /// <param name="buffer">Event buffer in which to advance to the next event.</param>
        /// <returns>Pointer to the next event.</returns>
        /// <exception cref="InvalidOperationException">There are no more events in the given buffer.</exception>
        internal static unsafe InputEvent* GetNextInMemoryChecked(InputEvent* currentPtr, ref InputEventBuffer buffer)
        {
            Debug.Assert(currentPtr != null);
            Debug.Assert(buffer.Contains(currentPtr), "Given event is not contained in given event buffer");

            var alignedSizeInBytes = NumberHelpers.AlignToMultiple(currentPtr->sizeInBytes, kAlignment);
            var nextPtr = (InputEvent*)((byte*)currentPtr + alignedSizeInBytes);

            if (!buffer.Contains(nextPtr))
                throw new InvalidOperationException(
                    $"Event '{new InputEventPtr(currentPtr)}' is last event in given buffer with size {buffer.sizeInBytes}");

            return nextPtr;
        }
    }
}
