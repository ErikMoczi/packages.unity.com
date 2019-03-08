#if UNITY_ANDROID && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;

namespace UnityEngine.XR.ARCore
{
    /// <summary>
    /// ARCore implementation of the <c>XRRaycastSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARCoreRaycastSubsystem : XRRaycastSubsystem
    {
        protected override IProvider CreateProvider()
        {
            return new Provider();
        }

        class Provider : IProvider
        {
            public override unsafe NativeArray<XRRaycastHit> Raycast(
                XRRaycastHit defaultRaycastHit,
                Ray ray,
                TrackableType trackableTypeMask,
                Allocator allocator)
            {
                void* hitBuffer;
                int hitCount, elementSize;

                UnityARCore_raycast_acquireHitResultsRay(
                    ray.origin,
                    ray.direction,
                    trackableTypeMask,
                    out hitBuffer,
                    out hitCount,
                    out elementSize);

                try
                {
                    return NativeCopyUtility.PtrToNativeArrayWithDefault<XRRaycastHit>(
                        defaultRaycastHit,
                        hitBuffer, elementSize,
                        hitCount, allocator);
                }
                finally
                {
                    UnityARCore_raycast_releaseHitResults(hitBuffer);
                }
            }

            public override unsafe NativeArray<XRRaycastHit> Raycast(
                XRRaycastHit defaultRaycastHit,
                Vector2 screenPoint,
                TrackableType trackableTypeMask,
                Allocator allocator)
            {
                void* hitBuffer;
                int hitCount, elementSize;

                UnityARCore_raycast_acquireHitResults(
                    screenPoint,
                    trackableTypeMask,
                    out hitBuffer,
                    out hitCount,
                    out elementSize);

                try
                {
                    return NativeCopyUtility.PtrToNativeArrayWithDefault<XRRaycastHit>(
                        defaultRaycastHit,
                        hitBuffer, elementSize,
                        hitCount, allocator);
                }
                finally
                {
                    UnityARCore_raycast_releaseHitResults(hitBuffer);
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport("UnityARCore")]
            static unsafe extern void UnityARCore_raycast_acquireHitResults(
                Vector2 screenPoint,
                TrackableType filter,
                out void* hitBuffer,
                out int hitCount,
                out int elementSize);

            [DllImport("UnityARCore")]
            static unsafe extern void UnityARCore_raycast_acquireHitResultsRay(
                Vector3 rayOrigin,
                Vector3 rayDirection,
                TrackableType filter,
                out void* hitBuffer,
                out int hitCount,
                out int elementSize);

            [DllImport("UnityARCore")]
            static unsafe extern void UnityARCore_raycast_releaseHitResults(
                void* buffer);
#else
            static unsafe void UnityARCore_raycast_acquireHitResults(
                Vector2 screenPoint,
                TrackableType filter,
                out void* hitBuffer,
                out int hitCount,
                out int elementSize)
            {
                hitBuffer = null;
                hitCount = elementSize = 0;
            }

            static unsafe void UnityARCore_raycast_acquireHitResultsRay(
                Vector3 rayOrigin,
                Vector3 rayDirection,
                TrackableType filter,
                out void* hitBuffer,
                out int hitCount,
                out int elementSize)
            {
                hitBuffer = null;
                hitCount = elementSize = 0;
            }

            static unsafe void UnityARCore_raycast_releaseHitResults(
                void* buffer)
            { }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterDescriptor()
        {
            XRRaycastSubsystemDescriptor.RegisterDescriptor(new XRRaycastSubsystemDescriptor.Cinfo
            {
                id = "ARCore-Raycast",
                subsystemImplementationType = typeof(ARCoreRaycastSubsystem),
                supportsViewportBasedRaycast = true,
                supportsWorldBasedRaycast = true,
                supportedTrackableTypes =
                    (TrackableType.Planes & ~TrackableType.PlaneWithinInfinity) |
                    TrackableType.FeaturePoint
            });
        }
    }
}
