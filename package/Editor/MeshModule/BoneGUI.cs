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
        public ISelection selection { get; set; }
        public IUndoObject undoObject { get; set; }

        public void DoBoneGUI()
        {
            if (Event.current.type == EventType.Layout)
                m_HoveredBone = -1;

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
                        m_HoveredBone = i;
                }
            }
        }

        private void HandleSelectBone()
        {
            if (m_HoveredBone != -1 && !selection.IsSelected(m_HoveredBone)
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
                if (selection.IsSelected(i) ||
                    (GUIUtility.hotControl == 0 && !Event.current.alt && m_HoveredBone == i))
                    outlineColor = Color.yellow;

                BoneDrawingUtility.DrawBoneOutline(bonePos, bonetipPos, outlineColor);
                BoneDrawingUtility.DrawBoneBody(bonePos, bonetipPos, CommonDrawingUtility.CalculateNiceColor(i, kNiceColorCount));
                BoneDrawingUtility.DrawBoneNodeOutline(bonePos, CommonDrawingUtility.CalculateNiceColor(i, kNiceColorCount));
                BoneDrawingUtility.DrawBoneNode(bonePos, Color.black.AlphaMultiplied(0.5f));
            }
        }

        private int m_HoveredBone = -1;
    }
}
