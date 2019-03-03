using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.Experimental.U2D.Common;
using UnityEditor.ShortcutManagement;
using UnityEditor.EditorTools;

namespace UnityEditor.U2D
{
    [EditorTool("Edit Sprite Shape", typeof(SpriteShapeController))]
    public class SpriteShapeEditorTool : EditorTool
    {
        internal static class Contents
        {
            public static readonly GUIContent editShapeIcon = SpriteShapeEditorGUI.IconContent("EditShape", "Start editing the Spline in the Scene View.");
            public static readonly GUIContent editShapeIconPro = SpriteShapeEditorGUI.IconContent("EditShapePro", "Start editing the Spline in the Scene View.");

            public static GUIContent icon
            {
                get
                {
                    var content = editShapeIcon;

                    if (EditorGUIUtility.isProSkin)
                        content = editShapeIconPro;

                    return content;
                }
            }
        }

        public static SpriteShapeEditorTool instance { get; private set; }

        public SpriteShapeController targetController
        {
            get { return target as SpriteShapeController; }
        }

        public override GUIContent toolbarIcon
        {
            get { return Contents.icon; }
        }

        private void OnEnable()
        {
            instance = this;
        }

        public override bool IsAvailable()
        {
            return targets.Count() == 1;
        }

        public bool isActive
        {
            get { return EditorTools.EditorTools.IsActiveTool(this) && IsAvailable(); }
        }
    }

    [InitializeOnLoad]
    public class SpriteShapeToolInitializer
    {
        private static SpriteShapeTool m_SpriteShapeTool = new SpriteShapeTool();
        private static InternalEditorBridge.ShortcutContext m_ShortcutContext;

        static SpriteShapeToolInitializer()
        {
            RegisterShortcuts();
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private static void DuringSceneGui(SceneView sceneView)
        {
            HandleActivation();

            if (m_SpriteShapeTool.isActive)
                m_SpriteShapeTool.OnGUI(sceneView);
        }

        private static void HandleActivation()
        {
            if (m_SpriteShapeTool.isActive)
            {
                if (SpriteShapeEditorTool.instance == null || !SpriteShapeEditorTool.instance.isActive)
                    m_SpriteShapeTool.Deactivate();
            }
            else if (SpriteShapeEditorTool.instance != null && SpriteShapeEditorTool.instance.isActive)
            {
                m_SpriteShapeTool.Activate();
            }
        }

        private static void RegisterShortcuts()
        {
            m_ShortcutContext = new InternalEditorBridge.ShortcutContext()
            {
                isActive = () => m_SpriteShapeTool.isActive,
                context = m_SpriteShapeTool
            };

            InternalEditorBridge.RegisterShortcutContext(m_ShortcutContext);
        }
        
        [Shortcut("SpriteShape Editing/Cycle Tangent Mode", typeof(InternalEditorBridge.ShortcutContext), KeyCode.M)]
        private static void ShortcutCycleTangentMode(ShortcutArguments args)
        {
            m_SpriteShapeTool.CycleTangentMode();
        }

        [Shortcut("SpriteShape Editing/Cycle Variant", typeof(InternalEditorBridge.ShortcutContext), KeyCode.N)]
        private static void ShortcutCycleSpriteIndex(ShortcutArguments args)
        {
            m_SpriteShapeTool.CycleSpriteIndex();
        }

        [Shortcut("SpriteShape Editing/Mirror Tangent", typeof(InternalEditorBridge.ShortcutContext), KeyCode.B)]
        private static void ShortcutCycleMirrorTangent(ShortcutArguments args)
        {
            m_SpriteShapeTool.MirrorTangent();
        }
    }

    public class SpriteShapeTool
    {
        public static SpriteShapeTool instance { get; private set; }
        private int m_RectSelectionID = -1;
        private SplineEditor m_SplineEditor;
        private RectSelectionTool m_RectSelectionTool = new RectSelectionTool();
        private Spline m_Spline;
        private Vector3 m_LastPosition;
        private Quaternion m_LastRotation;
        private Vector3 m_LastScale;
        private int m_LastHashCode;
        private Bounds m_Bounds;
        private bool m_IsActive = false;

        public bool isActive
        {
            get { return m_IsActive; }
        }

