using System.Collections.Generic;
using System.Linq;
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
            m_BoneGizmoController = new BoneGizmoController(new BoneGizmoView(new GUIWrapper()), new UndoObject(null), new BoneGizmoToggle());
            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChange;
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
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
        private Dictionary<Transform, Vector2> m_BoneData = new Dictionary<Transform, Vector2>();
        private HashSet<Transform> m_CachedBones = new HashSet<Transform>();
        private HashSet<Transform> m_SelectionRoots = new HashSet<Transform>();
        private IBoneGizmoView m_BoneGizmoView;
        private IUndoObject m_UndoObject;
        private IBoneGizmoToggle m_BoneGizmoToggle;
        private Tool m_PreviousTool = Tool.None;

        internal IBoneGizmoView boneGizmoView { get { return m_BoneGizmoView; } set { m_BoneGizmoView = value; } }
        internal IUndoObject undoObject { get { return m_UndoObject; } set { m_UndoObject = value; } }
        internal IBoneGizmoToggle boneGizmoToggle { get { return m_BoneGizmoToggle; } set { m_BoneGizmoToggle = value; } }

        public BoneGizmoController(IBoneGizmoView view, IUndoObject undo, IBoneGizmoToggle toggle)
        {
            m_BoneGizmoView = view;
            m_UndoObject = undo;
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
                m_PreviousTool = Tools.current;
                Tools.current = Tool.None;
            }

            if (m_PreviousTool != Tool.None && (Selection.activeTransform == null || !m_BoneData.ContainsKey(Selection.activeTransform)))
            {
                if (Tools.current == Tool.None)
                    Tools.current = m_PreviousTool;

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
            m_SkinComponents.Clear();
            m_SkinComponents.AddRange(GameObject.FindObjectsOfType<SpriteSkin>());

            SceneView.RepaintAll();
        }

        private void PrepareBones()
        {
            if (!m_BoneGizmoView.CanLayout())
                return;

            if (m_BoneGizmoView.IsActionHot(BoneGizmoAction.None))
                m_CachedBones.Clear();

            m_BoneData.Clear();

            foreach (var skinComponent in m_SkinComponents)
            {
                if (skinComponent == null)
                    continue;

                PrepareBones(skinComponent);
            }
        }

        private void PrepareBones(SpriteSkin spriteSkin)
        {
            Debug.Assert(spriteSkin != null);
            Debug.Assert(m_BoneGizmoView.CanLayout());

            if (!spriteSkin.isActiveAndEnabled || !spriteSkin.isValid || !spriteSkin.spriteRenderer.enabled)
                return;

            var sprite = spriteSkin.spriteRenderer.sprite;
            var boneTransforms = spriteSkin.boneTransforms;
            var spriteBones = spriteSkin.spriteBones;
            var alpha = AlphaFromSpriteSkin(spriteSkin);

            for (int i = 0; i < boneTransforms.Length; ++i)
            {
                var boneTransform = boneTransforms[i];

                if (boneTransform == null ||  m_BoneData.ContainsKey(boneTransform))
                    continue;

                var bone = spriteBones[i];

                if (m_BoneGizmoView.IsActionHot(BoneGizmoAction.None) && m_BoneGizmoView.IsBoneVisible(boneTransform, bone.length, alpha))
                    m_CachedBones.Add(boneTransform);

                m_BoneData.Add(boneTransform, new Vector2(bone.length, alpha));
            }
        }

        private void DoBoneGUI()
        {
            m_BoneGizmoView.SetupLayout();

            foreach (var boneTransform in m_CachedBones)
            {
                if (boneTransform == null)
                    continue;

                var value = m_BoneData[boneTransform];
                var length = value.x;

                m_BoneGizmoView.LayoutBone(boneTransform, length);

                BoneGizmoSelectionMode mode;
                if (m_BoneGizmoView.DoSelection(boneTransform, out mode))
                {
                    if(mode == BoneGizmoSelectionMode.Single)
                    {
                        if(!Selection.Contains(boneTransform.gameObject))
                            Selection.activeTransform = boneTransform;
                    }
                    else if(mode == BoneGizmoSelectionMode.Toggle)
                    {
                        var objectList = new List<Object>(Selection.objects);

                        if(objectList.Contains(boneTransform.gameObject))
                            objectList.Remove(boneTransform.gameObject);
                        else
                            objectList.Add(boneTransform.gameObject);

                        Selection.objects = objectList.ToArray();
                    }
                }
            }

            var selectedGameObjects = Selection.gameObjects;
            foreach (var selectedGameobject in selectedGameObjects)
            {
                if(!m_CachedBones.Contains(selectedGameobject.transform))
                    continue;
                
                var boneTransform = selectedGameobject.transform;

                float deltaAngle;
                if (m_BoneGizmoView.DoBoneRotation(boneTransform, out deltaAngle))
                    SetBoneRotation(deltaAngle);

                Vector3 deltaPosition;
                if (m_BoneGizmoView.DoBonePosition(boneTransform, out deltaPosition))
                    SetBonePosition(deltaPosition);
            }

            DrawBones();
        }

        private void SetBonePosition(Vector3 deltaPosition)
        {
            foreach (var selectedTransform in Selection.transforms)
            {
                if(!m_BoneData.ContainsKey(selectedTransform))
                    continue;
                
                var boneTransform = selectedTransform;
                
                m_UndoObject.undoObject = boneTransform;
                m_UndoObject.RecordObject("Move Bone");
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
                
                m_UndoObject.undoObject = boneTransform;
                m_UndoObject.RecordObject("Rotate Bone");
                boneTransform.Rotate(boneTransform.forward, deltaAngle, Space.World);
                InternalEngineBridge.SetLocalEulerHint(boneTransform);
            }
        }

        private void DrawBones()
        {
            if (!m_BoneGizmoView.CanRepaint())
                return;

            //Draw bone links first
            foreach (var boneData in m_BoneData)
            {
                var boneTransform = boneData.Key;

                if (boneTransform == null)
                    continue;

                var value = boneData.Value;
                var alpha = value.y;

                if (alpha == 0f)
                    continue;

                if (HasParentBone(boneTransform))
                {
                    var parentLength = m_BoneData[boneTransform.parent].x;
                    var color = Color.white;
                    color.a = alpha;

                    m_BoneGizmoView.DrawParentBoneLink(boneTransform, parentLength, color);
                }
            }

            //Draw bones
            foreach (var boneData in m_BoneData)
            {
                var boneTransform = boneData.Key;

                if (boneTransform == null)
                    continue;

                var value = boneData.Value;
                var length = value.x;
                var alpha = value.y;

                if (alpha == 0f)
                    continue;

                m_BoneGizmoView.DrawBone(boneTransform, length, alpha);
            }
        }

        private bool HasParentBone(Transform transform)
        {
            Debug.Assert(transform != null);

            return transform.parent != null && m_BoneData.ContainsKey(transform.parent);
        }

        private float AlphaFromSpriteSkin(SpriteSkin spriteSkin)
        {
            Debug.Assert(spriteSkin != null);
            Debug.Assert(spriteSkin.spriteBones != null);

            var spriteBones = spriteSkin.spriteBones;
            var size = m_BoneGizmoView.GetHandleSize(spriteSkin.transform.position);
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
    }
}
