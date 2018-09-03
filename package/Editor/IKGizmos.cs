using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D.IK;

namespace UnityEditor.Experimental.U2D.IK
{
    internal class IKGizmos : ScriptableSingleton<IKGizmos>
    {
        private static readonly int kEffectorHashCode = "IkEffector".GetHashCode();
        private Color enabledColor = Color.green;
        private Color disabledColor = Color.grey;
        private const float kCircleHandleRadius = 0.1f;
        private const float kNodeRadius = 0.05f;
        private const float kDottedLineLength = 5f;
        private const float kFadeStart = 0.75f;
        private const float kFadeEnd = 1.75f;
        private Dictionary<IKChain2D, Vector3> m_ChainPositionOverrides = new Dictionary<IKChain2D, Vector3>();
        public bool isDragging { get; private set; }

        public void DoSolverGUI(Solver2D solver)
        {
            if (solver == null || !solver.isValid)
                return;

            DrawSolver(solver);

            var allChainsHaveEffectors = solver.allChainsHaveEffectors;

            for (int i = 0; i < solver.chainCount; ++i)
            {
                var chain = solver.GetChain(i);
                if (chain == null)
                    continue;

                if (allChainsHaveEffectors)
                {
                    if (!IsEffectorTransformSelected(chain))
                        DoEffectorGUI(solver, chain);
                }
                else if(chain.effector == null)
                    DoIkPoseGUI(solver, chain);
            }

            if(GUIUtility.hotControl == 0)
                isDragging = false;
        }

        private void DoEffectorGUI(Solver2D solver, IKChain2D chain)
        {
            int controlId = GUIUtility.GetControlID(kEffectorHashCode, FocusType.Passive);

            var color = FadeFromChain(Color.white, chain);

            if (!isDragging && (color.a == 0f || !IsVisible(chain.effector.position)))
                return;

            EditorGUI.BeginChangeCheck();

            Handles.color = color;
            var newPosition = Handles.Slider2D(controlId, chain.effector.position, chain.effector.forward, chain.effector.up, chain.effector.right, HandleUtility.GetHandleSize(chain.target.position) * kCircleHandleRadius, Handles.CircleHandleCap, Vector2.zero);

            if (EditorGUI.EndChangeCheck())
            {
                if(!isDragging)
                {
                    isDragging = true;
                    IKEditorManager.instance.RegisterUndo(solver, "Move Effector");
                }

                Undo.RecordObject(chain.effector, "Move Effector");

                chain.effector.position = newPosition;
            }
        }

        private void DoIkPoseGUI(Solver2D solver, IKChain2D chain)
        {
            int controlId = GUIUtility.GetControlID(kEffectorHashCode, FocusType.Passive);

            var color = FadeFromChain(Color.white, chain);

            if (!isDragging && (color.a == 0f || !IsVisible(chain.target.position)))
                return;

            if (HandleUtility.nearestControl == controlId && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                StoreSolverPositionOverrides(solver); 

            EditorGUI.BeginChangeCheck();

            Handles.color = color;
            Vector3 newPosition = Handles.Slider2D(controlId, chain.target.position, chain.target.forward, chain.target.up, chain.target.right, HandleUtility.GetHandleSize(chain.target.position) * kCircleHandleRadius, Handles.CircleHandleCap, Vector2.zero);

            if (EditorGUI.EndChangeCheck())
            {
                if(!isDragging)
                    isDragging = true;
                
                IKEditorManager.instance.Record(solver, "IK Pose");

                SetSolverPositionOverrides();
                IKEditorManager.instance.SetChainPositionOverride(chain, newPosition);
                IKEditorManager.instance.UpdateSolverImmediate(solver, true);
            }
        }

        private void StoreSolverPositionOverrides(Solver2D solver)
        {
            Debug.Assert(solver.allChainsHaveEffectors == false);

            m_ChainPositionOverrides.Clear();

            IKManager2D manager = IKEditorManager.instance.FindManager(solver);
            foreach (Solver2D l_solver in manager.solvers)
            {
                if(l_solver.allChainsHaveEffectors)
                    continue;
                    
                for (int i = 0; i < l_solver.chainCount; ++i)
                {
                    var chain = l_solver.GetChain(i);
                        m_ChainPositionOverrides[chain] = chain.target.position;
                }
            }
        }

        private void SetSolverPositionOverrides()
        {
            foreach (var pair in m_ChainPositionOverrides)
                IKEditorManager.instance.SetChainPositionOverride(pair.Key, pair.Value);
        }

        private bool IsEffectorTransformSelected(IKChain2D chain)
        {
            Debug.Assert(chain.effector != null);

            return Selection.Contains(chain.effector.gameObject);
        }

        private void DrawSolver(Solver2D solver)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            IKManager2D manager = IKEditorManager.instance.FindManager(solver);
            bool isManagerDisabled = !solver.isActiveAndEnabled || manager == null || !manager.isActiveAndEnabled;

            for (int i = 0; i < solver.chainCount; ++i)
            {
                var chain = solver.GetChain(i);
                if (chain != null)
                    DrawChain(chain, isManagerDisabled, solver.allChainsHaveEffectors);
            }
        }

