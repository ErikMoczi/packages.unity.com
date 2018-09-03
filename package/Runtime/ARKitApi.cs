using System.Runtime.InteropServices;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARKit
{
    internal static class Api
    {
        // Should match ARKitAvailability in ARKitXRSessionProvider.mm
        public enum Availability
        {
            None,
            Supported
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        internal static extern TrackableId UnityARKit_attachReferencePoint(TrackableId trackableId, Pose pose);

        [DllImport("__Internal")]
        internal static extern Availability UnityARKit_CheckAvailability();

        [DllImport("__Internal")]
        internal static extern bool UnityARKit_IsCameraPermissionGranted();
#else
        internal static Availability UnityARKit_CheckAvailability() { return Availability.None; }

        internal static TrackableId UnityARKit_attachReferencePoint(TrackableId trackableId, Pose pose)
        {
            return TrackableId.InvalidId;
        }

        internal static bool UnityARKit_IsCameraPermissionGranted() { return false; }
#endif
    }
}
