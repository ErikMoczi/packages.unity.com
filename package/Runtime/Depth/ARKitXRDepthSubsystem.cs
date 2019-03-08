using System;
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
    /// The ARKit implementation of the <c>XRDepthSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARKitXRDepthSubsystem : XRDepthSubsystem
    {
        class Provider : IDepthApi
        {
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
            static extern void UnityARKit_depth_destroy();

            [DllImport("__Internal")]
            static extern void UnityARKit_depth_start();

            [DllImport("__Internal")]
            static extern void UnityARKit_depth_stop();

            [DllImport("__Internal")]
            static extern unsafe void* UnityARKit_depth_acquireChanges(
                out void* addedPtr, out int addedLength,
                out void* updatedPtr, out int updatedLength,
                out void* removedPtr, out int removedLength,
                out int elementSize);

            [DllImport("__Internal")]
            static extern unsafe void UnityARKit_depth_releaseChanges(
                void* changes);

            [DllImport("__Internal")]
            static extern unsafe void* UnityARKit_depth_acquirePoints(
                TrackableId trackableId,
                out int numPoints);

            [DllImport("__Internal")]
            static extern unsafe void UnityARKit_depth_releasePoints(
                void* points);

            [DllImport("__Internal")]
            static extern unsafe void* UnityARKit_depth_acquireIds(
                TrackableId trackableId,
                out int numPoints);

            [DllImport("__Internal")]
            static extern unsafe void UnityARKit_depth_releaseIds(
                void* ids);
#else
            static void UnityARKit_depth_destroy()
            { }

            static void UnityARKit_depth_start()
            { }

            static void UnityARKit_depth_stop()
            { }

            static unsafe void* UnityARKit_depth_acquireChanges(
                out void* addedPtr, out int addedLength,
                out void* updatedPtr, out int updatedLength,
                out void* removedPtr, out int removedLength,
                out int elementSize)
            {
                addedPtr = updatedPtr = removedPtr = null;
                addedLength = updatedLength = removedLength = elementSize = 0;
                return null;
            }

            static unsafe void UnityARKit_depth_releaseChanges(
                void* changes)
            { }

            static unsafe void* UnityARKit_depth_acquirePoints(
                TrackableId trackableId,
                out int numPoints)
            {
                numPoints = 0;
                return null;
            }

            static unsafe void UnityARKit_depth_releasePoints(
                void* points)
            { }

            static unsafe void* UnityARKit_depth_acquireIds(
                TrackableId trackableId,
                out int numPoints)
            {
                numPoints = 0;
                return null;
            }

            static unsafe void UnityARKit_depth_releaseIds(
                void* ids)
            { }
#endif

            public override unsafe TrackableChanges<XRPointCloud> GetChanges(
                XRPointCloud defaultPointCloud,
                Allocator allocator)
            {
                int addedLength, updatedLength, removedLength, elementSize;
                void* addedPtr, updatedPtr, removedPtr;

                var context = UnityARKit_depth_acquireChanges(
                    out addedPtr, out addedLength,
                    out updatedPtr, out updatedLength,
                    out removedPtr, out removedLength,
                    out elementSize);

                try
                {
                    return new TrackableChanges<XRPointCloud>(
                        addedPtr, addedLength,
                        updatedPtr, updatedLength,
                        removedPtr, removedLength,
                        defaultPointCloud, elementSize,
                        allocator);
                }
                finally
                {
                    UnityARKit_depth_releaseChanges(context);
                }
            }

            public override void Destroy()
            {
                UnityARKit_depth_destroy();
            }

            public override void Start()
            {
                UnityARKit_depth_start();
            }

            public override void Stop()
            {
                UnityARKit_depth_stop();
            }

            public override unsafe NativeArray<Vector3> GetFeaturePointPositions(
                TrackableId trackableId,
                Allocator allocator)
            {
                int numPoints;
                var pointsPtr = UnityARKit_depth_acquirePoints(trackableId, out numPoints);
                try
                {
                    var points = new NativeArray<Vector3>(numPoints, allocator);
                    points.CopyFrom(
                        NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(
                            pointsPtr, numPoints, Allocator.None));

                    return points;
                }
                finally
                {
                    UnityARKit_depth_releasePoints(pointsPtr);
                }
            }

            public override unsafe NativeArray<ulong> GetFeaturePointIds(
                TrackableId trackableId,
                Allocator allocator)
            {
                int count;
                var idPtr = UnityARKit_depth_acquireIds(trackableId, out count);
                try
                {
                    var ids = new NativeArray<ulong>(count, allocator);
                    ids.CopyFrom(
                        NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ulong>(
                            idPtr, count, Allocator.None));
                    return ids;
                }
                finally
                {
                    UnityARKit_depth_releaseIds(idPtr);
                }
            }

            public override NativeArray<float> GetFeaturePointConfidence(
                TrackableId trackableId,
                Allocator allocator)
            {
                return new NativeArray<float>(0, allocator);
            }
        }

        protected override IDepthApi GetInterface()
        {
            return new Provider();
        }

        //this method is run on startup of the app to register this provider with XR Subsystem Manager
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterDescriptor()
        {
            var descriptorParams = new XRDepthSubsystemDescriptor.Cinfo
            {
                id = "ARKit-Depth",
                implementationType = typeof(ARKitXRDepthSubsystem),
                supportsFeaturePoints = true,
                supportsConfidence = false,
                supportsUniqueIds = true
            };

            XRDepthSubsystemDescriptor.RegisterDescriptor(descriptorParams);
        }
    }
}
