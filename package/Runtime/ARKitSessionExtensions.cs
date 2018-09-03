using System.Runtime.InteropServices;
using UnityEngine.XR.ARExtensions;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// For internal use. Provides ARKit-specific extensions to the XRSessionSubsystem.
    /// </summary>
    internal class ARKitSessionExtension
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRSessionExtensions.RegisterGetAvailabilityAsyncHandler(k_SubsystemId, GetAvailabilityAsync);
        }
    }

    internal static class Api
    {
        // Should match ARKitAvailability in ARKitXRSessionProvider.mm
        public enum Availability
        {
            None,
            Supported
        }

#if UNITY_IOS
        [DllImport("__Internal")]
        public static extern Availability UnityARKit_CheckAvailability();
#else
        public static Availability UnityARKit_CheckAvailability() { return Availability.None; }
#endif
    }
}