        private void DrawChain(IKChain2D chain, bool isManagerDisabled, bool solverHasEffectors)
        {
            Handles.matrix = Matrix4x4.identity;
            Color color = enabledColor;

            if (isManagerDisabled)
                color = disabledColor;
            else if (!solverHasEffectors)
                color = Color.yellow;

            color = FadeFromChain(color, chain);

            if (color.a == 0f)
                return;

            Transform currentTransform = chain.target;
            for (int i = 0; i < chain.transformCount - 1; ++i)
            {
                var parentPosition = currentTransform.parent.position;
                var position = currentTransform.position;
                Vector3 projectedLocalPosition = Vector3.Project(currentTransform.localPosition, Vector3.right);
                Vector3 projectedEndPoint = currentTransform.parent.position + currentTransform.parent.TransformVector(projectedLocalPosition);
                var visible = IsVisible(projectedEndPoint) || IsVisible(position);

                if (visible && currentTransform.localPosition.sqrMagnitude != projectedLocalPosition.sqrMagnitude)
                {
                    Color red = Color.red;
                    red.a = color.a;
                    Handles.color = red;
                    Handles.DrawDottedLine(projectedEndPoint, position, kDottedLineLength);
                }

                visible = IsVisible(parentPosition) || IsVisible(projectedEndPoint);

                Handles.color = color;
                if (visible)
                    Handles.DrawDottedLine(parentPosition, projectedEndPoint, kDottedLineLength);

                currentTransform = currentTransform.parent;
            }

            Handles.color = color;
            currentTransform = chain.target;
            for (int i = 0; i < chain.transformCount; ++i)
            {
                var position = currentTransform.position;
                var size = HandleUtility.GetHandleSize(position);

                if (IsVisible(position))
                    Handles.DrawSolidDisc(position, currentTransform.forward, kNodeRadius * size);

                currentTransform = currentTransform.parent;
            }

            Handles.color = Color.white;
        }

        private Color FadeFromChain(Color color, IKChain2D chain)
        {
            var size = HandleUtility.GetHandleSize(chain.target.position);
            var scaleFactor = 1f;
            var lengths = chain.lengths;
            foreach (var length in lengths)
                scaleFactor = Mathf.Max(scaleFactor, length);

            return FadeFromSize(color, size, kFadeStart * scaleFactor, kFadeEnd * scaleFactor);
        }

        private Color FadeFromSize(Color color, float size, float fadeStart, float fadeEnd)
        {
            float alpha = Mathf.Lerp(1f, 0f, (size - fadeStart) / (fadeEnd - fadeStart));
            color.a = alpha;
            return color;
        }

        private bool IsVisible(Vector3 position)
        {
            var screenPos = HandleUtility.GUIPointToScreenPixelCoordinate(HandleUtility.WorldToGUIPoint(position));
            if (screenPos.x < 0f || screenPos.x > Camera.current.pixelWidth || screenPos.y < 0f || screenPos.y > Camera.current.pixelHeight)
                return false;

            return true;
        }
    }
}
