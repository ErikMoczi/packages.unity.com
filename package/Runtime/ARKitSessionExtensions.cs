using System;
using UnityEngine.XR.ARExtensions;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// For internal use. Provides ARKit-specific extensions to the XRSessionSubsystem.
    /// </summary>
    internal static class ARKitSessionExtension
    {
        static readonly string k_SubsystemId = "ARKit-Session";

        /// <summary>
        /// For internal use. Use <c>XRSessionSubsystem.GetAvailabilityAsync</c> instead.
        /// </summary>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> which this method extends.</param>
        /// <param name="callback">A callback to invoke when the availability has been determined.</param>
        public static Promise<SessionAvailability> GetAvailabilityAsync(XRSessionSubsystem sessionSubsystem)
        {
            var result = Api.UnityARKit_CheckAvailability();
            var retVal = SessionAvailability.None;
            if (result == Api.Availability.Supported)
                retVal = SessionAvailability.Installed | SessionAvailability.Supported;

            return Promise<SessionAvailability>.CreateResolvedPromise(retVal);
        }

        /// <summary>
        /// For internal use. Use <c>XRSessionSubsystem.GetNativePtr</c> instead.
        /// </summary>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> which this method extends.</param>
        /// <returns>An <c>IntPtr</c> associated with the <paramref name="sessionSubsystem"/>.</returns>
        public static IntPtr GetNativePtr(XRSessionSubsystem sessionSubsystem)
        {
            return Api.UnityARKit_getNativeSessionPtr();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRSessionExtensions.RegisterGetAvailabilityAsyncHandler(k_SubsystemId, GetAvailabilityAsync);
            XRSessionExtensions.RegisterGetNativePtrHandler(k_SubsystemId, GetNativePtr);
        }
    }
}
