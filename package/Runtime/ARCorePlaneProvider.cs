#if UNITY_ANDROID && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARCore
{
    /// <summary>
    /// The ARCore implementation of the <c>XRPlaneSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARCorePlaneProvider : XRPlaneSubsystem
    {
        protected override IProvider CreateProvider()
        {
            return new Provider();
        }

        class Provider : IProvider
        {
            public override void Start()
            {
                UnityARCore_planeTracking_startTracking();
            }

            public override void Stop()
            {
                UnityARCore_planeTracking_stopTracking();
            }

            public override unsafe NativeArray<Vector2> GetBoundary(
                TrackableId trackableId, Allocator allocator)
            {
                int numPoints;
                var context = UnityARCore_planeTracking_acquireBoundary(
                    trackableId,
                    out numPoints);

                var boundary = new NativeArray<Vector2>(numPoints, allocator);
                boundary.CopyFrom(
                    NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector2>(
                        context, numPoints, Allocator.None));

                UnityARCore_planeTracking_releaseBoundary(
                    context);

                return boundary;
            }

            public override unsafe TrackableChanges<BoundedPlane> GetChanges(
                BoundedPlane defaultPlane,
                Allocator allocator)
            {
                int addedLength, updatedLength, removedLength, elementSize;
                void* addedPtr, updatedPtr, removedPtr;
                var context = UnityARCore_planeTracking_acquireChanges(
                    out addedPtr, out addedLength,
                    out updatedPtr, out updatedLength,
                    out removedPtr, out removedLength,
                    out elementSize);

                try
                {
                    return new TrackableChanges<BoundedPlane>(
                        addedPtr, addedLength,
                        updatedPtr, updatedLength,
                        removedPtr, removedLength,
                        defaultPlane, elementSize,
                        allocator);
                }
                finally
                {
                    UnityARCore_planeTracking_releaseChanges(context);
                }
            }

            public override void Destroy()
            {
                UnityARCore_planeTracking_destroy();
            }

            public override PlaneDetectionMode planeDetectionMode
            {
                set
                {
                    UnityARCore_planeTracking_setPlaneDetectionMode(value);
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport("UnityARCore")]
            static extern void UnityARCore_planeTracking_startTracking();

            [DllImport("UnityARCore")]
            static extern void UnityARCore_planeTracking_stopTracking();

            [DllImport("UnityARCore")]
            static extern unsafe void* UnityARCore_planeTracking_acquireChanges(
                out void* addedPtr, out int addedLength,
                out void* updatedPtr, out int updatedLength,
                out void* removedPtr, out int removedLength,
                out int elementSize);

            [DllImport("UnityARCore")]
            static extern unsafe void UnityARCore_planeTracking_releaseChanges(
                void* changes);

            [DllImport("UnityARCore")]
            static extern unsafe void* UnityARCore_planeTracking_acquireBoundary(
                TrackableId trackableId,
                out int numPoints);

            [DllImport("UnityARCore")]
            static extern unsafe void UnityARCore_planeTracking_releaseBoundary(
                void* boundary);

            [DllImport("UnityARCore")]
            static extern void UnityARCore_planeTracking_setPlaneDetectionMode(
                PlaneDetectionMode mode);

            [DllImport("UnityARCore")]
            static extern void UnityARCore_planeTracking_destroy();
#else
            static void UnityARCore_planeTracking_startTracking()
            { }

            static void UnityARCore_planeTracking_stopTracking()
            { }

            static unsafe void* UnityARCore_planeTracking_acquireChanges(
                out void* addedPtr, out int addedLength,
                out void* updatedPtr, out int updatedLength,
                out void* removedPtr, out int removedLength,
                out int elementSize)
            {
                addedPtr = updatedPtr = removedPtr = null;
                addedLength = updatedLength = removedLength = elementSize = 0;
                return null;
            }

            static unsafe void UnityARCore_planeTracking_releaseChanges(
                void* changes)
            { }

            static unsafe void* UnityARCore_planeTracking_acquireBoundary(
                TrackableId trackableId,
                out int numPoints)
            {
                numPoints = 0;
                return null;
            }

            static unsafe void UnityARCore_planeTracking_releaseBoundary(
                void* boundary)
            { }

            static void UnityARCore_planeTracking_setPlaneDetectionMode(
                PlaneDetectionMode mode)
            { }

            static void UnityARCore_planeTracking_destroy()
            { }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterDescriptor()
        {
            var cinfo = new XRPlaneSubsystemDescriptor.Cinfo
            {
                id = "ARCore-Plane",
                subsystemImplementationType = typeof(ARCorePlaneProvider),
                supportsHorizontalPlaneDetection = true,
                supportsVerticalPlaneDetection = true,
                supportsArbitraryPlaneDetection = false,
                supportsBoundaryVertices = true
            };

            XRPlaneSubsystemDescriptor.Create(cinfo);
        }
    }
}
