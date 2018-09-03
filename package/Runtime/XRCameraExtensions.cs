using System;
using System.Collections.Generic;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARExtensions
{
    /// <summary>
    /// Provides extensions to the <c>XRCameraSubsystem</c>.
    /// </summary>
    public static class XRCameraExtensions
    {
        public delegate bool TryGetColorCorrectionDelegate(XRCameraSubsystem cameraSubsystems, out Color color);

        /// <summary>
        /// For internal use. Allows a camera provider to register for the IsPermissionGranted extension.
        /// </summary>
        /// <param name="subsystemId">The string name associated with the camera provider to extend.</param>
        /// <param name="handler">A method that returns true if permission is granted.</param>
        public static void RegisterIsPermissionGrantedHandler(string subsystemId, Func<XRCameraSubsystem, bool> handler)
        {
            s_IsPermissionGrantedDelegates[subsystemId] = handler;
        }

        /// <summary>
        /// For internal use. Allows a camera provider to register for the TryGetColorCorrection extension.
        /// </summary>
        /// <param name="subsystemId">The string name associated with the camera provider to extend.</param>
        /// <param name="handler">A method that returns true if color correction is available.</param>
        public static void RegisterTryGetColorCorrectionHandler(string subsystemId, TryGetColorCorrectionDelegate handler)
        {
            s_TryGetColorCorrectionDelegates[subsystemId] = handler;
        }

        /// <summary>
        /// Attempts to retrieve color correction data for the extended <c>XRCameraSubsystem</c>.
        /// The color correction data represents the scaling factors used for color correction.
        /// The RGB scale factors are used to match the color of the light
        /// in the scene. The alpha channel value is platform-specific.
        /// </summary>
        /// <param name="cameraSubsystem">The <c>XRCameraSubsystem</c> being extended.</param>
        /// <param name="color">The <c>Color</c> representing the color correction value.</param>
        /// <returns><c>True</c> if the data is available, otherwise <c>False</c>.</returns>
        public static bool TryGetColorCorrection(this XRCameraSubsystem cameraSubsystem, out Color color)
        {
            if (cameraSubsystem == null)
                throw new ArgumentNullException("cameraSubsystem");

            TryGetColorCorrectionDelegate handler;
            if (s_TryGetColorCorrectionDelegates.TryGetValue(cameraSubsystem.SubsystemDescriptor.id, out handler))
            {
                return handler(cameraSubsystem, out color);
            }
            else
            {
                color = default(Color);
                return false;
            }
        }

        /// <summary>
        /// Allows you to determine whether camera permission has been granted.
        /// </summary>
        /// <param name="cameraSubsystem">The <c>XRCameraSubsystem</c> being extended.</param>
        /// <returns>True if camera permission has been granted for this app, false otherwise.</returns>
        public static bool IsPermissionGranted(this XRCameraSubsystem cameraSubsystem)
        {
            if (cameraSubsystem == null)
                throw new ArgumentNullException("cameraSubsystem");

            Func<XRCameraSubsystem, bool> handler;
            if (s_IsPermissionGrantedDelegates.TryGetValue(cameraSubsystem.SubsystemDescriptor.id, out handler))
            {
                return handler(cameraSubsystem);
            }
            else
            {
                return true;
            }
        }

        static Dictionary<string, Func<XRCameraSubsystem, bool>> s_IsPermissionGrantedDelegates =
            new Dictionary<string, Func<XRCameraSubsystem, bool>>();

        static Dictionary<string, TryGetColorCorrectionDelegate> s_TryGetColorCorrectionDelegates =
            new Dictionary<string, TryGetColorCorrectionDelegate>();
    }
}
