using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;
using UnityEngine.Experimental.U2D.Common;

namespace UnityEditor.Experimental.U2D.Animation
{
    public enum BoneGizmoAction
    {
        None,
        Select,
        Rotate,
        Move
    }

    public enum BoneGizmoSelectionMode
    {
        Single,
        Toggle
    }

    public interface IBoneGizmoView
    {
        void SetupLayout();
        bool CanRepaint();
        bool CanLayout();
        void LayoutBone(Transform boneTransform, float length);
        bool DoSelection(Transform boneTransform, out BoneGizmoSelectionMode mode);
        bool DoBoneRotation(Transform boneTransform, out float deltaAngle);
        bool DoBonePosition(Transform boneTransform, out Vector3 deltaPosition);
        bool IsActionActive(Transform boneTransform, BoneGizmoAction action);
        bool IsActionHot(Transform boneTransform, BoneGizmoAction action);
        bool IsActionHot(BoneGizmoAction action);
        void DrawBone(Transform boneTransform, float length, float alpha);
        void DrawParentBoneLink(Transform boneTransform, float parentLength, Color color);
        bool IsBoneVisible(Transform boneTransform, float length, float alpha);
        bool IsVisible(Vector3 position);
        float GetHandleSize(Vector3 position);
    }

    internal class BoneGizmoView : IBoneGizmoView
    {
        readonly float kBoneScale = 0.1f;
        readonly float kBoneLenghtRatio = 0.5f;
        readonly int boneBodyHashCode = "BoneBody".GetHashCode();
        readonly int boneHeadHashCode = "BoneHead".GetHashCode();

        private IGUIWrapper m_GUIWapper;
        private BoneGizmoAction m_Action;
        private int m_HoveredBoneBodyControlID = -1;
        private int m_HoveredBoneHeadControlID = -1;
        private int m_RotateBoneControlID = -1;
        private int m_MoveBoneControlID = -1;
        private Transform m_HoveredBoneTransform;
        private Transform m_HotBoneTransform;
        private Vector3 m_SliderPosition;
        private Vector3 m_RotationPivot;

        public BoneGizmoView(IGUIWrapper guiWrapper)
        {
            m_GUIWapper = guiWrapper;
        }

        public void SetupLayout()
        {
            if (m_GUIWapper.eventType == EventType.Layout)
            {
                m_HoveredBoneBodyControlID = -1;
                m_HoveredBoneHeadControlID = -1;
                m_HoveredBoneTransform = null;

                if (m_GUIWapper.IsControlHot(0))
                {
                    m_RotateBoneControlID = -1;
                    m_MoveBoneControlID = -1;
                    m_HotBoneTransform = null;
                }
            }
        }

        public bool CanRepaint()
        {
            return m_GUIWapper.IsRepainting();
        }

        public bool CanLayout()
        {
            return m_GUIWapper.eventType == EventType.Layout;
        }

        public void LayoutBone(Transform boneTransform, float length)
        {
            if (IsLayerLocked(boneTransform))
                return;

            var position = boneTransform.position;
            var endPosition = boneTransform.TransformPoint(Vector3.right * length);

            int boneBodyControlID = m_GUIWapper.GetControlID(boneBodyHashCode, FocusType.Passive);
            int boneHeadControlID = m_GUIWapper.GetControlID(boneHeadHashCode, FocusType.Passive);

            var headEnd = position + (endPosition - position).normalized * GetHandleSize(position) * 0.25f;

            m_GUIWapper.LayoutControl(boneBodyControlID, m_GUIWapper.DistanceToSegment(headEnd, endPosition));

            if (m_GUIWapper.IsControlNearest(boneBodyControlID))
            {
                m_HoveredBoneBodyControlID = boneBodyControlID;
                m_HoveredBoneTransform = boneTransform;
            }

            m_GUIWapper.LayoutControl(boneHeadControlID, m_GUIWapper.DistanceToSegment(position, headEnd));

            if (m_GUIWapper.IsControlNearest(boneHeadControlID))
            {
                m_HoveredBoneHeadControlID = boneHeadControlID;
                m_HoveredBoneTransform = boneTransform;
            }
        }

        public bool DoSelection(Transform boneTransform, out BoneGizmoSelectionMode mode)
        {
            mode = BoneGizmoSelectionMode.Single;

            if(m_GUIWapper.isShiftDown || m_GUIWapper.isActionKeyDown)
                mode = BoneGizmoSelectionMode.Toggle;

            return TryActivateAction(boneTransform, BoneGizmoAction.Select);
        }

