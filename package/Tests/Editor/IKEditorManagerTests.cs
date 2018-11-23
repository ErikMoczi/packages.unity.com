using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.U2D.IK;
using UnityEngine.Experimental.U2D.IK.Tests;
using UnityEngine.TestTools;

namespace UnityEditor.Experimental.U2D.IK.Tests.IKEditorManagerTests
{
    public class IKEditorManagerTests
    {
        private Vector3Compare vec3Compare = new Vector3Compare();
        private FloatCompare floatCompare = new FloatCompare();

        private IKEditorManager editorManager;

        private GameObject go;
        private GameObject targetGO;
        private GameObject ikGO;
        private GameObject effectorGO;

        private IKManager2D manager;
        private Solver2D solver;
        private IKChain2D chain;

        [SetUp]
        public void Setup()
        {
            editorManager = IKEditorManager.instance;

            go = new GameObject();
            var child1GO = new GameObject();
            child1GO.transform.parent = go.transform;

            var child2GO = new GameObject();
            child2GO.transform.parent = child1GO.transform;

            var child3GO = new GameObject();
            child3GO.transform.parent = child2GO.transform;

            targetGO = new GameObject();
            targetGO.transform.parent = child3GO.transform;

            go.transform.position = Vector3.zero;
            child1GO.transform.position = new Vector3(1.0f, 0.0f, 0.0f);
            child2GO.transform.position = new Vector3(3.0f, 0.0f, 0.0f);
            child3GO.transform.position = new Vector3(6.0f, 0.0f, 0.0f);
            targetGO.transform.position = new Vector3(10.0f, 0.0f, 0.0f);

            ikGO = new GameObject();
            manager = ikGO.AddComponent<IKManager2D>();
            var lsGO = new GameObject();
            solver = lsGO.AddComponent<FabrikSolver2D>();
            lsGO.transform.parent = ikGO.transform;

            effectorGO = new GameObject();
            effectorGO.transform.parent = solver.transform;
            effectorGO.transform.position = new Vector3(10.0f, 0.0f, 0.0f);

            chain = solver.GetChain(0);
            chain.effector = targetGO.transform;
            chain.target = effectorGO.transform;
            chain.transformCount = 5;

            solver.Initialize();

            manager.AddSolver(solver);

            editorManager.Initialize();
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(ikGO);
        }

        [Test]
        public void EditorManager_UpdatesDirtyIK()
        {
            if (Application.isPlaying)
                return;
            
            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            Undo.RecordObject(effectorGO.transform, "Move Effector");
            effectorGO.transform.position = targetPosition;

            editorManager.OnLateUpdate();

            Assert.That(targetPosition, Is.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition - chain.effector.position).magnitude).Using(floatCompare));

            Undo.PerformUndo();
        }

        [Test]
        public void EditorManager_FindsManagerForSolver()
        {
            var solverManager = editorManager.FindManager(solver);

            Assert.AreSame(manager, solverManager);
        }

        [Test]
        public void UndoDirtyIK_UndosMoveForTransforms()
        {
            var originalPosition = chain.effector.position;
            var targetPosition = new Vector3(9.0f, 1.0f, 0.0f);

            editorManager.RegisterUndo(solver, "Move Effector");
            Undo.RecordObject(effectorGO.transform, "Move Effector");
            effectorGO.transform.position = targetPosition;
            editorManager.UpdateSolverImmediate(solver, false);

            Assert.That(targetPosition, Is.EqualTo(chain.effector.position).Using(vec3Compare));

            Undo.PerformUndo();

            Assert.That(targetPosition, Is.Not.EqualTo(originalPosition).Using(vec3Compare));
            Assert.That(targetPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(originalPosition, Is.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(originalPosition, Is.EqualTo(effectorGO.transform.position).Using(vec3Compare));
        }

        [Test]
        public void SetChainPositionOverride_OverridesEffector()
        {
            var targetPosition = new Vector3(9.0f, -1.0f, 0.0f);
            var effectorPosition = new Vector3(9.0f, 1.0f, 0.0f);

            chain.target = null;
            effectorGO.transform.position = effectorPosition;

            editorManager.SetChainPositionOverride(chain, targetPosition);
            editorManager.UpdateSolverImmediate(solver, false);

            Assert.That(effectorPosition, Is.Not.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(targetPosition, Is.EqualTo(chain.effector.position).Using(vec3Compare));
            Assert.That(0.0f, Is.EqualTo((targetPosition - chain.effector.position).magnitude).Using(floatCompare));
        }
    }
}
