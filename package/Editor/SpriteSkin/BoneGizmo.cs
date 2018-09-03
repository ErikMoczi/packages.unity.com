using System.Linq;
using UnityEngine;
using UnityEditor;
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
        //[DrawGizmo(GizmoType.Active | GizmoType.NonSelected)]
        static void DrawBoneGizmo(SpriteSkin skin, GizmoType gizmoType)
        {
            if (skin.rootBone != null)
            {
                var bones = skin.GetComponent<SpriteRenderer>().sprite.GetBones();
                var l = bones.First().length;

                RecurseBone(skin, skin.rootBone, null, l, Color.white);
            }
        }

        static void RecurseBone(SpriteSkin skin, Transform boneTransform, Vector3? parentEnd, float length, Color color, int depth = 0)
        {
            var startPos = boneTransform.position;
            var endPos = startPos + boneTransform.rotation * (Vector3.right * boneTransform.lossyScale.y * length);
            if (parentEnd.HasValue)
                DrawLink(parentEnd.Value, startPos, color);
            DrawBone(startPos, endPos, color, depth);

            if (boneTransform.childCount > 0)
            {
                foreach (Transform child in boneTransform)
                {
                    var childSpriteSkin = child.GetComponent<SpriteSkin>();
                    if (childSpriteSkin == null)
                    {
                        var bones = skin.GetComponent<SpriteRenderer>().sprite.GetBones();
                        if (bones.Any(x => x.name.Equals(child.name)))
                        {
                            var l = bones.First(x => x.name == child.name).length;
                            RecurseBone(skin, child, endPos, l, color, depth + 1);
                        }
                    }
                }
            }
        }

        static void DrawBone(Vector3 startPoint, Vector3 endPoint, Color color, int depth)
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
    }
}
