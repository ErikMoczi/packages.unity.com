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
        /// <summary>
        /// For internal use. Allows a camera provider to register for the IsPermissionGranted extension
        /// </summary>
        /// <param name="subsystemId">The string name associated with the camera provider to extend.</param>
        /// <param name="handler">A method that returns true if permission is granted.</param>
        public static void RegisterIsPermissionGrantedHandler(string subsystemId, Func<XRCameraSubsystem, bool> handler)
        {
            s_IsPermissionGrantedDelegates[subsystemId] = handler;
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
    }
}
