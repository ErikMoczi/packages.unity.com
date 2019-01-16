using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Device command to instruct the underlying platform to pair a user account to the targeted device.
    /// </summary>
    /// <remarks>
    ///
    /// If successful, the platform should then send an <see cref="DeviceConfigurationEvent"/>
    /// to signal that the device configuration has been changed. In response, a <see cref="QueryUserIdCommand"/>
    /// may be sent to fetch the paired user ID from the device.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct InitiateUserAccountPairingCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('P', 'A', 'I', 'R'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public enum Result : long
        {
            /// <summary>
            /// User pairing UI has been successfully opened.
            /// </summary>
            SuccessfullyInitiated = 1,

            /// <summary>
            /// System does not support application-invoked user pairing.
            /// </summary>
            ErrorNotSupported = InputDeviceCommand.kGenericFailure,

            /// <summary>
            /// There already is a pairing operation in progress and the system does not support
            /// pairing multiple devices at the same time.
            /// </summary>
            ErrorAlreadyInProgress = -2,
        }

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static InitiateUserAccountPairingCommand Create()
        {
            return new InitiateUserAccountPairingCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
