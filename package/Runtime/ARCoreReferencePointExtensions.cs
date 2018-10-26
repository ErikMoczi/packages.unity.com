using UnityEngine.XR.ARExtensions;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARCore
{
    /// <summary>
    /// For internal use. Provides ARCore-specific extensions to the XRReferencePointSubsystem.
    /// </summary>
    internal class ARCoreReferencePointExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            XRReferencePointExtensions.RegisterAttachReferencePointHandler(k_SubsystemId, AttachReferencePoint);
        }

        static TrackableId AttachReferencePoint(XRReferencePointSubsystem referencePointSubsystem,
            TrackableId trackableId, Pose pose)
        {
            return Api.UnityARCore_attachReferencePoint(trackableId, pose);
        }

        static readonly string k_SubsystemId = "ARCore-ReferencePoint";
    }
}
