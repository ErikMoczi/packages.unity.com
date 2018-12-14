using System.Collections.Generic;
using UnityEngine.Experimental.XR;
using System.Runtime.InteropServices;
using System;
using AOT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.XR.FaceSubsystem;
using UnityEngine.XR.FaceSubsystem.Providing;

namespace UnityEngine.XR.ARKit
{
    [Preserve]
    public class ARKitFaceSubsystem : XRFaceSubsystem
    {
        /// <summary>
        /// calls available to native code, linked via extern C symbols
        /// </summary>
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void UnityARKit_FaceProvider_Initialize();

        [DllImport("__Internal")]
        static extern void UnityARKit_FaceProvider_Shutdown();

        [DllImport("__Internal")]
        static extern void UnityARKit_FaceProvider_Start();

        [DllImport("__Internal")]
        static extern void UnityARKit_FaceProvider_Stop();

        [DllImport("__Internal")]
        static extern void UnityARKit_FaceProvider_SetFaceAnchorCallbacks(DelegateXrFaceAnchorAdded faceAnchorAddedCallback,
            DelegateXrFaceAnchorUpdated faceAnchorUpdatedCallback,
            DelegateXrFaceAnchorRemoved faceAnchorRemovedCallback,
            DelegateXrFaceSessionBeginFrame faceSessionBeginFrameCallback);

        [DllImport("__Internal")]
        static extern bool UnityARKit_FaceProvider_TryGetFaceMeshVertices(TrackableId faceId, out IntPtr ptrVertexData, out int numArrayVertices);

        [DllImport("__Internal")]
        static extern bool UnityARKit_FaceProvider_TryGetFaceMeshUVs(TrackableId faceId, out IntPtr ptrUvData, out int numArrayUVs);

        [DllImport("__Internal")]
        static extern bool UnityARKit_FaceProvider_TryGetFaceMeshIndices(TrackableId faceId, out IntPtr ptrIndexData, out int numArrayIndices);

        [DllImport("__Internal")]
        static extern bool UnityARKit_FaceProvider_TryGetFaceBlendCoefficients(TrackableId faceId, out IntPtr ptrBlendCoefficientData, out int numArrayBlendCoefficients);

        [DllImport("__Internal")]
        static extern bool UnityARKit_FaceProvider_TryGetAllFaces(out IntPtr ptrArrayUnityXrFace, out int numArrayFaces);

        [DllImport("__Internal")]
        static extern bool UnityARKit_FaceProvider_TryRemoveFace(TrackableId faceId);
#else
        static void UnityARKit_FaceProvider_Initialize()
        { }

        static void UnityARKit_FaceProvider_Shutdown()
        { }

        static void UnityARKit_FaceProvider_Start()
        { }

        static void UnityARKit_FaceProvider_Stop()
        { }

        static void UnityARKit_FaceProvider_SetFaceAnchorCallbacks(
            DelegateXrFaceAnchorAdded faceAnchorAddedCallback,
            DelegateXrFaceAnchorUpdated faceAnchorUpdatedCallback,
            DelegateXrFaceAnchorRemoved faceAnchorRemovedCallback,
            DelegateXrFaceSessionBeginFrame faceSessionBeginFrameCallback)
        { }

        static bool UnityARKit_FaceProvider_TryGetFaceMeshVertices(TrackableId faceId, out IntPtr ptrVertexData, out int numArrayVertices)
        {
            ptrVertexData = IntPtr.Zero;
            numArrayVertices = 0;
            return false;
        }

        static bool UnityARKit_FaceProvider_TryGetFaceMeshUVs(TrackableId faceId, out IntPtr ptrUvData, out int numArrayUVs)
        {
            ptrUvData = IntPtr.Zero;
            numArrayUVs = 0;
            return false;
        }

        static bool UnityARKit_FaceProvider_TryGetFaceMeshIndices(TrackableId faceId, out IntPtr ptrIndexData, out int numArrayIndices)
        {
            ptrIndexData = IntPtr.Zero;
            numArrayIndices = 0;
            return false;
        }

        static bool UnityARKit_FaceProvider_TryGetFaceBlendCoefficients(TrackableId faceId, out IntPtr ptrBlendCoefficientData, out int numArrayBlendCoefficients)
        {
            ptrBlendCoefficientData = IntPtr.Zero;
            numArrayBlendCoefficients = 0;
            return false;
        }

