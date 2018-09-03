using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.Experimental.U2D.IK
{
    [Solver2DMenuAttribute("Chain (FABRIK)")]
    public class FabrikSolver2D : Solver2D
    {
        private const float kMinTolerance = 0.001f;
        private const int kMinIterations = 1;

        [SerializeField]
        private IKChain2D m_Chain = new IKChain2D();
        [SerializeField][Range(kMinIterations, 50)]
        private int m_Iterations = 10;
        [SerializeField][Range(kMinTolerance, 0.1f)]
        private float m_Tolerance = 0.01f;

        private float[] m_Lengths;
        private Vector2[] m_Positions;
        private Vector3[] m_WorldPositions;

        public int iterations
        {
            get { return m_Iterations; }
            set { m_Iterations = Mathf.Max(value, kMinIterations); }
        }

        public float tolerance
        {
            get { return m_Tolerance; }
            set { m_Tolerance = Mathf.Max(value, kMinTolerance); }
        }

        protected override int GetChainCount()
        {
            return 1;
        }

        public override IKChain2D GetChain(int index)
        {
            return m_Chain;
        }

        protected override void DoPrepare()
        {
            if (m_Positions == null || m_Positions.Length != m_Chain.transformCount)
            {
                m_Positions = new Vector2[m_Chain.transformCount];
                m_Lengths = new float[m_Chain.transformCount - 1];
                m_WorldPositions = new Vector3[m_Chain.transformCount];
            }

            for (int i = 0; i < m_Chain.transformCount; ++i)
            {
                m_Positions[i] = GetPointOnSolverPlane(m_Chain.transforms[i].position);
            }
            for (int i = 0; i < m_Chain.transformCount - 1; ++i)
            {
                m_Lengths[i] = (m_Positions[i + 1] - m_Positions[i]).magnitude;
            }
        }

        protected override void DoUpdateIK(List<Vector3> effectorPositions)
        {
            Profiler.BeginSample("FABRIKSolver2D.DoUpdateIK");

            Vector3 effectorPosition = effectorPositions[0];
            effectorPosition = GetPointOnSolverPlane(effectorPosition);
            if (FABRIK2D.Solve(effectorPosition, iterations, tolerance, m_Lengths, ref m_Positions))
            {
                // Convert all plane positions to world positions
                for (int i = 0; i < m_Positions.Length; ++i)
                {
                    m_WorldPositions[i] = GetWorldPositionFromSolverPlanePoint(m_Positions[i]);
                }

                for (int i = 0; i < m_Chain.transformCount - 1; ++i)
                {
                    Vector3 startLocalPosition = m_Chain.transforms[i + 1].localPosition;
                    Vector3 endLocalPosition = m_Chain.transforms[i].InverseTransformPoint(m_WorldPositions[i + 1]);
                    m_Chain.transforms[i].localRotation *= Quaternion.FromToRotation(startLocalPosition, endLocalPosition);
                }
            }

            Profiler.EndSample();
        }
    }
}
