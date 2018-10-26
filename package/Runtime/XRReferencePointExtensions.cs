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
        /// <param name="subsystemId">The string name associated with the reference point provider to extend.</param>
        /// <param name="handler">A method that returns true if permission is granted.</param>
        public static void RegisterAttachReferencePointHandler(string subsystemId, AttachReferencePointDelegate handler)
        {
            s_AttachReferencePointDelegates[subsystemId] = handler;
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
            if (referencePointSubsystem == null)
                throw new ArgumentNullException("referencePointSubsystem");

            return s_AttachReferencePointDelegate(referencePointSubsystem, trackableId, pose);
        }

        static TrackableId DefaultAttachReferencePoint(this XRReferencePointSubsystem referencePointSubsystem,
            TrackableId trackableId, Pose pose)
        {
            return TrackableId.InvalidId;
        }

        /// <summary>
        /// Sets the active subsystem whose extension methods should be used.
        /// </summary>
        /// <param name="referencePointSubsystem">The <c>XRReferencePointSubsystem</c> being extended.</param>
        public static void ActivateExtensions(this XRReferencePointSubsystem referencePointSubsystem)
        {
            if (referencePointSubsystem == null)
            {
                SetDefaultDelegates();
            }
            else
            {
                var id = referencePointSubsystem.SubsystemDescriptor.id;
                s_AttachReferencePointDelegate = RegistrationHelper.GetValueOrDefault(s_AttachReferencePointDelegates, id, DefaultAttachReferencePoint);
            }
        }

        static void SetDefaultDelegates()
        {
            s_AttachReferencePointDelegate = DefaultAttachReferencePoint;
        }

        static XRReferencePointExtensions()
        {
            SetDefaultDelegates();
        }

        static AttachReferencePointDelegate s_AttachReferencePointDelegate;

        static Dictionary<string, AttachReferencePointDelegate> s_AttachReferencePointDelegates =
            new Dictionary<string, AttachReferencePointDelegate>();
    }
}