        private SpriteShapeController target
        {
            get { return SpriteShapeEditorTool.instance.targetController; }
        }

        public SpriteShapeTool()
        {
            instance = this;
        }

        public void Activate()
        {
            m_IsActive = true;

            RegisterCallbacks();
            InitializeCheck();
        }

        public void Deactivate()
        {
            m_IsActive = false;

            UnregisterCallbacks();
            InvalidateShapeEditor();
        }

        private void RegisterCallbacks()
        {
            UnregisterCallbacks();
            
            Undo.undoRedoPerformed += OnUndoRedo;
            Selection.selectionChanged += SelectionChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private void UnregisterCallbacks()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Selection.selectionChanged -= SelectionChanged;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        }

        private void InvalidateShapeEditor()
        {
            m_SplineEditor = null;
            m_Spline = null;
            SplineEditorCache.SetTarget(null);
        }

        private bool InitializeCheck()
        {
            if (target != SplineEditorCache.GetTarget())
            {
                InvalidateShapeEditor();
                SplineEditorCache.ClearSelection();
                SplineEditorCache.SetTarget(target);
            }

            if (target != null && m_SplineEditor == null)
                SetupSpriteShapeEditor(target.spline);

            return target != null;
        }

        public void OnGUI(SceneView sceneView)
        {
            if (!InitializeCheck() || GUI.enabled == false)
                return;

            TransformChangedCheck();

            EditorGUI.BeginChangeCheck();

            m_SplineEditor.OnGUI();
            DoRectSelectionGUI();

            if (EditorGUI.EndChangeCheck())
            {
                m_SplineEditor.SetDirty();
                m_SplineEditor.Repaint();
            }

            SplineHashCheck();

            if (Event.current.type == EventType.MouseMove)
                sceneView.Repaint();
        }

        private void OnUndoRedo()
        {
            InvalidateShapeEditor();
            SceneView.RepaintAll();
        }

        private void SelectionChanged()
        {
            if (target == null)
                return;

            if (target != SplineEditorCache.GetTarget())
                SplineEditorCache.RigisterUndo();

            InvalidateShapeEditor();
        }

        private void PlayModeStateChanged(PlayModeStateChange stateChange)
        {
            InvalidateShapeEditor();
        }

        private void DoRectSelectionGUI()
        {
            Debug.Assert(m_Spline != null);

            var selection = SplineEditorCache.GetSelection();

            if (m_RectSelectionID == -1)
                m_RectSelectionID = GUIUtility.GetControlID("RectSelection".GetHashCode(), FocusType.Passive);

            if (Event.current.GetTypeForControl(m_RectSelectionID) == EventType.MouseDown && Event.current.button == 0)
            {
                if (!Event.current.shift && !EditorGUI.actionKey)
                {
                    SplineEditorCache.RigisterUndo("Edit Selection");
                    SplineEditorCache.ClearSelection();
                    GUI.changed = true;
                }
            }

            if (Event.current.GetTypeForControl(m_RectSelectionID) == EventType.MouseUp && Event.current.button == 0)
            {
                SplineEditorCache.RigisterUndo("Edit Selection");

                selection.EndSelection(true);

                GUI.changed = true;
            }

            EditorGUI.BeginChangeCheck();

            var selectionRect = m_RectSelectionTool.Do(m_RectSelectionID, GetTransform().position);

            if (EditorGUI.EndChangeCheck())
            {
                selection.BeginSelection();

                for (int i = 0; i < m_Spline.GetPointCount(); ++i)
                {
                    if (selectionRect.Contains(HandleUtility.WorldToGUIPoint(LocalToWorld(m_Spline.GetPosition(i))), true))
                        selection.Select(i, true);
                }
            }
        }

        private void SplineHashCheck()
        {
            Debug.Assert(m_Spline != null);
            Debug.Assert(m_SplineEditor != null);

            var hashCode = m_Spline.GetHashCode();

            if (m_LastHashCode != hashCode)
                m_SplineEditor.SetDirty();

            m_LastHashCode = hashCode;
        }

