using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.Animation
{
    [InitializeOnLoad]
    internal static class BoneGizmo
    {
        static readonly float kBoneScale = 0.01f;

        static BoneGizmo()
        {
            // TODO : Remove when scene gizmo drawing performance regression is fix.
            SceneView.onSceneGUIDelegate += delegate(SceneView sv)
                {
                    GameObject go = Selection.activeGameObject;
                    if (go != null)
                    {
                        var ss = go.GetComponentInParent<SpriteSkin>();
                        if (ss != null)
                            DrawBoneGizmo(ss, GizmoType.InSelectionHierarchy);
                    }
                };
        }

        //TODO : Re-enable when scene gizmo drawing performance regression is fix.
        //TODO : DrawGizmo does not have events and will not be able to support bone picking
        // [DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
        static void DrawBoneGizmo(SpriteSkin skin, GizmoType gizmoType)
        {
            if (skin.rootBone != null)
            {
                var bones = skin.GetComponent<SpriteRenderer>().sprite.GetBones();
                var l = bones.First().length;

                RecurseBone(skin.rootBone, bones, null, l);
            }
        }

        static void RecurseBone(Transform boneTransform, IList<SpriteBone> bones, Vector3? parentEnd, float length)
        {
            var startPos = boneTransform.position;
            var endPos = startPos + boneTransform.rotation * (Vector3.right * boneTransform.lossyScale.y * length);
            if (parentEnd.HasValue)
                DrawLink(parentEnd.Value, startPos, Color.white);
            DrawBone(startPos, endPos, (Selection.activeGameObject != null && Selection.activeGameObject.transform == boneTransform) ? Color.yellow : Color.white);

            // Handle selection of bone transforms on mouse click
            int controlID = GUIUtility.GetControlID("BoneGizmo".GetHashCode(), FocusType.Passive);
            var distance = HandleUtility.DistancePointLine(Event.current.mousePosition,
                HandleUtility.WorldToGUIPoint(startPos), HandleUtility.WorldToGUIPoint(endPos));
            if (IsLayout())
                HandleUtility.AddControl(controlID, distance);

            if (IsMousePick() && HandleUtility.nearestControl == controlID)
            {
                Selection.activeGameObject = boneTransform.gameObject;
                // Switch to the Transform tool to manipulate the transform fsor animation purposes
                Tools.current = Tool.Transform;
                Event.current.Use();
            }

            if (boneTransform.childCount > 0)
            {
                foreach (Transform child in boneTransform)
                {
                    var childSpriteSkin = child.GetComponent<SpriteSkin>();
                    if (childSpriteSkin == null)
                    {
                        if (bones.Any(x => x.name.Equals(child.name)))
                        {
                            var l = bones.First(x => x.name == child.name).length;
                            RecurseBone(child, bones, endPos, l);
                        }
                    }
                }
            }
        }

        static void DrawBone(Vector3 startPoint, Vector3 endPoint, Color color)
        {
            float scale = (endPoint - startPoint).magnitude * kBoneScale;
            BoneDrawingUtility.DrawBoneOutline(startPoint, endPoint, Color.black, scale);
            BoneDrawingUtility.DrawBoneBody(startPoint, endPoint, color, scale);
            BoneDrawingUtility.DrawBoneNodeOutline(startPoint, color, scale);
            BoneDrawingUtility.DrawBoneNode(startPoint, Color.black, scale);
        }

        static void DrawLink(Vector3 startPoint, Vector3 endPoint, Color color)
        {
            CommonDrawingUtility.DrawLine(startPoint, endPoint, Vector3.back, kBoneScale, kBoneScale, color);
        }

        static bool IsLayout()
        {
            var evt = Event.current;
            if (evt.type == EventType.Layout)
            {
                return true;
            }
            return false;
        }

        static bool IsMousePick()
        {
            var evt = Event.current;
            if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                return true;
            }
            return false;
        }
    }
}