        static bool UnityARKit_FaceProvider_TryGetAllFaces(out IntPtr ptrArrayUnityXrFace, out int numArrayFaces)
        {
            ptrArrayUnityXrFace = IntPtr.Zero;
            numArrayFaces = 0;
            return false;
        }

        static bool UnityARKit_FaceProvider_TryRemoveFace(TrackableId faceId)
        {
            return false;
        }
#endif

        /// <summary>
        /// Definition of callback delegates to get face data from provider into this XRFaceSubsystem implementation
        /// </summary>

        static XRFaceSubsystem s_CurrentInstance;

        delegate void DelegateXrFaceAnchorAdded(XRFace anchorData);
        delegate void DelegateXrFaceAnchorUpdated(XRFace anchorData);
        delegate void DelegateXrFaceAnchorRemoved(XRFace anchorData);

        delegate void DelegateXrFaceSessionBeginFrame();

        [MonoPInvokeCallback(typeof(DelegateXrFaceAnchorAdded))]
        static void UnityARKit_face_anchor_added(XRFace anchor)
        {
            InvokeFaceAddedCallback(anchor, s_CurrentInstance);
        }

        [MonoPInvokeCallback(typeof(DelegateXrFaceAnchorUpdated))]
        static void UnityARKit_face_anchor_updated(XRFace anchor)
        {
            InvokeFaceUpdatedCallback(anchor, s_CurrentInstance);
        }

        [MonoPInvokeCallback(typeof(DelegateXrFaceAnchorRemoved))]
        static void UnityARKit_face_anchor_removed(XRFace anchor)
        {
            InvokeFaceRemovedCallback(anchor, s_CurrentInstance);
        }

        [MonoPInvokeCallback(typeof(DelegateXrFaceSessionBeginFrame))]
        static void UnityARKit_face_session_begin_frame()
        {
            OnBeginFrame();
        }

        /// <summary>
        /// Constructs the XRFaceSubsystem implementation and static native arrays and initializes the provider.
        /// </summary>
        public ARKitFaceSubsystem()
        {
            s_CurrentInstance = this;
            UnityARKit_FaceProvider_Initialize();
        }

        /// <summary>
        /// Shutsdown the provider, removes the XRFaceSubsystem implementation and disposes of the memory used by static arrays.
        /// </summary>
        public override void Destroy()
        {
            s_CurrentInstance = null;
            UnityARKit_FaceProvider_Shutdown();
        }

        /// <summary>
        /// Starts the XRFaceSubsystem provider to begin providing face data via the callback delegates
        /// </summary>
        public override void Start()
        {
            UnityARKit_FaceProvider_SetFaceAnchorCallbacks(UnityARKit_face_anchor_added, UnityARKit_face_anchor_updated,
                UnityARKit_face_anchor_removed, UnityARKit_face_session_begin_frame);
            UnityARKit_FaceProvider_Start();
        }

        /// <summary>
        /// Stops the XRFaceSubsystem provider from providing face data
        /// </summary>
        public override void Stop()
        {
            UnityARKit_FaceProvider_Stop();
            UnityARKit_FaceProvider_SetFaceAnchorCallbacks(null, null, null, null);
        }

        /// <inheritdoc />
        /// <summary>
        /// XRFaceSubsystem API method that this provider overrides to provide a list of faces it keeps track of.
        /// </summary>
        /// <param name="facesOut">A list that will contain all the faces that the provider is tracking currently</param>
        /// <exception cref="T:System.ArgumentNullException">Throws an exception if the facesOut parameter is not a valid list.</exception>
        public override bool TryGetAllFaces(List<XRFace> facesOut)
        {
            if (facesOut == null)
                throw new ArgumentNullException("facesOut");

            return TryGetNativeAllFaces(facesOut);
        }

        /// <summary>
        /// FaeSubsystem API method to remove a face from the session.
        /// </summary>
        /// <param name="faceId"> The <see cref="TrackableId"/> of the face you are interested in. </param>
        /// <returns>True if face was removed</returns>
        public override bool TryRemoveFace(TrackableId faceId)
        {
            return TryRemoveNativeFace(faceId);
        }

