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
    internal class BindPoseController
    {
        public delegate void SkinPreviewDelegate();

        public SkinPreviewDelegate onSkinPreviewChanged;

        private const int kNiceColorCount = 6;

        public IBindPoseView bindPoseView { get; set; }
        public SpriteMeshData spriteMeshData { get; set; }
        public ISelection selection { get; set; }
        public IUndoObject undoObject { get; set; }
        public Matrix4x4[] localToWorldMatrices { get { return m_LocalToWorldMatrices; } }
        public bool inSkinningPreview { get { return m_PreviewChanged && (bindPoseView.IsActionHot(BindPoseAction.RotateBone) || bindPoseView.IsActionHot(BindPoseAction.MoveBone)); } }
        bool m_PreviewChanged = false;
        private Matrix4x4[] m_LocalToWorldMatrices = null;

        public void DoBoneGUI()
        {
            bindPoseView.selection = selection;

            bindPoseView.SetupLayout();

            LayoutBindPose();
            HandleSelectBone();
            HandleRotateBone();
            HandleMoveBone();

            if (bindPoseView.IsRepainting())
                DrawBindPose();
        }

        private void LayoutBindPose()
        {
            for (int i = 0; i < spriteMeshData.bones.Count; ++i)
            {
                var spriteBone = spriteMeshData.bones[i];

                bindPoseView.LayoutBone(spriteBone.position, spriteBone.endPosition, i);
            }
        }

        private void HandleSelectBone()
        {
            if (bindPoseView.DoSelectBone())
            {
                undoObject.RegisterCompleteObjectUndo("Select Bone");
                undoObject.IncrementCurrentGroup();

                selection.Clear();
                selection.Select(bindPoseView.hoveredBone, true);
            }
        }

        private void HandleRotateBone()
        {
            if (bindPoseView.IsActionTriggering(BindPoseAction.RotateBone))
            {
                m_PreviewChanged = false;
                undoObject.RegisterCompleteObjectUndo("Rotate Bone");
                UpdateBones();
            }

            if (bindPoseView.IsActionFinishing(BindPoseAction.RotateBone))
            {
                m_PreviewChanged = true;
                undoObject.RevertAllInCurrentGroup();
                UpdateBones();
            }

            Vector2 lookAtPosition;
            if (bindPoseView.DoRotateBone(out lookAtPosition))
            {
                if (selection.single == -1)
                    throw new ArgumentException("no bone or multiple bone selected");

                var spriteBoneData = spriteMeshData.bones[selection.single];
                var localLookAtPosition = localToWorldMatrices[selection.single].inverse.MultiplyPoint3x4(lookAtPosition);
                spriteBoneData.localRotation = MeshModuleUtility.NormalizeQuaternion(spriteBoneData.localRotation * Quaternion.FromToRotation(Vector2.right, localLookAtPosition));
                m_PreviewChanged = true;
                UpdateBones();
            }
        }

        private void HandleMoveBone()
        {
            if (bindPoseView.IsActionTriggering(BindPoseAction.MoveBone))
            {
                m_PreviewChanged = false;
                undoObject.RegisterCompleteObjectUndo("Move Bone");
                UpdateBones();
            }

            if (bindPoseView.IsActionFinishing(BindPoseAction.MoveBone))
            {
                m_PreviewChanged = true;
                undoObject.RevertAllInCurrentGroup();
                UpdateBones();
            }

            Vector2 worldPosition;
            if (bindPoseView.DoMoveBone(out worldPosition))
            {
                if (selection.single == -1)
                    throw new ArgumentException("no bone or multiple bone selected");

                var spriteBoneData = spriteMeshData.bones[selection.single];
                var parentMatrix = Matrix4x4.identity;

                if (spriteBoneData.parentId == -1)
                    parentMatrix = spriteMeshData.CalculateRootMatrix();
                else
                    parentMatrix = localToWorldMatrices[spriteBoneData.parentId];

                spriteBoneData.localPosition = parentMatrix.inverse.MultiplyPoint(worldPosition);
                m_PreviewChanged = true;
                UpdateBones();
            }
        }

        private void UpdateBones()
        {
            MeshModuleUtility.UpdateLocalToWorldMatrices(spriteMeshData.bones, spriteMeshData.CalculateRootMatrix(), ref m_LocalToWorldMatrices);
            spriteMeshData.UpdateSpriteBoneDataWorldPosition(m_LocalToWorldMatrices);

            if (onSkinPreviewChanged != null)
                onSkinPreviewChanged.Invoke();
        }

        private void DrawBindPose()
        {
            Debug.Assert(bindPoseView.IsRepainting());

            for (int i = 0; i < spriteMeshData.bones.Count; i++)
            {
                var spriteBoneData = spriteMeshData.bones[i];
                var color = CommonDrawingUtility.CalculateNiceColor(i, kNiceColorCount);

                bindPoseView.DrawBone(spriteBoneData.position, spriteBoneData.endPosition, selection.IsSelected(i), bindPoseView.hoveredBone == i, color);
            }
        }
    }
}
