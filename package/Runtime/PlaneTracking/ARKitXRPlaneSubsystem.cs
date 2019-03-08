#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// The ARKit implementation of the <c>XRPlaneSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARKitXRPlaneSubsystem : XRPlaneSubsystem
    {
        protected override IProvider CreateProvider()
        {
            return new Provider();
        }

        class Provider : IProvider
        {
            public override void Destroy()
            {
                UnityARKit_planes_shutdown();
            }

            public override void Start()
            {
                UnityARKit_planes_start();
            }

            public override void Stop()
            {
                UnityARKit_planes_stop();
            }

            public override unsafe NativeArray<Vector2> GetBoundary(
                TrackableId trackableId,
                Allocator allocator)
            {
                int numPoints;
                var context = UnityARKit_planes_acquireBoundary(
                    trackableId,
                    out numPoints);

                var boundary = new NativeArray<Vector2>(numPoints, allocator);
                boundary.CopyFrom(
                    NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector2>(
                        context, numPoints, Allocator.None));

                UnityARKit_planes_releaseBoundary(context);

                return boundary;
            }

            public override unsafe TrackableChanges<BoundedPlane> GetChanges(
                BoundedPlane defaultPlane,
                Allocator allocator)
            {
                int addedLength, updatedLength, removedLength, elementSize;
                void* addedArrayPtr, updatedArrayPtr, removedArrayPtr;
                var context = UnityARKit_planes_acquireChanges(
                    out addedArrayPtr, out addedLength,
                    out updatedArrayPtr, out updatedLength,
                    out removedArrayPtr, out removedLength,
                    out elementSize);

                try
                {
                    return new TrackableChanges<BoundedPlane>(
                        addedArrayPtr, addedLength,
                        updatedArrayPtr, updatedLength,
                        removedArrayPtr, removedLength,
                        defaultPlane, elementSize,
                        allocator);
                }
                finally
                {
                    UnityARKit_planes_releaseChanges(context);
                }
            }

            public override PlaneDetectionMode planeDetectionMode
            {
                set
                {
                    UnityARKit_planes_setPlaneDetectionMode(value);
                }
            }

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
            static extern void UnityARKit_planes_shutdown();

            [DllImport("__Internal")]
            static extern void UnityARKit_planes_start();

            [DllImport("__Internal")]
            static extern void UnityARKit_planes_stop();

            [DllImport("__Internal")]
            static extern unsafe void* UnityARKit_planes_acquireChanges(
                out void* addedPtr, out int addedLength,
                out void* updatedPtr, out int updatedLength,
                out void* removedPtr, out int removedLength,
                out int elementSize);

            [DllImport("__Internal")]
            static extern unsafe void UnityARKit_planes_releaseChanges(void* changes);

            [DllImport("__Internal")]
            static extern void UnityARKit_planes_setPlaneDetectionMode(PlaneDetectionMode mode);

            [DllImport("__Internal")]
            static extern unsafe void* UnityARKit_planes_acquireBoundary(
                TrackableId trackableId,
                out int numPoints);

            [DllImport("__Internal")]
            static extern unsafe void UnityARKit_planes_releaseBoundary(
                void* boundary);
#else
            static void UnityARKit_planes_shutdown()
            { }

            static void UnityARKit_planes_start()
            { }

            static void UnityARKit_planes_stop()
            { }

            static unsafe void* UnityARKit_planes_acquireChanges(
                out void* addedPtr, out int addedLength,
                out void* updatedPtr, out int updatedLength,
                out void* removedPtr, out int removedLength,
                out int elementSize)
            {
                addedPtr = updatedPtr = removedPtr = null;
                addedLength = updatedLength = removedLength = elementSize = 0;
                return null;
            }

            static unsafe void UnityARKit_planes_releaseChanges(void* changes)
            { }

            static void UnityARKit_planes_setPlaneDetectionMode(PlaneDetectionMode mode)
            { }

            static unsafe void* UnityARKit_planes_acquireBoundary(
                TrackableId trackableId,
                out int numPoints)
            {
                numPoints = 0;
                return null;
            }

            static unsafe void UnityARKit_planes_releaseBoundary(
                void* boundary)
            { }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterDescriptor()
        {
            var cinfo = new XRPlaneSubsystemDescriptor.Cinfo
            {
                id = "ARKit-Plane",
                subsystemImplementationType = typeof(ARKitXRPlaneSubsystem),
                supportsHorizontalPlaneDetection = true,
                supportsVerticalPlaneDetection = true,
                supportsArbitraryPlaneDetection = false,
                supportsBoundaryVertices = true
            };

            XRPlaneSubsystemDescriptor.Create(cinfo);
        }
    }
}