        /// <summary>
        /// FaceSubsytem API method that this provider overrides to provide a list of vertex positions for a face mesh
        /// </summary>
        /// <param name="faceId"> The <see cref="TrackableId"/> of the face you are interested in.</param>
        /// <param name="verticesOut">Replaces the content with the list of <see cref="Vector3"/> that will contain all the vertex positions of the face mesh.</param>
        /// <exception cref="ArgumentNullException">Throws an exception if the verticesOut parameter is a null reference.</exception>
        /// <returns>True if face mesh vertices were successfully populated.</returns>
        public override bool TryGetFaceMeshVertices(TrackableId faceId, List<Vector3> verticesOut)
        {
            if (verticesOut == null)
                throw new ArgumentNullException("verticesOut");

            return TryGetNativeFaceMeshVertices(faceId, verticesOut);
        }

        /// <summary>
        /// XRFaceSubsystem API method that this provider overrides to provide a list of texture coordinates for a face mesh
        /// </summary>
        /// <param name="faceId"> The <see cref="TrackableId"/> of the face you are interested in.</param>
        /// <param name="uvsOut">Replaces the content with the list of <see cref="Vector2"/> that will contain all the texture coordinates of the face mesh.</param>
        /// <exception cref="ArgumentNullException">Throws an exception if the uvsOut parameter is not a valid list.</exception>
        /// <returns>True if face mesh UVs were successfully populated.</returns>
        public override bool TryGetFaceMeshUVs(TrackableId faceId, List<Vector2> uvsOut)
        {
            if (uvsOut == null)
                throw new ArgumentNullException("uvsOut");

            return TryGetNativeFaceMeshUVs(faceId, uvsOut);
        }

        /// <summary>
        /// XRFaceSubsystem API method that this provider overrides to provide a list of triangle indices for a face mesh
        /// </summary>
        /// <param name="faceId">The <see cref="TrackableId"/> of the face you are interested in.</param>
        /// <param name="indicesOut">Replaces the content with the list of <see cref="Int32"/> that will contain all the triangle indices of the face mesh.</param>
        /// <exception cref="ArgumentNullException">Throws an exception if the indicesOut parameter is not a valid list.</exception>
        /// <returns>True if face mesh triangle indices were successfully populated.</returns>
        public override bool TryGetFaceMeshIndices(TrackableId faceId, List<int> indicesOut)
        {
            if (indicesOut == null)
                throw new ArgumentNullException("indicesOut");

            return TryGetNativeFaceMeshIndices(faceId, indicesOut);
        }

        /// <summary>
        /// Platform specific API method that this provider uses to provide a list of <see cref="XRFaceArkitBlendShapeCoefficient"/> that can be used to render facial expressions using blend shapes.
        /// </summary>
        /// <param name="faceId">The <see cref="TrackableId"/> of the face you are interested in.</param>
        /// <param name="coefficientsOut">Replaces the content with the list of <see cref="XRFaceArkitBlendShapeCoefficient"/> that will contain pairs of <see cref="XRArkitBlendShapeLocation"/> and floats that describe the current facial expression on the face.</param>
        /// <exception cref="ArgumentNullException">Throws an exception if the coefficientsOut parameter is not a valid list.</exception>
        /// <returns>True if face blend shape coefficients were successfully populated.</returns>
        public bool TryGetFaceARKitBlendShapeCoefficients (TrackableId faceId, List<XRFaceArkitBlendShapeCoefficient> coefficientsOut)
        {
            if (coefficientsOut == null)
                throw new ArgumentException("coefficientsOut");

            return TryGetNativeFaceBlendCoefficients (faceId, coefficientsOut);
        }

        // this method is run on startup of the app to register this provider with XR Subsystem Manager
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterDescriptor()
        {
            FaceSubsystemParams descriptorParams = new FaceSubsystemParams
            {
                supportsFacePose = true,
                supportsFaceMeshVerticesAndIndices = true,
                supportsFaceMeshUVs = true,
                id = "ARKitFace",
                implementationType = typeof(ARKitFaceSubsystem)
            };

            XRFaceSubsystemDescriptor.Create(descriptorParams);
        }