        public bool DoBoneRotation(Transform boneTransform, out float deltaAngle)
        {
            deltaAngle = 0f;

            Transform pivotTransform;
            if (!FindPivotTransform(boneTransform, out pivotTransform))
                pivotTransform = boneTransform;

            if (TryActivateAction(boneTransform, BoneGizmoAction.Rotate))
            {
                m_RotateBoneControlID = m_HoveredBoneBodyControlID;
                m_RotationPivot = pivotTransform.position;
                m_SliderPosition = m_GUIWapper.GUIToWorld(m_GUIWapper.mousePosition, pivotTransform.forward, pivotTransform.position);
            }

            if (m_HotBoneTransform != boneTransform)
                return false;

            Vector3 newPosition;
            if (m_GUIWapper.DoSlider(m_RotateBoneControlID, m_SliderPosition, pivotTransform.forward, pivotTransform.up, pivotTransform.right, out newPosition))
            {
                var fromVector = m_SliderPosition - m_RotationPivot;
                var toVector = newPosition - m_RotationPivot;
                deltaAngle = Vector3.SignedAngle(fromVector, toVector, pivotTransform.forward);

                m_SliderPosition = newPosition;
                return true;
            }

            return false;
        }

        public bool DoBonePosition(Transform boneTransform, out Vector3 deltaPosition)
        {
            deltaPosition = Vector3.zero;

            if (TryActivateAction(boneTransform, BoneGizmoAction.Move))
            {
                m_MoveBoneControlID = m_HoveredBoneHeadControlID;
                m_SliderPosition = m_GUIWapper.GUIToWorld(m_GUIWapper.mousePosition, boneTransform.forward, boneTransform.position);
            }

            if (m_HotBoneTransform != boneTransform)
                return false;

            Vector3 newPosition;
            if(m_GUIWapper.DoSlider(m_MoveBoneControlID, m_SliderPosition, m_HotBoneTransform.forward, m_HotBoneTransform.up, m_HotBoneTransform.right, out newPosition))
            {
                deltaPosition = newPosition - m_SliderPosition;
                m_SliderPosition = newPosition;

                return true;
            }

            return false;
        }

        private bool IsDescendentOf(Transform transform, Transform ancestor)
        {
            Debug.Assert(transform != null, "Transform is null");

            Transform currentParent = transform.parent;

            while (currentParent)
            {
                if (currentParent == ancestor)
                    return true;

                currentParent = currentParent.parent;
            }

            return false;
        }

        private bool FindPivotTransform(Transform transform, out Transform selectedTransform)
        {
            selectedTransform = null;

            foreach(var t in Selection.transforms)
            {
                if(IsDescendentOf(transform, t))
                {
                    selectedTransform = t;
                    return true;
                }
            }

            return false;
        }

        private bool TryActivateAction(Transform boneTransform, BoneGizmoAction action)
        {
            if (IsActionTriggering(boneTransform, action))
            {
                m_HotBoneTransform = m_HoveredBoneTransform;
                return true;
            }

            return false;
        }

        public bool IsActionActive(Transform boneTransform, BoneGizmoAction action)
        {
            if (m_GUIWapper.IsViewToolActive() || !IsActionHot(BoneGizmoAction.None) || m_HoveredBoneTransform != boneTransform)
                return action == BoneGizmoAction.None;

            if (action == BoneGizmoAction.Select)
                return m_GUIWapper.IsControlNearest(m_HoveredBoneHeadControlID) || m_GUIWapper.IsControlNearest(m_HoveredBoneBodyControlID);

            if (action == BoneGizmoAction.Rotate)
                return m_GUIWapper.IsControlNearest(m_HoveredBoneBodyControlID);

            if (action == BoneGizmoAction.Move)
                return m_GUIWapper.IsControlNearest(m_HoveredBoneHeadControlID);

            if (action == BoneGizmoAction.None)
                return true;

            return false;
        }

        public bool IsActionHot(Transform boneTransform, BoneGizmoAction action)
        {
            if (m_HotBoneTransform != boneTransform)
                return false;

            return IsActionHot(action);
        }

        public bool IsActionHot(BoneGizmoAction action)
        {
            if (action == BoneGizmoAction.None)
                return m_GUIWapper.IsControlHot(0);

            if (action == BoneGizmoAction.Rotate)
                return m_GUIWapper.IsControlHot(m_RotateBoneControlID);

            if (action == BoneGizmoAction.Move)
                return m_GUIWapper.IsControlHot(m_MoveBoneControlID);

            return false;
        }

