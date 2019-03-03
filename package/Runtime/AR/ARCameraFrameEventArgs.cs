using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// A structure for camera-related information pertaining to a particular frame.
    /// This is used to communicate information in the <see cref="ARSubsystemManager.cameraFrameReceived" /> event.
    /// </summary>
    public struct ARCameraFrameEventArgs : IEquatable<ARCameraFrameEventArgs>
    {
        /// <summary>
        /// The <see cref="LightEstimationData" /> associated with this frame.
        /// </summary>
        public LightEstimationData lightEstimation { get; set; }

        /// <summary>
        /// The time, in nanoseconds, associated with this frame.
        /// Use <c>timestampNs.HasValue</c> to determine if this data is available.
        /// </summary>
        public long? timestampNs { get; set; }

        // TODO
        public Matrix4x4? projectionMatrix { get; set; }

        // TODO
        public Matrix4x4? displayMatrix { get; set; }

        // TODO
        public List<Texture2D> textures { get; set; }

        // TODO
        public List<int> propertyNameIds { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = lightEstimation.GetHashCode();
                hash = hash * 486187739 + timestampNs.GetHashCode();
                hash = hash * 486187739 + projectionMatrix.GetHashCode();
                hash = hash * 486187739 + displayMatrix.GetHashCode();
                hash = hash * 486187739 + (textures == null ? 0 : textures.GetHashCode());
                hash = hash * 486187739 + (propertyNameIds == null ? 0 : propertyNameIds.GetHashCode());
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ARCameraFrameEventArgs))
                return false;

            return Equals((ARCameraFrameEventArgs)obj);
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("lightEstimation: " + lightEstimation.ToString());
            stringBuilder.Append("\ntimestamp: "  + timestampNs);
            if (timestampNs.HasValue)
                stringBuilder.Append("ns");
            stringBuilder.Append("\nprojectionMatrix: " + projectionMatrix);
            stringBuilder.Append("\ndisplayMatrix: " + displayMatrix);
            stringBuilder.Append("\ntexture count: " + (textures == null ? 0 : textures.Count));
            stringBuilder.Append("\npropertyNameId count: " + (propertyNameIds == null ? 0 : propertyNameIds.Count));

            return stringBuilder.ToString();
        }

        public bool Equals(ARCameraFrameEventArgs other)
        {
            return
                (lightEstimation.Equals(other.lightEstimation)) &&
                (projectionMatrix == other.projectionMatrix) &&
                (displayMatrix == other.displayMatrix) &&
                (timestampNs == other.timestampNs);
        }

        public static bool operator ==(ARCameraFrameEventArgs lhs, ARCameraFrameEventArgs rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ARCameraFrameEventArgs lhs, ARCameraFrameEventArgs rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}