        // private methods that implement public API above, while managing allocations and native arrays
        static unsafe bool TryGetNativeAllFaces(List<XRFace> facesOut)
        {
            facesOut.Clear();

            IntPtr ptrNativeFacesArray;
            int facesCount;
            if (!UnityARKit_FaceProvider_TryGetAllFaces(out ptrNativeFacesArray, out facesCount))
            {
                return false;
            }

            if (facesCount <= 0)
            {
                return false;
            }

            // Points directly to the native memory
            var nativeFacesArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<XRFace>(
                (void*)ptrNativeFacesArray, facesCount, Allocator.None);

            if (facesCount > facesOut.Capacity)
            {
                facesOut.Capacity = facesCount;
            }

            facesOut.AddRange(nativeFacesArray);

            return true;
        }

        static bool TryRemoveNativeFace(TrackableId faceId)
        {
            return UnityARKit_FaceProvider_TryRemoveFace(faceId);
        }

        static unsafe bool TryGetNativeFaceMeshVertices(TrackableId faceId, List<Vector3> verticesOut)
        {
            verticesOut.Clear();

            IntPtr ptrNativeVerticesArray;
            int vertexCount;
            if (!UnityARKit_FaceProvider_TryGetFaceMeshVertices(faceId, out ptrNativeVerticesArray,
                out vertexCount))
            {
                return false;
            }

            if (vertexCount <= 0)
                return false;

            if (vertexCount > verticesOut.Capacity)
            {
                verticesOut.Capacity = vertexCount;
            }

            // Points directly to the native memory
            var nativeVertexArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(
                (void*)ptrNativeVerticesArray, vertexCount, Allocator.None);

            verticesOut.AddRange(nativeVertexArray);

            return true;
        }

        static unsafe bool TryGetNativeFaceMeshUVs(TrackableId faceId, List<Vector2> uvsOut)
        {
            uvsOut.Clear();

            IntPtr ptrNativeUVsArray;
            int uvCount;
            if (!UnityARKit_FaceProvider_TryGetFaceMeshUVs(faceId, out ptrNativeUVsArray,
                out uvCount))
            {
                return false;
            }

            if (uvCount <= 0)
            {
                return false;
            }

            if (uvCount > uvsOut.Capacity)
            {
                uvsOut.Capacity = uvCount;
            }

            // Points directly to the native memory
            var nativeUvArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector2>(
                (void*)ptrNativeUVsArray, uvCount, Allocator.None);

            uvsOut.AddRange(nativeUvArray);

            return true;
        }

        static unsafe bool TryGetNativeFaceMeshIndices(TrackableId faceId, List<int> indicesOut)
        {
            indicesOut.Clear();

            IntPtr ptrNativeIndicesArray;
            int indicesCount;
            if (!UnityARKit_FaceProvider_TryGetFaceMeshIndices(faceId, out ptrNativeIndicesArray,
                out indicesCount))
            {
                return false;
            }

            if (indicesCount <= 0)
            {
                return false;
            }

            if (indicesCount > indicesOut.Capacity)
            {
                indicesOut.Capacity = indicesCount;
            }

            // Points directly to the native memory
            var nativeIndicesArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(
                (void*)ptrNativeIndicesArray, indicesCount, Allocator.None);

            indicesOut.AddRange(nativeIndicesArray);

            return true;
        }

        static unsafe bool TryGetNativeFaceBlendCoefficients(TrackableId faceId, List<XRFaceArkitBlendShapeCoefficient> coefficientsOut)
        {
            coefficientsOut.Clear();

            IntPtr ptrNativeCoefficientsArray;
            int coefficientsCount;
            if (!UnityARKit_FaceProvider_TryGetFaceBlendCoefficients(faceId, out ptrNativeCoefficientsArray,
                out coefficientsCount))
            {
                return false;
            }

            if (coefficientsCount <= 0)
            {
                return false;
            }

            if (coefficientsCount > coefficientsOut.Capacity)
            {
                coefficientsOut.Capacity = coefficientsCount;
            }

            // Points directly to the native memory
            var nativeCoefficientsArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<XRFaceArkitBlendShapeCoefficient>(
                (void*)ptrNativeCoefficientsArray, coefficientsCount, Allocator.None);

            coefficientsOut.AddRange(nativeCoefficientsArray);

            return true;
        }
    }
}