        private void TransformChangedCheck()
        {
            Debug.Assert(m_SplineEditor != null);

            var transform = GetTransform();
            if (m_LastPosition != transform.position || m_LastRotation != transform.rotation || m_LastScale != transform.lossyScale)
            {
                m_SplineEditor.SetDirty();

                m_LastPosition = transform.position;
                m_LastRotation = transform.rotation;
                m_LastScale = transform.lossyScale;
            }
        }

        private Transform GetTransform()
        {
            return target.transform;
        }

        private Vector3 LocalToWorld(Vector3 position)
        {
            return GetTransform().TransformPoint(position);
        }

        private Vector3 WorldToLocal(Vector3 position)
        {
            return GetTransform().InverseTransformPoint(position);
        }

        private void SetupSpriteShapeEditor(Spline spline)
        {
            m_Spline = spline;
            m_SplineEditor = new SplineEditor() 
            {
                //Data
                GetPosition = i => LocalToWorld(spline.GetPosition(i)),
                SetPosition = (i, p) => spline.SetPosition(i, WorldToLocal(p)),
                GetLeftTangent = i => LocalToWorld(spline.GetLeftTangent(i) + spline.GetPosition(i)) - LocalToWorld(spline.GetPosition(i)),
                SetLeftTangent = (i, p) => spline.SetLeftTangent(i, WorldToLocal(p + LocalToWorld(spline.GetPosition(i))) - spline.GetPosition(i)),
                GetRightTangent = i => LocalToWorld(spline.GetRightTangent(i) + spline.GetPosition(i)) - LocalToWorld(spline.GetPosition(i)),
                SetRightTangent = (i, p) => spline.SetRightTangent(i, WorldToLocal(p + LocalToWorld(spline.GetPosition(i))) - spline.GetPosition(i)),
                GetTangentMode = i => (SplineEditor.TangentMode)spline.GetTangentMode(i),
                SetTangentMode = (i, m) => spline.SetTangentMode(i, (ShapeTangentMode)m),
                InsertPointAt = (i, p) => spline.InsertPointAt(i, WorldToLocal(p)),
                RemovePointAt = i => spline.RemovePointAt(i),
                GetPointCount = () => spline.GetPointCount(),
                // Transforms
                ScreenToWorld = (p) => ScreenToWorld(p),
                LocalToWorldMatrix = () => Matrix4x4.identity,
                WorldToScreen = (p) => HandleUtility.WorldToGUIPoint(p),
                GetForwardVector = () => GetTransform().forward,
                GetUpVector = () => GetTransform().up,
                GetRightVector = () => GetTransform().right,
                // Other
                GetSpriteIndex = (i) => spline.GetSpriteIndex(i),
                SetSpriteIndex = (s, i) => spline.SetSpriteIndex(s, i),
                Snap = (p) => SnapPoint(p),
                RegisterUndo = RegisterUndo,
                OpenEnded = () => spline.isOpenEnded,
                Repaint = () => { UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
            };

            m_SplineEditor.UpdateTangentCache();
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition, Plane plane)
        {
            var ray = HandleUtility.GUIPointToWorldRay(screenPosition);

            float distance;
            plane.Raycast(ray, out distance);
            return ray.GetPoint(distance);
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            return ScreenToWorld(screenPosition, new Plane(GetTransform().forward, GetTransform().position));
        }

        private Vector3 SnapPoint(Vector3 position)
        {
            var np0screen = m_SplineEditor.WorldToScreen(position);
            var snappedScreen = m_SplineEditor.WorldToScreen(SnappingUtility.Snap(position));

            var snapDistance = (np0screen - snappedScreen).magnitude;
            if (snapDistance < 15f)
            {
                position = SnappingUtility.Snap(position);
            }
            return position;
        }

        private void RegisterUndo()
        {
            Undo.RegisterCompleteObjectUndo(SplineEditorCache.GetTarget(), "Edit Sprite Shape");
        }

        public void CycleTangentMode()
        {
            m_SplineEditor.CycleTangentMode();
        }

        public void CycleSpriteIndex()
        {
            m_SplineEditor.CycleSpriteIndex();
        }

        public void MirrorTangent()
        {
            m_SplineEditor.MirrorTangent();
        }

        public void SetTangentMode(int pointIndex, ShapeTangentMode mode)
        {
            m_SplineEditor.SetTangentModeUseThisOne(pointIndex, (SplineEditor.TangentMode)mode);
        }
    }
}
