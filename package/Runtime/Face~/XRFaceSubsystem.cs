using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.XR
{
    public struct FaceAddedEventArgs
    {
        public XRFaceSubsystem FaceSubsystem { get; internal set; }
        public XRFace Face { get; internal set; }
    }

    public struct FaceUpdatedEventArgs
    {
        public XRFaceSubsystem FaceSubsystem { get; internal set; }
        public XRFace Face { get; internal set; }
    }

    public struct FaceRemovedEventArgs
    {
        public XRFaceSubsystem FaceSubsystem { get; internal set; }
        public XRFace Face { get; internal set; }
    }

    public class XRFaceSubsystem : Subsystem<XRFaceSubsystemDescriptor>
    {
        public void GetAllFaces(List<XRFace> facesOut)
        {
            if (facesOut == null)
                throw new ArgumentException("facesOut");

            GetAllFacesAsList(facesOut);
        }

        void GetAllFacesAsList(List<XRFace> faces)
        {
            throw new NotImplementedException();
        }
    }
}