        public void DrawBone(Transform boneTransform, float length, float alpha)
        {
            Debug.Assert(m_GUIWapper.IsRepainting());

            if (!IsBoneVisible(boneTransform, length, alpha))
                return;

            var boneColor = Color.white.AlphaMultiplied(alpha);
            var boneNodeColor = Color.gray.AlphaMultiplied(alpha);
            var hoveredBoneColor = Handles.preselectionColor.AlphaMultiplied(alpha);
            var selectedBoneColor = Handles.selectedColor.AlphaMultiplied(alpha);

            Handles.matrix = boneTransform.localToWorldMatrix;
            var colorTmp = Handles.color;

            var position = Vector3.zero;
            var endPosition = Vector3.right * length;
            var radius = GetBoneRadius(position, endPosition);
            var color = boneColor;

            if (IsSelected(boneTransform))
                color = selectedBoneColor;

            if (IsActionActive(boneTransform, BoneGizmoAction.Rotate))
                color = hoveredBoneColor;

            Handles.color = ToActiveColorSpace(color);
            CommonDrawingUtility.DrawSolidArc(position, Vector3.back, Vector3.Cross(endPosition - position, Vector3.forward), 180f, radius, 8);
            CommonDrawingUtility.DrawLine(position, endPosition, Vector3.back, radius * 2f, radius * 0.1f);

            if (IsActionActive(boneTransform, BoneGizmoAction.Move))
            {
                Handles.color = ToActiveColorSpace(hoveredBoneColor);
                CommonDrawingUtility.DrawSolidArc(position, Vector3.back, Vector3.Cross(endPosition - position, Vector3.forward), 360f, radius, 16);
            }

            Handles.color = ToActiveColorSpace(boneNodeColor);
            CommonDrawingUtility.DrawSolidArc(position, Vector3.back, Vector3.Cross(endPosition - position, Vector3.forward), 360f, radius * 0.55f, 16);

            Handles.color = colorTmp;
            Handles.matrix = Matrix4x4.identity;
        }

        public void DrawParentBoneLink(Transform boneTransform, float parentLength, Color color)
        {
            Debug.Assert(boneTransform != null);
            Debug.Assert(boneTransform.parent != null);

            var parentEndPoint = boneTransform.parent.TransformPoint(Vector3.right * parentLength);
            var length = (parentEndPoint - boneTransform.position).sqrMagnitude;

            if (length <= 0.0001f)
                return;

            if (!IsVisible(boneTransform.position) && !IsVisible(boneTransform.parent.position))
                return;

            Handles.matrix = Matrix4x4.identity;
            Handles.color = color;
            Handles.DrawLine(boneTransform.position, boneTransform.parent.position);
        }

        public bool IsBoneVisible(Transform boneTransform, float length, float alpha)
        {
            if (alpha == 0f || !IsLayerVisible(boneTransform))
                return false;

            var position = boneTransform.position;
            var endPosition = boneTransform.TransformPoint(Vector3.right * length);

            return IsVisible(position) || IsVisible(endPosition);
        }

        public bool IsVisible(Vector3 position)
        {
            Debug.Assert(Camera.current != null);

            var screenPos = HandleUtility.GUIPointToScreenPixelCoordinate(HandleUtility.WorldToGUIPoint(position));
            if (screenPos.x < 0f || screenPos.x > Camera.current.pixelWidth || screenPos.y < 0f || screenPos.y > Camera.current.pixelHeight)
                return false;

            return true;
        }

        public float GetHandleSize(Vector3 position)
        {
            return m_GUIWapper.GetHandleSize(position);
        }

        private bool IsActionTriggering(Transform boneTransform, BoneGizmoAction action)
        {
            if (!IsActionActive(boneTransform, action))
                return false;

            if (action == BoneGizmoAction.Select)
                return m_GUIWapper.IsMouseDown(0);

            if (action == BoneGizmoAction.Rotate)
                return m_GUIWapper.IsMouseDown(0);

            if (action == BoneGizmoAction.Move)
                return m_GUIWapper.IsMouseDown(0);

            return false;
        }

        private float GetBoneRadius(Vector3 position, Vector3 endPosition)
        {
            float length = (endPosition - position).magnitude;
            return kBoneScale * Mathf.Min(kBoneLenghtRatio * length, GetHandleSize(position));
        }

        private bool IsSelected(Transform boneTransform)
        {
            return Selection.Contains(boneTransform.gameObject);
        }

        private bool IsLayerVisible(Transform boneTransform)
        {
            return (Tools.visibleLayers & 1 << boneTransform.gameObject.layer) != 0;
        }

        private bool IsLayerLocked(Transform boneTransform)
        {
            return (Tools.lockedLayers & 1 << boneTransform.gameObject.layer) != 0;
        }

        private static Color ToActiveColorSpace(Color color)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }
    }
}
