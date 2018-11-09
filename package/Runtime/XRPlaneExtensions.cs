using System;
using System.Collections.Generic;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARExtensions
{
    /// <summary>
    /// Provides extensions to the <c>XRPlaneSubsystem</c>.
    /// </summary>
    public static class XRPlaneExtensions
    {
        /// <summary>
        /// For internal use. Allows a plane provider to register for the GetTrackingState extension
        /// </summary>
        /// <param name="subsystemId">The string name associated with the plane provider to extend.</param>
        /// <param name="handler">A method that returns the <c>TrackingState</c> of the given <c>TrackableId</c>.</param>
        public static void RegisterGetTrackingStateHandler(string subsystemId, Func<XRPlaneSubsystem, TrackableId, TrackingState> handler)
        {
            s_GetTrackingStateDelegates[subsystemId] = handler;
        }

        /// <summary>
        /// For internal use. Allows a plane provider to register for the <see cref="GetNativePtr(XRPlaneSubsystem, TrackableId)"/>.
        /// </summary>
        /// <param name="subsystemId">The string name associated with the plane provider to extend.</param>
        /// <param name="handler">A method that returns the <c>IntPtr</c> associated with a given <c>TrackableId</c>.</param>
        public static void RegisterGetNativePtrHandler(string subsystemId, Func<XRPlaneSubsystem, TrackableId, IntPtr> handler)
        {
            s_GetNativePtrDelegates[subsystemId] = handler;
        }

        /// <summary>
        /// Retrieve the <c>TrackingState</c> of the given <paramref name="planeId"/>.
        /// </summary>
        /// <param name="planeSubsystem">The <c>XRPlaneSubsystem</c> being extended.</param>
        /// <param name="planeId">The <c>TrackableId</c> associated with this plane.</param>
        /// <returns>The <c>TrackingState</c> of the plane with id <paramref name="planeId"/>.</returns>
        public static TrackingState GetTrackingState(this XRPlaneSubsystem planeSubsystem, TrackableId planeId)
        {
            if (planeSubsystem == null)
                throw new ArgumentNullException("planeSubsystem");

            return s_GetTrackingStateDelegate(planeSubsystem, planeId);
        }

        static TrackingState DefaultGetTrackingState(this XRPlaneSubsystem planeSubsystem, TrackableId planeId)
        {
            return TrackingState.Unknown;
        }

        /// <summary>
        /// Retrieves a native <c>IntPtr</c> associated with a plane with <c>TrackableId</c>
        /// <paramref name="trackableId"/>.
        /// </summary>
        /// <param name="planeSubsystem">The <c>XRPlaneSubsystem</c> being extended.</param>
        /// <param name="trackableId">The <c>TrackableId</c> of a reference point.</param>
        /// <returns>An <c>IntPtr</c> associated with the reference point, or <c>IntPtr.Zero</c> if unavailable.</returns>
        public static IntPtr GetNativePtr(this XRPlaneSubsystem planeSubsystem,
            TrackableId planeId)
        {
            if (planeSubsystem == null)
                throw new ArgumentNullException("planeSubsystem");

            return s_GetNativePtrDelegate(planeSubsystem, planeId);
        }

        static IntPtr DefaultGetNativePtr(XRPlaneSubsystem referencePointSubsystem,
            TrackableId trackableId)
        {
            return IntPtr.Zero;
        }

        /// <summary>
        /// Sets the active subsystem whose extension methods should be used.
        /// </summary>
        /// <param name="planeSubsystem">The <c>XRPlaneSubsystem</c> being extended.</param>
        public static void ActivateExtensions(this XRPlaneSubsystem planeSubsystem)
        {
            if (planeSubsystem == null)
            {
                SetDefaultDelegates();
            }
            else
            {
                var id = planeSubsystem.SubsystemDescriptor.id;
                s_GetNativePtrDelegate = RegistrationHelper.GetValueOrDefault(s_GetNativePtrDelegates, id, DefaultGetNativePtr);
                s_GetTrackingStateDelegate = RegistrationHelper.GetValueOrDefault(s_GetTrackingStateDelegates, id, DefaultGetTrackingState);
            }
        }

        static void SetDefaultDelegates()
        {
            s_GetNativePtrDelegate = DefaultGetNativePtr;
            s_GetTrackingStateDelegate = DefaultGetTrackingState;
        }

        static XRPlaneExtensions()
        {
            SetDefaultDelegates();
        }

        static Func<XRPlaneSubsystem, TrackableId, IntPtr> s_GetNativePtrDelegate;

        static Func<XRPlaneSubsystem, TrackableId, TrackingState> s_GetTrackingStateDelegate;

        static Dictionary<string, Func<XRPlaneSubsystem, TrackableId, IntPtr>> s_GetNativePtrDelegates =
            new Dictionary<string, Func<XRPlaneSubsystem, TrackableId, IntPtr>>();

        static Dictionary<string, Func<XRPlaneSubsystem, TrackableId, TrackingState>> s_GetTrackingStateDelegates =
            new Dictionary<string, Func<XRPlaneSubsystem, TrackableId, TrackingState>>();
    }
}
