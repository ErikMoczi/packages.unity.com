using System;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// A structure for light estimation information provided by the AR device.
    /// </summary>
    public struct LightEstimationData : IEquatable<LightEstimationData>
    {
        /// <summary>
        /// An estimate for the average brightness in the scene.
        /// Use <c>averageBrightness.HasValue</c> to determine if this information is available.
        /// </summary>
        /// <remarks>
        /// <c>averageBrightness</c> may be null when light estimation is not enabled in the <see cref="ARSession"/>,
        /// if the platform does not support it, or if a platform-specific error has occurred.
        /// </remarks>
        public float? averageBrightness { get; set; }

        /// <summary>
        /// An estimate for the average color temperature of the scene.
        /// Use <c>averageColorTemperature.HasValue</c> to determine if this information is available.
        /// </summary>
        /// <remarks>
        /// <c>averageColorTemperature</c> may be null when light estimation is not enabled in the <see cref="ARSession"/>,
        /// if the platform does not support it, or if a platform-specific error has occurred.
        /// </remarks>
        public float? averageColorTemperature { get; set; }

        public override int GetHashCode()
        {
            return averageBrightness.GetHashCode() ^ averageColorTemperature.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LightEstimationData))
                return false;

            return Equals((LightEstimationData)obj);
        }

        public override string ToString()
        {
            return string.Format("(Avg. Brightness: {0}, Avg. Color Temperature {1})", averageBrightness, averageColorTemperature);
        }

        public bool Equals(LightEstimationData other)
        {
            return
                (averageBrightness.Equals(other.averageBrightness)) &&
                (averageColorTemperature.Equals(other.averageColorTemperature));
        }

        public static bool operator ==(LightEstimationData lhs, LightEstimationData rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(LightEstimationData lhs, LightEstimationData rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}
