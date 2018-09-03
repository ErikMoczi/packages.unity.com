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
    public enum BindPoseAction
    {
        None,
        SelectBone,
        RotateBone,
        MoveBone
    }

    public interface IBindPoseView
    {
        ISelection selection { get; set; }
        int defaultControlID { get; set; }
        int hoveredBone { get; }
        float boneOpacity { get; set; }
        void SetupLayout();
        void LayoutBone(Vector2 startPosition, Vector2 endPosition, int index);
        bool DoSelectBone();
        bool DoRotateBone(out Vector2 lookAtPosition);
        bool DoMoveBone(out Vector2 worldPosition);
        bool IsActionActive(BindPoseAction action);
        bool IsActionHot(BindPoseAction action);
        bool IsActionTriggering(BindPoseAction action);
        bool IsActionFinishing(BindPoseAction action);
        bool IsRepainting();
        void DrawBone(Vector2 position, Vector2 endPosition, bool selected, bool isHovered, Color color);
    }
}
