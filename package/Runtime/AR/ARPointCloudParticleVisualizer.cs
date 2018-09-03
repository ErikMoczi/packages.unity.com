using System.Collections.Generic;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Renders an <see cref="ARPointCloud"/> as a <c>ParticleSystem</c>.
    /// </summary>
    [RequireComponent(typeof(ARPointCloud))]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ARPointCloudParticleVisualizer : MonoBehaviour
    {
        void OnPointCloudChanged(ARPointCloud pointCloud)
        {
            var points = s_Vertices;
            pointCloud.GetPoints(points, Space.Self);

            int numParticles = points.Count;
            if (m_Particles == null || m_Particles.Length < numParticles)
                m_Particles = new ParticleSystem.Particle[points.Count];

            var color = m_ParticleSystem.main.startColor.color;
            var size = m_ParticleSystem.main.startSize.constant;

            for (int i = 0; i < numParticles; ++i)
            {
                m_Particles[i].startColor = color;
                m_Particles[i].startSize = size;
                m_Particles[i].position = points[i];
            }

            m_ParticleSystem.SetParticles(m_Particles, points.Count);
        }

        void Awake()
        {
            m_PointCloud = GetComponent<ARPointCloud>();
            m_ParticleSystem = GetComponent<ParticleSystem>();
        }

        void OnEnable()
        {
            m_PointCloud.updated += OnPointCloudChanged;
        }

        void OnDisable()
        {
            m_PointCloud.updated -= OnPointCloudChanged;
        }

        ARPointCloud m_PointCloud;

        ParticleSystem m_ParticleSystem;

        ParticleSystem.Particle[] m_Particles;

        static List<Vector3> s_Vertices = new List<Vector3>();
    }
}
