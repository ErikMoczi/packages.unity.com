using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARExtensions;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// <c>XRSessionSubsystem</c> extensions for interacting with <see cref="ARWorldMap"/>s.
    /// </summary>
    public static class ARWorldMapSessionExtensions
    {
        /// <summary>
        /// Asynchronously create an <see cref="ARWorldMap"/>. An <c>ARWorldMap</c>
        /// represents the state of the session and can be serialized to a byte
        /// array to persist the session data, or send it to another device for
        /// shared AR experiences.
        /// It is a wrapper for <a href="https://developer.apple.com/documentation/arkit/arworldmap">ARKit's ARWorldMap</a>.
        /// See also <see cref="ApplyWorldMap(XRSessionSubsystem, ARWorldMap)"/>.
        /// and <see cref="WorldMapSupported(XRSessionSubsystem)"/>.
        /// </summary>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> being extended.</param>
        /// <returns>An <see cref="ARWorldMapRequest"/> which can be used to determine the status
        /// of the request and get the <c>ARWorldMap</c> when complete.</returns>
        public static ARWorldMapRequest GetARWorldMapAsync(this XRSessionSubsystem sessionSubsystem)
        {
            if (sessionSubsystem == null)
                throw new ArgumentNullException("sessionSubsystem");

            var requestId = Api.UnityARKit_createWorldMapRequest();
            return new ARWorldMapRequest(requestId);
        }

        /// <summary>
        /// Asynchronously create an <see cref="ARWorldMap"/>. An <c>ARWorldMap</c>
        /// represents the state of the session and can be serialized to a byte
        /// array to persist the session data, or send it to another device for
        /// shared AR experiences.
        /// It is a wrapper for <a href="https://developer.apple.com/documentation/arkit/arworldmap">ARKit's ARWorldMap</a>.
        /// See also <see cref="ApplyWorldMap(XRSessionSubsystem, ARWorldMap)"/>.
        /// and <see cref="WorldMapSupported(XRSessionSubsystem)"/>.
        ///
        /// If the <see cref="ARWorldMapRequestStatus"/> is <see cref="ARWorldMapRequestStatus.Success"/>, then
        /// the resulting <see cref="ARWorldMap"/> must be disposed to avoid leaking native resources. Otherwise,
        /// the <see cref="ARWorldMap"/> is not valid, and need not be disposed.
        /// </summary>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> being extended.</param>
        /// <param name="onComplete">A method to invoke when the world map has either been created, or determined
        /// that it could not be created. Check the value of the <see cref="ARWorldMapRequestStatus"/> parameter
        /// to determine whether the world map was successfully created.</param>
        public static void GetARWorldMapAsync(
            this XRSessionSubsystem sessionSubsystem,
            Action<ARWorldMapRequestStatus, ARWorldMap> onComplete)
        {
            if (sessionSubsystem == null)
                throw new ArgumentNullException("sessionSubsystem");

            var handle = GCHandle.Alloc(onComplete);
            var context = GCHandle.ToIntPtr(handle);

            Api.UnityARKit_createWorldMapRequestWithCallback(s_OnAsyncWorldMapCompleted, context);
        }

        /// <summary>
        /// Detect <see cref="ARWorldMap"/> support. <c>ARWorldMap</c> requires iOS 12 or greater.
        /// See also <see cref="GetARWorldMapAsync(XRSessionSubsystem)"/>.
        /// </summary>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> being extended.</param>
        /// <returns><c>true</c> if <c>ARWorldMap</c>s are supported, otherwise <c>false</c>.</returns>
        public static bool WorldMapSupported(this XRSessionSubsystem sessionSubsystem)
        {
            if (sessionSubsystem == null)
                throw new ArgumentNullException("sessionSubsystem");

            return Api.UnityARKit_worldMapSupported();
        }

        /// <summary>
        /// Get the world mapping status. Used to determine the suitability of the current session for
        /// creating an <see cref="ARWorldMap"/>.
        /// </summary>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> being extended.</param>
        /// <returns>The <see cref="ARWorldMappingStatus"/> of the session.</returns>
        public static ARWorldMappingStatus GetWorldMappingStatus(this XRSessionSubsystem sessionSubsystem)
        {
            if (sessionSubsystem == null)
                throw new ArgumentNullException("sessionSubsystem");

            return Api.UnityARKit_getWorldMappingStatus();
        }

        /// <summary>
        /// Apply an existing <see cref="ARWorldMap"/> to the session. This will attempt
        /// to relocalize the current session to the given <paramref name="worldMap"/>.
        /// If relocalization is successful, the stored planes & reference points from
        /// the <paramref name="worldMap"/> will be added to the current session.
        /// This is equivalent to setting the <a href="https://developer.apple.com/documentation/arkit/arworldtrackingconfiguration/2968180-initialworldmap">initialWorldMap</a>
        /// property on the session's <a href="https://developer.apple.com/documentation/arkit/arworldtrackingconfiguration">ARWorldTrackingConfiguration</a>.
        /// </summary>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> being extended.</param>
        /// <param name="worldMap">An <see cref="ARWorldMap"/> with which to relocalize the session.</param>
        public static void ApplyWorldMap(
            this XRSessionSubsystem sessionSubsystem,
            ARWorldMap worldMap)
        {
            if (sessionSubsystem == null)
                throw new ArgumentNullException("sessionSubsystem");

            if (worldMap.nativeHandle == ARWorldMap.k_InvalidHandle)
                throw new InvalidOperationException("ARWorldMap has been disposed.");

            Api.UnityARKit_applyWorldMap(worldMap.nativeHandle);
        }

        static ARWorldMapSessionExtensions()
        {
            s_OnAsyncWorldMapCompleted = OnAsyncConversionComplete;
        }

        internal delegate void OnAsyncConversionCompleteDelegate(
            ARWorldMapRequestStatus status,
            int worldMapId,
            IntPtr context);

        static OnAsyncConversionCompleteDelegate s_OnAsyncWorldMapCompleted;

        [MonoPInvokeCallback(typeof(OnAsyncConversionCompleteDelegate))]
        static unsafe void OnAsyncConversionComplete(ARWorldMapRequestStatus status, int worldMapId, IntPtr context)
        {
            var handle = GCHandle.FromIntPtr(context);
            var onComplete = (Action<ARWorldMapRequestStatus, ARWorldMap>)handle.Target;

            if (status.IsError())
            {
                onComplete(status, default(ARWorldMap));
            }
            else
            {
                var worldMap = new ARWorldMap(worldMapId);
                onComplete(status, worldMap);
            }

            handle.Free();
        }
    }
}
