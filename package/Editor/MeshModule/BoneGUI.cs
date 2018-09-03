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
    internal class BoneGUI
    {
        private const int kNiceColorCount = 6;

        public SpriteMeshData spriteMeshdata { get; set; }
        public float boneOpacity { get; set; }
        public ISelection selection { get; set; }
        public IUndoObject undoObject { get; set; }
        public int defaultControlID { get; set; }
        public int hoveredBone { get { return m_HoveredBone; } }

        public BoneGUI()
        {
            boneOpacity = 1f;
        }

        public void DoBoneGUI()
        {
            if (Event.current.type == EventType.Layout)
                m_HoveredBone = -1;

            if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == defaultControlID)
                m_HoveredBoneControlID = -1;

            DrawBones();
            LayoutBones();
            HandleSelectBone();
        }

        private Vector2 GetBoneTip(SpriteBone bone)
        {
            return bone.position + bone.rotation * Vector3.right * bone.length;
        }

        private void LayoutBones()
        {
            for (int i = 0; i < spriteMeshdata.bones.Count; ++i)
            {
                SpriteBone spriteBone = spriteMeshdata.bones[i];

                int controlID = GUIUtility.GetControlID("Bone".GetHashCode(), FocusType.Keyboard);

                if (Event.current.type == EventType.Layout)
                {
                    Vector2 v1 = spriteBone.position;
                    Vector2 v2 = GetBoneTip(spriteBone);

                    HandleUtility.AddControl(controlID, MeshModuleUtility.DistanceToSegment(v1, v2));

                    if (HandleUtility.nearestControl == controlID)
                    {
                        m_HoveredBone = i;
                        m_HoveredBoneControlID = controlID;
                    }
                }
            }
        }

        private void HandleSelectBone()
        {
            if (HandleUtility.nearestControl == m_HoveredBoneControlID && !selection.IsSelected(m_HoveredBone)
                && Event.current.type == EventType.MouseDown && Event.current.button == 0 && !Event.current.alt)
            {
                undoObject.RegisterCompleteObjectUndo("Select Bone");

                selection.Clear();
                selection.Select(m_HoveredBone, true);

                Event.current.Use();
            }
        }

        private void DrawBones()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            for (int i = 0; i < spriteMeshdata.bones.Count; i++)
            {
                SpriteBone boneNode = spriteMeshdata.bones[i];

                Vector2 bonePos = (Vector2)(boneNode.position);
                Vector2 bonetipPos = GetBoneTip(boneNode);

                Color outlineColor = Color.black;
                outlineColor.a = boneOpacity * boneOpacity;

                if (selection.IsSelected(i) ||
                    (GUIUtility.hotControl == 0 && !Event.current.alt && m_HoveredBone == i && HandleUtility.nearestControl == m_HoveredBoneControlID))
                    outlineColor = Color.yellow;

                Color boneColor = CommonDrawingUtility.CalculateNiceColor(i, kNiceColorCount);
                boneColor.a = boneOpacity;

                Color nodeColor = Color.black;
                nodeColor.a = boneOpacity;

                BoneDrawingUtility.DrawBoneOutline(bonePos, bonetipPos, outlineColor);
                BoneDrawingUtility.DrawBoneBody(bonePos, bonetipPos, boneColor);
                BoneDrawingUtility.DrawBoneNodeOutline(bonePos, boneColor);
                BoneDrawingUtility.DrawBoneNode(bonePos, nodeColor.AlphaMultiplied(0.5f));
            }
        }

        private int m_HoveredBone = -1;
        private int m_HoveredBoneControlID = -1;
    }
}
