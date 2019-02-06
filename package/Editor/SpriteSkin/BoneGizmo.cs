using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;
using UnityEngine.Experimental.U2D.Common;

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
        private BoneGizmoController m_BoneGizmoController;

        internal BoneGizmoController boneGizmoController { get { return m_BoneGizmoController; } }

        internal void Initialize()
        {
            m_BoneGizmoController = new BoneGizmoController(new SkeletonView(new GUIWrapper()), new UnityEngineUndo(), new BoneGizmoToggle());
            RegisterCallbacks();
        }

        internal void ClearSpriteBoneCache()
        {
            boneGizmoController.ClearSpriteBoneCache();
        }

        private void RegisterCallbacks()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChange;
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            boneGizmoController.OnGUI();
        }

        private void OnSelectionChanged()
        {
            boneGizmoController.OnSelectionChanged();
        }

        private void OnHierarchyChange()
        {
            boneGizmoController.FindSkinComponents();
        }
    }

    internal class BoneGizmoController
    {
        private const float kFadeStart = 0.75f;
        private const float kFadeEnd = 1.75f;
        private List<SpriteSkin> m_SkinComponents = new List<SpriteSkin>();
        private Dictionary<Sprite, SpriteBone[]> m_SpriteBones = new Dictionary<Sprite, SpriteBone[]>();
        private Dictionary<Transform, Vector2> m_BoneData = new Dictionary<Transform, Vector2>();
        private HashSet<Transform> m_CachedBones = new HashSet<Transform>();
        private HashSet<Transform> m_SelectionRoots = new HashSet<Transform>();
        private ISkeletonView view;
        private IUndo m_Undo;
        private IBoneGizmoToggle m_BoneGizmoToggle;
        private Tool m_PreviousTool = Tool.None;

        internal IBoneGizmoToggle boneGizmoToggle { get { return m_BoneGizmoToggle; } set { m_BoneGizmoToggle = value; } }

        public Transform hoveredBone
        {
            get { return GetBone(view.hoveredBoneID); }
        }
        public Transform hoveredTail
        {
            get { return GetBone(view.hoveredTailID); }
        }
        public Transform hoveredBody
        {
            get { return GetBone(view.hoveredBodyID); }
        }
        public Transform hoveredJoint
        {
            get { return GetBone(view.hoveredJointID); }
        }
        public Transform hotBone
        {
            get { return GetBone(view.hotBoneID); }
        }

        private Transform GetBone(int instanceID)
        {
            return EditorUtility.InstanceIDToObject(instanceID) as Transform;
        }

        public BoneGizmoController(ISkeletonView view, IUndo undo, IBoneGizmoToggle toggle)
        {
            this.view = view;
            view.mode = SkeletonMode.EditPose;
            view.InvalidID = 0;
            m_Undo = undo;
            m_BoneGizmoToggle = toggle;

            FindSkinComponents();
        }

        internal void OnSelectionChanged()
        {
            m_SelectionRoots.Clear();

            foreach (var selectedTransform in Selection.transforms)
                m_SelectionRoots.Add(selectedTransform.root);

            if (m_PreviousTool == Tool.None && Selection.activeTransform != null && m_BoneData.ContainsKey(Selection.activeTransform))
            {
                m_PreviousTool = UnityEditor.Tools.current;
                UnityEditor.Tools.current = Tool.None;
            }

            if (m_PreviousTool != Tool.None && (Selection.activeTransform == null || !m_BoneData.ContainsKey(Selection.activeTransform)))
            {
                if (UnityEditor.Tools.current == Tool.None)
                    UnityEditor.Tools.current = m_PreviousTool;

                m_PreviousTool = Tool.None;
            }
        }

        internal void OnGUI()
        {
            m_BoneGizmoToggle.OnGUI();

            if (!m_BoneGizmoToggle.enableGizmos)
                return;

            PrepareBones();
            DoBoneGUI();
        }

        internal void FindSkinComponents()
        {
            var currentStage = StageUtility.GetCurrentStageHandle();
            var skins =  currentStage.FindComponentsOfType<SpriteSkin>().Where(x => x.gameObject.scene.isLoaded).ToArray();

            m_SkinComponents.Clear();
            m_SkinComponents.AddRange(skins);

            SceneView.RepaintAll();
        }

        internal void ClearSpriteBoneCache()
        {
            m_SpriteBones.Clear();
        }

        private void PrepareBones()
        {
            if (!view.CanLayout())
                return;

            if (view.IsActionHot(SkeletonAction.None))
                m_CachedBones.Clear();

            m_BoneData.Clear();

            foreach (var skinComponent in m_SkinComponents)
            {
                if (skinComponent == null)
                    continue;

                PrepareBones(skinComponent);
            }
        }

        private SpriteBone[] GetSpriteBones(SpriteSkin spriteSkin)
        {
            Debug.Assert(spriteSkin.isValid);

            var sprite = spriteSkin.spriteRenderer.sprite;
            SpriteBone[] spriteBones;
            if (!m_SpriteBones.TryGetValue(sprite, out spriteBones))
            {
                spriteBones = sprite.GetBones();
                m_SpriteBones[sprite] = sprite.GetBones();
            }
            
            return spriteBones;
        }

        private void PrepareBones(SpriteSkin spriteSkin)
        {
            Debug.Assert(spriteSkin != null);
            Debug.Assert(view.CanLayout());

            if (!spriteSkin.isActiveAndEnabled || !spriteSkin.isValid || !spriteSkin.spriteRenderer.enabled)
                return;

            var sprite = spriteSkin.spriteRenderer.sprite;
            var boneTransforms = spriteSkin.boneTransforms;
            var spriteBones = GetSpriteBones(spriteSkin);
            var alpha = 1f;

            if (spriteBones == null)
                return;
            
            for (int i = 0; i < boneTransforms.Length; ++i)
            {
                var boneTransform = boneTransforms[i];

                if (boneTransform == null ||  m_BoneData.ContainsKey(boneTransform))
                    continue;

                var bone = spriteBones[i];

                if (view.IsActionHot(SkeletonAction.None))
                    m_CachedBones.Add(boneTransform);

                m_BoneData.Add(boneTransform, new Vector2(bone.length, alpha));
            }
        }

        private void DoBoneGUI()
        {
            view.BeginLayout();

            if (view.CanLayout())
                LayoutBones();

            view.EndLayout();

            HandleSelectBone();
            HandleRotateBone();
            HandleMoveBone();
            DrawBones();
            DrawCursors();
        }

        private void LayoutBones()
        {
            foreach (var bone in m_CachedBones)
            {
                if (bone == null)
                    continue;

                Vector2 value;
                if (!m_BoneData.TryGetValue(bone, out value))
                    continue;

                var length = value.x;

                if (bone != hotBone)
                    view.LayoutBone(bone.GetInstanceID(), bone.position, bone.position + bone.GetScaledRight() * length, bone.forward, bone.up, bone.right, false);
            }
        }

        private void HandleSelectBone()
        {
            int instanceID;
            bool additive;
            if (view.DoSelectBone(out instanceID, out additive))
            {
                var bone = GetBone(instanceID);

                if (!additive)
                {
                    if (!Selection.Contains(bone.gameObject))
                        Selection.activeTransform = bone;
                }
                else
                {
                    var objectList = new List<Object>(Selection.objects);

                    if(objectList.Contains(bone.gameObject))
                        objectList.Remove(bone.gameObject);
                    else
                        objectList.Add(bone.gameObject);

                    Selection.objects = objectList.ToArray();
                }
            }
        }

        private void HandleRotateBone()
        {
            var pivot = hoveredBone;

            if (view.IsActionHot(SkeletonAction.RotateBone))
                pivot = hotBone;

            if (pivot == null)
                return;

            FindPivotTransform(pivot, out pivot);

            if (pivot == null)
                return;

            float deltaAngle;
            if (view.DoRotateBone(pivot.position, pivot.forward, out deltaAngle))
                SetBoneRotation(deltaAngle);
        }

        private void HandleMoveBone()
        {
            Vector3 deltaPosition;
            if (view.DoMoveBone(out deltaPosition))
                SetBonePosition(deltaPosition);
        }

        private bool FindPivotTransform(Transform transform, out Transform selectedTransform)
        {
            selectedTransform = transform;
            var selectedRoots = Selection.transforms;

            foreach(var selectedRoot in selectedRoots)
            {
                if(transform.IsDescendentOf(selectedRoot))
                {
                    selectedTransform = selectedRoot;
                    return true;
                }
            }

            return false;
        }

        private void SetBonePosition(Vector3 deltaPosition)
        {
            foreach (var selectedTransform in Selection.transforms)
            {
                if(!m_BoneData.ContainsKey(selectedTransform))
                    continue;
                
                var boneTransform = selectedTransform;
                
                m_Undo.RecordObject(boneTransform, TextContent.moveBone);
                boneTransform.position += deltaPosition;
            }
        }

        private void SetBoneRotation(float deltaAngle)
        {
            foreach(var selectedGameObject in Selection.gameObjects)
            {
                if(!m_BoneData.ContainsKey(selectedGameObject.transform))
                    continue;
                
                var boneTransform = selectedGameObject.transform;
                
                m_Undo.RecordObject(boneTransform, TextContent.rotateBone);
                boneTransform.Rotate(boneTransform.forward, deltaAngle, Space.World);
                InternalEngineBridge.SetLocalEulerHint(boneTransform);
            }
        }

        private void DrawBones()
        {
            if (!view.IsRepainting())
                return;

            var selectedOutlineColor = SelectionOutlineSettings.outlineColor;
            var selectedOutlineSize = SelectionOutlineSettings.selectedBoneOutlineSize;
            var defaultOutlineColor = Color.black.AlphaMultiplied(0.5f);

            //Draw bone outlines
            foreach (var boneData in m_BoneData)
            {
                var bone = boneData.Key;

                if (bone == null)
                    continue;

                var value = boneData.Value;
                var length = value.x;
                var alpha = value.y;

                if (alpha == 0f || !bone.gameObject.activeInHierarchy)
                    continue;

                var color = defaultOutlineColor;
                var outlineSize = 1.25f;

                var isSelected = Selection.Contains(bone.gameObject);
                var isHovered = hoveredBody == bone && view.IsActionHot(SkeletonAction.None);

                if (isSelected)
                {
                    color = selectedOutlineColor;
                    outlineSize = selectedOutlineSize * 0.5f + 1f;
                }
                else if (isHovered)
                    color = Handles.preselectionColor;

                DrawBoneOutline(bone, length, color, outlineSize);
            }

            //Draw bones
            foreach (var boneData in m_BoneData)
            {
                var bone = boneData.Key;

                if (bone == null)
                    continue;

                var value = boneData.Value;
                var length = value.x;
                var alpha = value.y;

                if (alpha == 0f || !bone.gameObject.activeInHierarchy)
                    continue;

                DrawBone(bone, length, Color.white);
            }
        }

        private void DrawBone(Transform bone, float length, Color color)
        {
            var isSelected = Selection.Contains(bone.gameObject);
            var isJointHovered = view.IsActionHot(SkeletonAction.None) && hoveredJoint == bone;
            var isTailHovered = view.IsActionHot(SkeletonAction.None) && hoveredTail == bone;

            view.DrawBone(bone.position, bone.GetScaledRight(), bone.forward, length, color, false, isSelected, isJointHovered, isTailHovered, bone == hotBone);
        }

        private void DrawBoneOutline(Transform bone, float length, Color color, float outlineScale)
        {
            view.DrawBoneOutline(bone.position, bone.GetScaledRight(), bone.forward, length, color, outlineScale);
        }

        private void DrawCursors()
        {
            if (!view.IsRepainting())
                return;

            view.DrawCursors(true);
        }

        private bool HasParentBone(Transform transform)
        {
            Debug.Assert(transform != null);

            return transform.parent != null && m_BoneData.ContainsKey(transform.parent);
        }

        /*
        private float AlphaFromSpriteSkin(SpriteSkin spriteSkin)
        {
            Debug.Assert(spriteSkin != null);
            Debug.Assert(spriteSkin.spriteBones != null);

            var spriteBones = spriteSkin.spriteBones;
            var size = view.GetHandleSize(spriteSkin.transform.position);
            var scaleFactorSqr = 1f;

            for (int i = 0; i < spriteBones.Length; ++i)
            {
                var spriteBone = spriteBones[i];
                var transform = spriteSkin.boneTransforms[i];
                var endPoint = transform.TransformPoint(Vector3.right * spriteBone.length);
                scaleFactorSqr = Mathf.Max(scaleFactorSqr, (transform.position - endPoint).sqrMagnitude);
            }

            var scaleFactor = Mathf.Sqrt(scaleFactorSqr);

            return AlphaFromSize(size, kFadeStart * scaleFactor, kFadeEnd * scaleFactor);
        }

        private float AlphaFromSize(float size, float fadeStart, float fadeEnd)
        {
            return Mathf.Lerp(1f, 0f, (size - fadeStart) / (fadeEnd - fadeStart));
        }
        */
    }
}
