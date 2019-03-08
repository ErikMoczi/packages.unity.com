using System;
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
    /// The ARCore implementation of the <c>XRDepthSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARCoreXRDepthSubsystem : XRDepthSubsystem
    {
        class Provider : IDepthApi
        {
            static class NativeApi
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                [DllImport("UnityARCore")]
                static extern public void UnityARCore_depth_Initialize();

                [DllImport("UnityARCore")]
                static extern public void UnityARCore_depth_Shutdown();

                [DllImport("UnityARCore")]
                static extern public void UnityARCore_depth_Start(Guid guid);

                [DllImport("UnityARCore")]
                static extern public void UnityARCore_depth_Stop();

                [DllImport("UnityARCore")]
                static extern unsafe public void* UnityARCore_depth_AcquireChanges(
                    out void* addedPtr, out int addedLength,
                    out void* updatedPtr, out int updatedLength,
                    out void* removedPtr, out int removedLength,
                    out int elementSize);

                [DllImport("UnityARCore")]
                static extern unsafe public void UnityARCore_depth_ReleaseChanges(void* changes);

                [DllImport("UnityARCore")]
                static extern unsafe public void* UnityARCore_depth_AcquireFeaturePointPositions(
                    TrackableId trackableId, out int numPoints);

                [DllImport("UnityARCore")]
                static extern unsafe public void UnityARCore_depth_ReleaseFeaturePointPositions(
                    void* positions);

                [DllImport("UnityARCore")]
                static extern unsafe public void* UnityARCore_depth_AcquireFeaturePointIds(
                    TrackableId trackableId, out int numIds);

                [DllImport("UnityARCore")]
                static extern unsafe public void UnityARCore_depth_ReleaseFeaturePointIds(void* ids);

                [DllImport("UnityARCore")]
                static extern unsafe public void* UnityARCore_depth_AcquireFeaturePointConfidence(
                    TrackableId trackableId, out int numConfidence);

                [DllImport("UnityARCore")]
                static extern unsafe public void UnityARCore_depth_ReleaseFeaturePointConfidence(void* confidence);
#else
                static public void UnityARCore_depth_Initialize() {}

                static public void UnityARCore_depth_Shutdown() {}

                static public void UnityARCore_depth_Start(Guid guid) {}

                static public void UnityARCore_depth_Stop() {}

                static unsafe public void* UnityARCore_depth_AcquireChanges(
                    out void* addedPtr, out int addedLength,
                    out void* updatedPtr, out int updatedLength,
                    out void* removedPtr, out int removedLength,
                    out int elementSize) 
                {
                    addedPtr = updatedPtr = removedPtr = null;
                    addedLength = updatedLength = removedLength = elementSize = 0;
                    return null;
                }

                static public unsafe void UnityARCore_depth_ReleaseChanges(void* changes) {}

                static public unsafe void* UnityARCore_depth_AcquireFeaturePointPositions(
                    TrackableId trackableId,
                    out int numPoints)
                {
                    numPoints = 0;
                    return null;
                }
                static public unsafe void UnityARCore_depth_ReleaseFeaturePointPositions(
                    void* positions) {}

                static public unsafe void* UnityARCore_depth_AcquireFeaturePointIds(
                    TrackableId trackableId, out int numIds)
                {
                    numIds = 0;
                    return null;
                }

                static public unsafe void UnityARCore_depth_ReleaseFeaturePointIds(void* ids) {}

                static public unsafe void* UnityARCore_depth_AcquireFeaturePointConfidence(
                    TrackableId trackableId, out int numConfidence)
                {
                    numConfidence = 0;
                    return null;
                }

                static public unsafe void UnityARCore_depth_ReleaseFeaturePointConfidence(void* confidence) {}
#endif
            }

            public override unsafe TrackableChanges<XRPointCloud> GetChanges(
                XRPointCloud defaultPointCloud,
                Allocator allocator)
            {
                void* addedPtr, updatedPtr, removedPtr;
                int addedLength, updatedLength, removedLength, elementSize;

                var context = NativeApi.UnityARCore_depth_AcquireChanges(
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
                    NativeApi.UnityARCore_depth_ReleaseChanges(context);
                }
            }

            public override unsafe NativeArray<Vector3> GetFeaturePointPositions(
                TrackableId trackableId, Allocator allocator)
            {
                int numPoints;
                var context = NativeApi.UnityARCore_depth_AcquireFeaturePointPositions(trackableId, out numPoints);

                var positions = new NativeArray<Vector3>(numPoints, allocator);

                positions.CopyFrom(
                    NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(
                        context, numPoints, Allocator.None));

                NativeApi.UnityARCore_depth_ReleaseFeaturePointPositions(context);

                return positions;
            }

            public override unsafe NativeArray<ulong> GetFeaturePointIds(
                TrackableId trackableId, Allocator allocator)
            {
                int numPoints;
                var context = NativeApi.UnityARCore_depth_AcquireFeaturePointIds(trackableId, out numPoints);
                var ids = new NativeArray<ulong>(numPoints, allocator);

                ids.CopyFrom(
                    NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ulong>(
                        context, numPoints, Allocator.None));

                NativeApi.UnityARCore_depth_ReleaseFeaturePointPositions(context);

                return ids;
            }

            public override unsafe NativeArray<float> GetFeaturePointConfidence(
                TrackableId trackableId, Allocator allocator)
            {
                int numPoints;
                var context = NativeApi.UnityARCore_depth_AcquireFeaturePointConfidence(trackableId, out numPoints);
                var confidence = new NativeArray<float>(numPoints, allocator);

                confidence.CopyFrom(
                    NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(
                        context, numPoints, Allocator.None));

                NativeApi.UnityARCore_depth_ReleaseFeaturePointPositions(context);

                return confidence;
            }

            public override void Destroy()
            { }

            /// <summary>
            /// Starts the DepthSubsystem provider to begin providing face data via the callback delegates
            /// </summary>
            public override void Start()
            {
                NativeApi.UnityARCore_depth_Start(Guid.NewGuid());
            }

            /// <summary>
            /// Stops the DepthSubsystem provider from providing face data
            /// </summary>
            public override void Stop()
            {
                NativeApi.UnityARCore_depth_Stop();
            }
        }

        protected override IDepthApi GetInterface()
        {
            return new Provider();
        }

        // this method is run on startup of the app to register this provider with XR Subsystem Manager
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterDescriptor()
        {
            var descriptorParams = new XRDepthSubsystemDescriptor.Cinfo
            {
                id = "ARCore-Depth",
                implementationType = typeof(ARCoreXRDepthSubsystem),
                supportsFeaturePoints = true,
                supportsUniqueIds = true,
                supportsConfidence = true
            };

            XRDepthSubsystemDescriptor.RegisterDescriptor(descriptorParams);
        }
    }
}