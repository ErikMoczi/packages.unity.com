using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D;
using UnityEditor.Experimental.U2D.Animation;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class BindPoseView : IBindPoseView
    {
        private static readonly int kBoneBodyHashCode = "BoneBody".GetHashCode();
        private static readonly int kBoneNodeHashCode = "BoneNode".GetHashCode();
        const float kBoneRadius = 5f;

        public ISelection selection { get; set; }
        public int defaultControlID { get; set; }
        public int hoveredBone { get { return m_HoveredBone; } }
        public float boneOpacity { get; set; }
        public Vector2 mouseWorldPosition
        {
            get { return guiWrapper.GUIToWorld(guiWrapper.mousePosition); }
        }
        
        private IGUIWrapper guiWrapper { get; set; }
        private int m_RotateBoneControlID = -1;
        private int m_MoveBoneControlID = -1;
        private int m_HoveredBone = -1;
        private int m_HoveredBoneBodyControlID = -1;
        private int m_HoveredBoneNodeControlID = -1;
        private Vector2 m_HoveredBonePosition;
        private Vector2 m_SliderPosition;

        public BindPoseView(IGUIWrapper gw)
        {
            guiWrapper = gw;
            boneOpacity = 1f;
        }

        public void SetupLayout()
        {
            if (guiWrapper.eventType == EventType.Layout)
            {
                m_HoveredBone = -1;
                m_HoveredBoneBodyControlID = -1;

                if (guiWrapper.IsControlHot(0))
                {
                    m_RotateBoneControlID = -1;
                }
            }
        }

        public void LayoutBone(Vector2 startPosition, Vector2 endPosition, int index)
        {
            int bodyControlID = guiWrapper.GetControlID(kBoneBodyHashCode, FocusType.Passive);
            int nodeControlID = guiWrapper.GetControlID(kBoneNodeHashCode, FocusType.Passive);

            if (guiWrapper.eventType == EventType.Layout)
            {
                float distanceBody = guiWrapper.DistanceToSegment(startPosition, endPosition);
                float distanceNode = guiWrapper.DistanceToCircle(startPosition, kBoneRadius);

                guiWrapper.LayoutControl(bodyControlID, distanceBody);

                if (guiWrapper.IsControlNearest(bodyControlID))
                {
                    m_HoveredBone = index;
                    m_HoveredBonePosition = startPosition;
                    m_HoveredBoneBodyControlID = bodyControlID;
                }

                guiWrapper.LayoutControl(nodeControlID, distanceNode);

                if (guiWrapper.IsControlNearest(nodeControlID))
                {
                    m_HoveredBone = index;
                    m_HoveredBonePosition = startPosition;
                    m_HoveredBoneNodeControlID = nodeControlID;
                }
            }
        }

        public bool DoSelectBone()
        {
            if (IsActionTriggering(BindPoseAction.SelectBone))
            {
                guiWrapper.Repaint();
                return true;
            }

            return false;
        }

        public bool DoRotateBone(out Vector2 lookAtPosition)
        {
            lookAtPosition = Vector2.zero;

            if (IsActionTriggering(BindPoseAction.RotateBone))
            {
                m_RotateBoneControlID = m_HoveredBoneBodyControlID;
                m_SliderPosition = mouseWorldPosition;
            }

            Vector2 newPosition;
            if (guiWrapper.DoSlider(m_RotateBoneControlID, m_SliderPosition, out newPosition))
            {
                lookAtPosition = newPosition;
                m_SliderPosition = newPosition;

                return true;
            }

            return false;
        }

        public bool DoMoveBone(out Vector2 worldPosition)
        {
            worldPosition = Vector2.zero;

            if (IsActionTriggering(BindPoseAction.MoveBone))
            {
                m_MoveBoneControlID = m_HoveredBoneNodeControlID;
                m_SliderPosition = m_HoveredBonePosition;
            }

            Vector2 newPosition;
            if (guiWrapper.DoSlider(m_MoveBoneControlID, m_SliderPosition, out newPosition))
            {
                worldPosition = newPosition;
                m_SliderPosition = newPosition;

                return true;
            }

            return false;
        }

        public bool IsActionActive(BindPoseAction action)
        {
            if (guiWrapper.isAltDown || !guiWrapper.IsControlHot(0))
                return false;

            if (action == BindPoseAction.None)
                return guiWrapper.IsControlNearest(defaultControlID);

            if (action == BindPoseAction.RotateBone)
                return guiWrapper.IsControlNearest(m_HoveredBoneBodyControlID);

            if (action == BindPoseAction.MoveBone)
                return guiWrapper.IsControlNearest(m_HoveredBoneNodeControlID);

            if (action == BindPoseAction.SelectBone)
                return (guiWrapper.IsControlNearest(m_HoveredBoneBodyControlID) || guiWrapper.IsControlNearest(m_HoveredBoneNodeControlID)) && !selection.IsSelected(hoveredBone);

            return false;
        }

        public bool IsActionHot(BindPoseAction action)
        {
            if (action == BindPoseAction.None)
                return guiWrapper.IsControlHot(0);

            if (action == BindPoseAction.RotateBone)
                return guiWrapper.IsControlHot(m_RotateBoneControlID);

            if (action == BindPoseAction.MoveBone)
                return guiWrapper.IsControlHot(m_MoveBoneControlID);

            return false;
        }

        public bool IsActionTriggering(BindPoseAction action)
        {
            if (!IsActionActive(action))
                return false;

            if (action == BindPoseAction.MoveBone)
                return guiWrapper.IsMouseDown(0);

            if (action == BindPoseAction.RotateBone)
                return guiWrapper.IsMouseDown(0);

            if (action == BindPoseAction.MoveBone)
                return guiWrapper.IsMouseDown(0);

            if (action == BindPoseAction.SelectBone)
                return guiWrapper.IsMouseDown(0);

            return false;
        }

        public bool IsActionFinishing(BindPoseAction action)
        {
            if (guiWrapper.IsEventOutsideWindow())
                return true;

            if (!IsActionHot(action))
                return false;

            if (action == BindPoseAction.RotateBone)
                return guiWrapper.IsMouseUp(0);

            if (action == BindPoseAction.MoveBone)
                return guiWrapper.IsMouseUp(0);

            return false;
        }

        public bool IsRepainting()
        {
            return guiWrapper.IsRepainting();
        }

        public void DrawBone(Vector2 position, Vector2 endPosition, bool isSelected, bool isHovered, Color color)
        {
            color.a = boneOpacity;

            Color outlineColor = Color.black;
            outlineColor.a = boneOpacity * boneOpacity;

            if (isSelected || (isHovered && IsActionActive(BindPoseAction.SelectBone)))
                outlineColor = Color.yellow;


            Color nodeColor = Color.black;

            if (isHovered && IsActionActive(BindPoseAction.MoveBone) ||
                isSelected && IsActionHot(BindPoseAction.MoveBone))
                nodeColor = Color.yellow;

            nodeColor.a = boneOpacity;

            BoneDrawingUtility.DrawBoneOutline(position, endPosition, outlineColor);
            BoneDrawingUtility.DrawBoneBody(position, endPosition, color);
            BoneDrawingUtility.DrawBoneNodeOutline(position, color);
            BoneDrawingUtility.DrawBoneNode(position, nodeColor.AlphaMultiplied(0.5f));
        }
    }
}
