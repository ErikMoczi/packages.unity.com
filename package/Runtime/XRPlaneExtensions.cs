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
        /// Retrieve the <c>TrackingState</c> of the given <paramref name="planeId"/>.
        /// </summary>
        /// <param name="planeSubsystem">The <c>XRPlaneSubsystem</c> being extended.</param>
        /// <param name="planeId">The <c>TrackableId</c> associated with this plane.</param>
        /// <returns>The <c>TrackingState</c> of the plane with id <paramref name="planeId"/>.</returns>
        public static TrackingState GetTrackingState(this XRPlaneSubsystem planeSubsystem, TrackableId planeId)
        {
            if (planeSubsystem == null)
                throw new ArgumentNullException("planeSubsystem");

            Func<XRPlaneSubsystem, TrackableId, TrackingState> handler;
            if (s_GetTrackingStateDelegates.TryGetValue(planeSubsystem.SubsystemDescriptor.id, out handler))
            {
                return handler(planeSubsystem, planeId);
            }
            else
            {
                return TrackingState.Unknown;
            }
        }

        static Dictionary<string, Func<XRPlaneSubsystem, TrackableId, TrackingState>> s_GetTrackingStateDelegates =
            new Dictionary<string, Func<XRPlaneSubsystem, TrackableId, TrackingState>>();
    }
}
