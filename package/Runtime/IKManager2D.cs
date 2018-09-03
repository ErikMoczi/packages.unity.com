using System.Collections.Generic;

namespace UnityEngine.Experimental.U2D.IK
{
    [DefaultExecutionOrder(-1)]
    public class IKManager2D : MonoBehaviour
    {
        [SerializeField]
        private List<Solver2D> m_Solvers = new List<Solver2D>();
        [SerializeField][Range(0f, 1f)]
        private float m_Weight = 1f;

        public float weight
        {
            get { return m_Weight; }
            set { m_Weight = Mathf.Clamp01(value); }
        }

        public List<Solver2D> solvers
        {
            get { return m_Solvers; }
        }

        private void OnValidate()
        {
            m_Weight = Mathf.Clamp01(m_Weight);
        }

        private void OnEnable()
        {
        }

        private void Reset()
        {
            FindChildSolvers();
        }

        private void FindChildSolvers()
        {
            m_Solvers.Clear();

            List<Solver2D> solvers = new List<Solver2D>();
            transform.GetComponentsInChildren<Solver2D>(true, solvers);

            foreach (Solver2D solver in solvers)
            {
                if (solver.GetComponentInParent<IKManager2D>() == this)
                    AddSolver(solver);
            }
        }

        public void AddSolver(Solver2D solver)
        {
            if (!m_Solvers.Contains(solver))
                m_Solvers.Add(solver);
        }

        public void RemoveSolver(Solver2D solver)
        {
            m_Solvers.Remove(solver);
        }

        public void UpdateManager()
        {
            foreach (var solver in m_Solvers)
            {
                if (solver == null || !solver.isActiveAndEnabled)
                    continue;

                if (!solver.isValid)
                    solver.Initialize();

                solver.UpdateIK(weight);
            }
        }

        private void LateUpdate()
        {
            UpdateManager();
        }
    }
}
