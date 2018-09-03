using System;
using System.Collections.Generic;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARExtensions
{
    /// <summary>
    /// Provides extensions to the <c>XRReferencePointSubsystem</c>.
    /// </summary>
    public static class XRReferencePointExtensions
    {
        /// <summary>
        /// A delegate which defines the AttachReferencePoint method which may be implemented by platform-specific packages.
        /// </summary>
        /// <param name="referencePointSubsystem">The <c>XRReferencePointSubsystem</c> being extended.</param>
        /// <param name="trackableId">The <c>TrackableId</c> of the trackable to which to attach.</param>
        /// <param name="pose">The initial <c>Pose</c> of the trackable.</param>
        /// <returns></returns>
        public delegate TrackableId AttachReferencePointDelegate(XRReferencePointSubsystem referencePointSubsystem,
            TrackableId trackableId, Pose pose);

        /// <summary>
        /// For internal use. Allows a reference point provider to register for the TryAttachReferencePoint extension
        /// </summary>
        /// <param name="subsystemId">The string name associated with the camera provider to extend.</param>
        /// <param name="handler">A method that returns true if permission is granted.</param>
        public static void RegisterAttachReferencePointHandler(string subsystemId, AttachReferencePointDelegate handler)
        {
            s_AttachReferencePointHandlers[subsystemId] = handler;
            UpdateCurrentHandler(subsystemId, s_AttachReferencePointHandlers, ref s_CurrentAttachDelegate);
        }

        /// <summary>
        /// Creates a new reference point that is "attached" to an existing trackable, like a plane.
        /// The reference point will update with the trackable according to rules specific to that
        /// trackable type.
        /// </summary>
        /// <param name="referencePointSubsystem">The <c>XRReferencePointSubsystem</c> being extended.</param>
        /// <param name="trackableId">The <c>TrackableId</c> of the trackable to which to attach.</param>
        /// <param name="pose">The initial <c>Pose</c> of the trackable.</param>
        /// <returns></returns>
        public static TrackableId AttachReferencePoint(this XRReferencePointSubsystem referencePointSubsystem,
            TrackableId trackableId, Pose pose)
        {
            if (s_CurrentAttachDelegate == null)
                return TrackableId.InvalidId;

            return s_CurrentAttachDelegate(referencePointSubsystem, trackableId, pose);
        }

        /// <summary>
        /// Sets the active subsystem whose extension methods should be used.
        /// </summary>
        /// <param name="referencePointSubsystem">The <c>XRReferencePointSubsystem</c> being extended.</param>
        public static void ActivateExtensions(this XRReferencePointSubsystem referencePointSubsystem)
        {
            if (referencePointSubsystem == null)
                throw new ArgumentNullException("referencePointSubsystem");

            s_CurrentSubsystem = referencePointSubsystem;

            var id = referencePointSubsystem.SubsystemDescriptor.id;
            s_AttachReferencePointHandlers.TryGetValue(id, out s_CurrentAttachDelegate);
        }

        static void UpdateCurrentHandler<T>(string subsystemId, Dictionary<string, T> handlers, ref T currentHandler)
        {
            if (s_CurrentSubsystem == null)
                return;

            if (s_CurrentSubsystem.SubsystemDescriptor.id == subsystemId)
                handlers.TryGetValue(subsystemId, out currentHandler);
        }

        static AttachReferencePointDelegate s_CurrentAttachDelegate;

        static XRReferencePointSubsystem s_CurrentSubsystem;

        static Dictionary<string, AttachReferencePointDelegate> s_AttachReferencePointHandlers =
            new Dictionary<string, AttachReferencePointDelegate>();
    }
}
