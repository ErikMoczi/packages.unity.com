using System;
using Unity.Collections.LowLevel.Unsafe;

////TODO: add API to send events in bulk rather than one by one

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Input functions that have to be performed by the underlying input runtime.
    /// </summary>
    public unsafe interface IInputRuntime
    {
        /// <summary>
        /// Allocate a new unique device ID.
        /// </summary>
        /// <returns>A numeric device ID that is not <see cref="InputDevice.kInvalidDeviceId"/>.</returns>
        /// <remarks>
        /// Device IDs are managed by the runtime. This method allows creating devices that
        /// can use the same ID system but are not known to the underlying runtime.
        /// </remarks>
        int AllocateDeviceId();

        /// <summary>
        /// Manually trigger an update.
        /// </summary>
        /// <param name="type">Type of update to run. If this is a combination of updates, each flag
        /// that is set in the mask will run a separate update.</param>
        /// <remarks>
        /// Updates will flush out events and trigger <see cref="onBeforeUpdate"/> and <see cref="onUpdate"/>.
        /// Also, newly discovered devices will be reported by an update is run.
        /// </remarks>
        void Update(InputUpdateType type);

        /// <summary>
        /// Queue an input event.
        /// </summary>
        /// <remarks>
        /// This method has to be thread-safe.
        /// </remarks>
        /// <param name="ptr">Pointer to the event data. Uses the <see cref="InputEvent"/> format.</param>
        /// <remarks>
        /// Events are copied into an internal buffer. Thus the memory referenced by this method does
        /// not have to persist until the event is processed.
        /// </remarks>
        void QueueEvent(IntPtr ptr);

        //NOTE: This method takes an IntPtr instead of a generic ref type parameter (like InputDevice.ExecuteCommand)
        //      to avoid issues with AOT where generic interface methods can lead to problems. Il2cpp can handle it here
        //      just fine but Mono will run into issues.
        /// <summary>
        /// Perform an I/O transaction directly against a specific device.
        /// </summary>
        /// <remarks>
        /// This function is used to set up device-specific communication controls between
        /// a device and the user of a device. The interface does not dictate a set of supported
        /// IOCTL control codes.
        /// </remarks>
        /// <param name="deviceId">Device to send the command to.</param>
        /// <param name="commandPtr">Pointer to the command buffer.</param>
        /// <returns>Negative value on failure, >=0 on success. Meaning of return values depends on the
        /// command sent to the device.</returns>
        long DeviceCommand(int deviceId, InputDeviceCommand* commandPtr);

        /// <summary>
        /// Set delegate to be called on input updates.
        /// </summary>
        Action<InputUpdateType, int, IntPtr> onUpdate { set; }

        /// <summary>
        /// Set delegate to be called right before <see cref="onUpdate"/>.
        /// </summary>
        /// <remarks>
        /// This delegate is meant to allow events to be queued that should be processed right
        /// in the upcoming update.
        /// </remarks>
        Action<InputUpdateType> onBeforeUpdate { set; }

        /// <summary>
        /// Set delegate to be called when a new device is discovered.
        /// </summary>
        /// <remarks>
        /// The runtime should delay reporting of already present devices until the delegate
        /// has been put in place and then call the delegate for every device already in the system.
        ///
        /// First parameter is the ID assigned to the device, second parameter is a description
        /// in JSON format of the device (see <see cref="InputDeviceDescription.FromJson"/>).
        /// </remarks>
        Action<int, string> onDeviceDiscovered { set; }

        /// <summary>
        /// Set the background polling frequency for devices that have to be polled.
        /// </summary>
        float pollingFrequency { set; }

        InputUpdateType updateMask { set; }
    }

    internal static class InputRuntime
    {
        public static IInputRuntime s_Instance;
    }

    public static class InputRuntimeExtensions
    {
        public static unsafe long DeviceCommand<TCommand>(this IInputRuntime runtime, int deviceId, ref TCommand command)
            where TCommand : struct, IInputDeviceCommandInfo
        {
            return runtime.DeviceCommand(deviceId, (InputDeviceCommand*)UnsafeUtility.AddressOf(ref command));
        }
    }
}
