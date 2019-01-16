using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Indicates what type of change related to an input device occurred.
    /// </summary>
    /// <seealso cref="InputSystem.onDeviceChange"/>
    public enum InputDeviceChange
    {
        /// <summary>
        /// A new device was added to the system.
        /// </summary>
        /// <seealso cref="InputSystem.AddDevice(string,string)"/>
        Added,

        /// <summary>
        /// An existing device was removed from the system.
        /// </summary>
        /// <remarks>
        /// Other than when a device is removed programmatically, this happens when a device
        /// is unplugged from the system. Subsequent to the notification, the system will remove
        /// the <see cref="InputDevice"/> instance from its list and remove the device's
        /// recorded input state.
        /// </remarks>
        /// <seealso cref="InputSystem.RemoveDevice"/>
        Removed,

        Enabled,

        Disabled,

        /// <summary>
        /// The usages on a device have changed.
        /// </summary>
        /// <remarks>
        /// This may signal, for example, that what was the right hand XR controller before
        /// is now the left hand controller.
        /// </remarks>
        /// <seealso cref="InputSystem.SetUsage(InputDevice,InternedString)"/>
        /// <seealso cref="InputControl.usages"/>
        UsageChanged,

        VariantChanged,

        /// <summary>
        /// The configuration of a device has changed.
        /// </summary>
        /// <remarks>
        /// This may signal, for example, that the layout used by the keyboard has changed or
        /// that, on a console, a gamepad has changed which player ID(s) it is assigned to.
        /// </remarks>
        /// <seealso cref="DeviceConfigurationEvent"/>
        /// <seealso cref="InputSystem.QueueConfigChangeEvent"/>
        ConfigurationChanged,

        ////REVIEW: it doesn't seem smart to deliver this high-frequency change on the same path
        ////        as the other low-frequency changes
        StateChanged,

        ////REVIEW: should 'current' be renamed to 'lastActive'?

        CurrentChanged
    }
}
