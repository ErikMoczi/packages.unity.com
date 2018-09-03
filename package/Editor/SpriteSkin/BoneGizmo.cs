using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.Animation
{
    //Make sure Bone Gizmo registers callbacks before anyone else
    [InitializeOnLoad]
    internal class BoneGizmoInitializer
    {
        static BoneGizmoInitializer()
        {
            BoneGizmo.instance.Initialize();
        }
    }

    internal class BoneGizmo : ScriptableSingleton<BoneGizmo>
    {
        readonly float kBoneScale = 0.1f;
        readonly float kBoneLenghtRatio = 0.5f;
        private const float kFadeStart = 0.75f;
        private const float kFadeEnd = 1.75f;
        readonly int boneHashCode = "Bone".GetHashCode();

        private List<SpriteSkin> m_SkinComponents = new List<SpriteSkin>();
        private Dictionary<Transform, Vector2> m_CachedBones = new Dictionary<Transform, Vector2>();
        private HashSet<Transform> m_SelectionRoots = new HashSet<Transform>();

        private void RegisterCallbacks()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChange;
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        private void UnregisterCallbacks()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChange;
            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        private void OnSelectionChanged()
        {
            m_SelectionRoots.Clear();

            foreach (var selectedTransform in Selection.transforms)
                m_SelectionRoots.Add(selectedTransform.root);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            PrepareBones();
            DoBoneGUI();
        }

        [Callbacks.DidReloadScripts]
        internal void Initialize()
        {
            UnregisterCallbacks();
            RegisterCallbacks();
            OnHierarchyChange();
        }

        private void OnHierarchyChange()
        {
            m_SkinComponents.Clear();
            m_SkinComponents.AddRange(GameObject.FindObjectsOfType<SpriteSkin>());
        }

        private void PrepareBones()
        {
            if (Event.current.type != EventType.Layout)
                return;

            m_CachedBones.Clear();

            foreach (var skinComponent in m_SkinComponents)
            {
                if (skinComponent == null)
                    continue;

                if (m_SelectionRoots.Contains(skinComponent.transform.root))
                    PrepareBones(skinComponent);
            }
        }

        private void PrepareBones(SpriteSkin spriteSkin)
        {
            if (Event.current.type != EventType.Layout)
                return;

            if (!spriteSkin.isValid)
                return;

            var sprite = spriteSkin.spriteRenderer.sprite;
            var boneTransforms = spriteSkin.boneTransforms;
            var spriteBones = spriteSkin.spriteBones;
            var color = FadeFromSpriteSkin(Color.white, spriteSkin, spriteBones);

            if (color.a == 0f)
                return;

            if (Event.current.type == EventType.Layout)
            {
                for (int i = 0; i < boneTransforms.Length; ++i)
                {
                    var boneTransform = boneTransforms[i];

                    if (boneTransform == null || m_CachedBones.ContainsKey(boneTransform))
                        continue;

                    var bone = spriteBones[i];
                    var position = boneTransform.position;
                    var endPosition = boneTransform.TransformPoint(Vector3.right * bone.length);

                    if (IsVisible(position) || IsVisible(endPosition))
                        m_CachedBones.Add(boneTransform, new Vector2(bone.length, color.a));
                }
            }
        }

        private void DoBoneGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                foreach (var bone in m_CachedBones)
                {
                    var boneTransform = bone.Key;
                    var value = bone.Value;
                    var alpha = value.y;

                    if (HasParentBone(boneTransform))
                    {
                        var parentLength = m_CachedBones[boneTransform.parent];
                        var parentEndPoint = boneTransform.parent.TransformPoint(Vector3.right * parentLength);
                        var length = (parentEndPoint - boneTransform.position).magnitude;
                        var color = Color.white;
                        color.a = alpha;

                        if (length > 0.01f)
                            DrawParentLink(boneTransform.position, boneTransform.parent.position, color);
                    }
                }
            }

            foreach (var bone in m_CachedBones)
            {
                int controlID = GUIUtility.GetControlID(boneHashCode, FocusType.Passive);

                var boneTransform = bone.Key;
                var value = bone.Value;
                var length = value.x;
                var alpha = value.y;
                var position = boneTransform.position;
                var endPosition = boneTransform.TransformPoint(Vector3.right * length);

                LayoutBone(controlID, position, endPosition);

                if (DoBoneSelection(controlID))
                {
                    Selection.activeGameObject = boneTransform.gameObject;
                    Tools.current = Tool.Transform;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    var color = Color.white;

                    if (IsBoneHovered(controlID))
                        color = Handles.preselectionColor;

                    if (IsSelected(boneTransform))
                        color = Handles.selectedColor;

                    color.a = alpha;

                    Handles.matrix = boneTransform.localToWorldMatrix;
                    DrawBone(Vector3.zero, Vector3.right * length, color);
                }
            }

            Handles.matrix = Matrix4x4.identity;
        }

        private void LayoutBone(int controlID, Vector3 position, Vector3 endPosition)
        {
            EventType eventType = Event.current.GetTypeForControl(controlID);

            if (eventType == EventType.Layout)
            {
                var distance = HandleUtility.DistancePointLine(Event.current.mousePosition,
                        HandleUtility.WorldToGUIPoint(position), HandleUtility.WorldToGUIPoint(endPosition));

                HandleUtility.AddControl(controlID, distance);
            }
        }

        private bool IsBoneHovered(int controlID)
        {
            return HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0;
        }

        private bool DoBoneSelection(int controlID)
        {
            EventType eventType = Event.current.GetTypeForControl(controlID);

            if (IsBoneHovered(controlID) && eventType == EventType.MouseDown && Event.current.button == 0 && Tools.current == Tool.Rect)
                Event.current.Use();

            if (IsBoneHovered(controlID) && eventType == EventType.MouseUp && Event.current.button == 0)
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        private bool HasParentBone(Transform transform)
        {
            return m_CachedBones.ContainsKey(transform.parent);
        }

        private void DrawBone(Vector3 position, Vector3 endPosition, Color color)
        {
            var alpha = color.a;
            var colorTmp = Handles.color;
            Handles.color = color;

            float radius = GetBoneRadius(position, endPosition);
            CommonDrawingUtility.DrawSolidArc(position, Vector3.back, Vector3.Cross(endPosition - position, Vector3.forward), 180f, radius, 8);
            CommonDrawingUtility.DrawLine(position, endPosition, Vector3.back, radius * 2f, radius * 0.1f);

            color = Color.gray;
            color.a = alpha;
            Handles.color = color;
            CommonDrawingUtility.DrawSolidArc(position, Vector3.back, Vector3.Cross(endPosition - position, Vector3.forward), 360f, radius * 0.55f, 16);

            Handles.color = colorTmp;
        }

        private float GetBoneRadius(Vector3 position, Vector3 endPosition)
        {
            float length = (endPosition - position).magnitude;
            return kBoneScale * Mathf.Min(kBoneLenghtRatio * length, HandleUtility.GetHandleSize(position));
        }

        private void DrawParentLink(Vector3 startPoint, Vector3 endPoint, Color color)
        {
            Handles.matrix = Matrix4x4.identity;
            Handles.color = color;
            Handles.DrawLine(startPoint, endPoint);
        }

        private bool IsSelected(Transform transform)
        {
            return Selection.Contains(transform.gameObject);
        }

        private Color FadeFromSpriteSkin(Color color, SpriteSkin spriteSkin, SpriteBone[] spriteBones)
        {
            Debug.Assert(spriteSkin != null);
            Debug.Assert(spriteBones != null);

            var size = HandleUtility.GetHandleSize(spriteSkin.transform.position);
            var scaleFactorSqr = 1f;

            for (int i = 0; i < spriteBones.Length; ++i)
            {
                var spriteBone = spriteBones[i];
                var transform = spriteSkin.boneTransforms[i];
                var endPoint = transform.TransformPoint(Vector3.right * spriteBone.length);
                scaleFactorSqr = Mathf.Max(scaleFactorSqr, (transform.position - endPoint).sqrMagnitude);
            }

            var scaleFactor = Mathf.Sqrt(scaleFactorSqr);

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
