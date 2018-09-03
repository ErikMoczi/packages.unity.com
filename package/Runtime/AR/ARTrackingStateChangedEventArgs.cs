using System;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Arguments for the <see cref="ARSubsystemManager.trackingStateChanged"/> event.
    /// </summary>
    public struct ARTrackingStateChangedEventArgs : IEquatable<ARTrackingStateChangedEventArgs>
    {
        /// <summary>
        /// The new <c>TrackingState</c> of the AR session.
        /// </summary>
        public TrackingState trackingState { get; private set; }

        /// <summary>
        /// Constructor for the <see cref="ARTrackingStateChangedEventArgs"/>. This is normally only used by an <see cref="ARSubsystemManager"/>.
        /// </summary>
        /// <param name="trackingState">The <c>TrackingState</c> for the event.</param>
        public ARTrackingStateChangedEventArgs(TrackingState trackingState)
        {
            this.trackingState = trackingState;
        }

        public override int GetHashCode()
        {
            return trackingState.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ARTrackingStateChangedEventArgs))
                return false;

            return Equals((ARTrackingStateChangedEventArgs)obj);
        }

        public bool Equals(ARTrackingStateChangedEventArgs other)
        {
            return (trackingState == other.trackingState);
        }

        public static bool operator ==(ARTrackingStateChangedEventArgs lhs, ARTrackingStateChangedEventArgs rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ARTrackingStateChangedEventArgs lhs, ARTrackingStateChangedEventArgs rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}
