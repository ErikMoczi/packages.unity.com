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
    public enum Mode
    {
        Geometry,
        Weights
    }

    public enum WeightTool
    {
        Slider,
        Brush
    }

    [RequireSpriteDataProvider(typeof(ISpriteMeshDataProvider), typeof(ISpriteBoneDataProvider))]
    public class SpriteMeshModule : SpriteEditorModuleBase
    {
        private const float kFilterTolerance = 0.01f;
        private const int kNiceColorCount = 6;

        private static class Contents
        {
            public static readonly GUIContent geometry = new GUIContent("Geometry");
            public static readonly GUIContent weights = new GUIContent("Weights");
            public static readonly GUIContent auto = new GUIContent("Auto");
            public static readonly GUIContent safe = new GUIContent("Safe");
            public static readonly GUIContent normalize = new GUIContent("Normalize");
            public static readonly GUIContent clear = new GUIContent("Clear");
            public static readonly GUIContent generate = new GUIContent("Generate");
            public static readonly GUIContent selection = new GUIContent("Selection");
            public static readonly GUIContent createVertex = new GUIContent("Create Vertex");
            public static readonly GUIContent createEdge = new GUIContent("Create Edge");
            public static readonly GUIContent splitEdge = new GUIContent("Split Edge");
            public static readonly GUIContent slider = new GUIContent("Slider");
            public static readonly GUIContent brush = new GUIContent("Brush");
            public static readonly GUIContent weightEditor = new GUIContent("Weight Editor");
            public static readonly GUIContent inspector = new GUIContent("Inspector");
        }

        private class Styles
        {
            public readonly GUIStyle preLabel = "preLabel";
            public readonly GUIStyle preSlider = "preSlider";
            public readonly GUIStyle preSliderThumb = "preSliderThumb";
            public readonly GUIContent RGBIcon;
            public readonly GUIContent BoneIcon;

            public Styles()
            {
                RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
                BoneIcon = new GUIContent(Resources.Load<Texture>("NormalCreate"));
            }
        }

        private void InitStyles()
        {
            if (m_Styles == null)
                m_Styles = new Styles();
        }

        private SpriteMeshData selectedSpriteMeshData
        {
            get
            {
                SpriteRect spriteRect = spriteEditor.selectedSpriteRect;

                if (spriteRect != null)
                    return m_SpriteMeshCache.GetSpriteMeshData(spriteRect.spriteID);

                return null;
            }
        }

        private int defaultControlID { get { return m_RectSelectionTool.controlID; } }

        #region ISpriteEditorModule implemenation
        public override string moduleName
        {
            get { return "Skin Weights And Geometry Editor"; }
        }

        public override void OnModuleActivate()
        {
            InitStyles();

            Undo.undoRedoPerformed += UndoRedoPerformed;
            spriteEditor.enableMouseMoveEvent = true;

            m_SpriteMeshCache = ScriptableObject.CreateInstance<SpriteMeshCache>();
            m_Triangulator = new Triangulator();
            m_WeightGenerator = new BoundedBiharmonicWeightsGenerator();
            m_OutlineGenerator = new OutlineGenerator();
            m_UndoObject = new UndoObject(m_SpriteMeshCache);
            m_SpriteMeshController = new SpriteMeshController();
            m_SpriteMeshView = new SpriteMeshView();
            m_BoneGUI = new BoneGUI();
            m_RectSelectionTool = new RectSelectionTool(m_SpriteMeshCache);
            m_UnselectTool = new UnselectTool(m_SpriteMeshCache);
            m_WeightEditor = new WeightEditor();
            m_BrushWeightTool = new BrushWeightTool();
            m_SliderWeightTool = new SliderWeightTool();
            m_WeightInspector = new WeightInspector(m_SpriteMeshCache);
            m_BoneInspector = new BoneInspector(m_SpriteMeshCache);

            var dataProvider = spriteEditor.GetDataProvider<ISpriteEditorDataProvider>();
            var boneProvider = spriteEditor.GetDataProvider<ISpriteBoneDataProvider>();
            var spriteMeshProvider = spriteEditor.GetDataProvider<ISpriteMeshDataProvider>();
            var spriteRects = dataProvider.GetSpriteRects();

            for (var i = 0; i < spriteRects.Length; i++)
            {
                var spriteRect = spriteRects[i];
                var data = new SpriteMeshData();
                data.spriteID = spriteRect.spriteID;
                data.frame = spriteRect.rect;

                var importedBones = boneProvider.GetBones(spriteRect.spriteID);
                data.bones = MeshModuleUtility.ConvertBoneFromLocalSpaceToTextureSpace(importedBones, data.frame.position);

                var metaVertices = spriteMeshProvider.GetVertices(spriteRect.spriteID);
                foreach (var mv in metaVertices)
                {
                    var v = new Vertex2D(mv.position + spriteRect.rect.position, mv.boneWeight);
                    data.vertices.Add(v);
                }

                data.indices = new List<int>(spriteMeshProvider.GetIndices(spriteRect.spriteID));

                Vector2Int[] edges = spriteMeshProvider.GetEdges(spriteRect.spriteID);

                foreach (var e in edges)
                    data.edges.Add(new Edge(e.x, e.y));

                m_SpriteMeshCache.AddSpriteMeshData(data);
            }

            m_WeightEditorWindow = new ModuleWindow(Contents.weightEditor.text, new Rect(0f, 0f, 300f, 195f));
            m_WeightEditorWindow.windowGUICallback = WeightEditorInspector;

            m_InspectorWindow = new ModuleWindow(Contents.inspector.text, new Rect(0f, 0f, 300f, 95f));

            InitializeMesh();
            InitializeMaterials();

            m_GenerateGeometryMenuContents.settings = m_GenerateGeometrySettings;
            m_GenerateGeometryMenuContents.onGenerateGeometry = OnGenerateGeometry;

            m_MeshDirty = true;
        }

        private void DoBoneSelectionInspector()
        {
            if (m_BoneInspector.selection == null || m_BoneInspector.selection.Count != 1)
                return;

            EditorGUI.BeginChangeCheck();

            m_BoneInspector.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                m_IndicesDirty = true;
                spriteEditor.SetDataModified();
            }
        }

        private void DoWeightSelectionInspector()
        {
            EditorGUI.BeginChangeCheck();
            m_WeightInspector.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                m_ColorsDirty = true;
                spriteEditor.SetDataModified();
            }
        }

        public override void OnModuleDeactivate()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            spriteEditor.enableMouseMoveEvent = false;
            m_RectSelectionTool = null;
            m_UnselectTool = null;
            m_SpriteMeshView = null;
            m_WeightInspector = null;
            m_BrushWeightTool = null;
            m_SliderWeightTool = null;
            m_WeightEditorWindow = null;
            InvalidateMesh();
            InvalidateSpriteMeshCache();
            InvalidateMaterials();
        }

        private void UndoRedoPerformed()
        {
            m_MeshDirty = true;
        }

        public override bool CanBeActivated()
        {
            var dataProvider = spriteEditor.GetDataProvider<ISpriteEditorDataProvider>();
            return dataProvider == null ? false : dataProvider.spriteImportMode != SpriteImportMode.None;
        }

        #endregion

        #region ISpriteEditorModule implemenation
        public override void DoPostGUI()
        {
            ((EditorWindow)spriteEditor).BeginWindows();
            DoWindowsGUI();
            ((EditorWindow)spriteEditor).EndWindows();

            if (!spriteEditor.windowDimension.Contains(Event.current.mousePosition))
                HandleUtility.nearestControl = 0;

            if (Event.current.type == EventType.Layout && m_PrevNearestControl != HandleUtility.nearestControl)
            {
                m_PrevNearestControl = HandleUtility.nearestControl;
                spriteEditor.RequestRepaint();
            }
        }

        public override void DoMainGUI()
        {
            HandleSpriteRectSelectionChange();
            DrawGizmos();

            SetupElements();

            if (selectedSpriteMeshData != null)
            {
                PrepareMesh();

                if (m_SpriteMeshCache.mode == Mode.Weights)
                    DrawWeights();

                DrawTriangles();

                EditorGUI.BeginChangeCheck();

                m_SpriteMeshController.OnGUI();

                if (EditorGUI.EndChangeCheck())
                {
                    m_VerticesDirty = true;
                    m_IndicesDirty = true;
                    spriteEditor.SetDataModified();
                }

                if (m_SpriteMeshCache.mode == Mode.Weights)
                    m_BoneGUI.DoBoneGUI();

                m_RectSelectionTool.OnGUI();
                m_UnselectTool.OnGUI();

                if (m_SpriteMeshCache.mode == Mode.Weights && m_SpriteMeshCache.selectedWeightTool == WeightTool.Brush &&
                    (m_BoneGUI.hoveredBone == -1 || m_BoneGUI.selection.IsSelected(m_BoneGUI.hoveredBone)))
                {
                    EditorGUI.BeginChangeCheck();

                    m_BrushWeightTool.OnGUI();

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_ColorsDirty = true;
                        spriteEditor.SetDataModified();
                    }
                }
            }
        }

        private void HandleSpriteRectSelectionChange()
        {
            if ((selectedSpriteMeshData == null || HandleUtility.nearestControl == m_RectSelectionTool.controlID || HandleUtility.nearestControl == m_BrushWeightTool.controlID) &&
                Event.current.clickCount == 2 && spriteEditor.HandleSpriteSelection())
            {
                Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, Undo.GetCurrentGroupName());

                m_MeshDirty = true;
                m_SpriteMeshCache.boneSelection.Clear();
                m_SpriteMeshCache.selection.Clear();
            }
        }

        public override void DoToolbarGUI(Rect drawArea)
        {
            using (new EditorGUI.DisabledScope(spriteEditor.editingDisabled || selectedSpriteMeshData == null))
            {
                GUILayout.BeginArea(drawArea);
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();

                Mode mode = m_SpriteMeshCache.mode;

                if (GUILayout.Toggle(mode == Mode.Geometry, Contents.geometry, EditorStyles.toolbarButton, GUILayout.Width(75f)))
                    mode =  Mode.Geometry;

                if (GUILayout.Toggle(mode == Mode.Weights, Contents.weights, EditorStyles.toolbarButton, GUILayout.Width(75f)))
                    mode = Mode.Weights;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, "Change Mode");
                    m_SpriteMeshCache.mode = mode;
                }

                GUILayout.Space(10f);

                if (m_SpriteMeshCache.mode == Mode.Geometry)
                    DoGeometryToolbar();
                else if (m_SpriteMeshCache.mode == Mode.Weights)
                    DoWeightsToolbar();

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        private void DoGeometryToolbar()
        {
            bool pressed = EditorGUILayout.DropdownButton(Contents.generate, FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.Width(70f));

            if (Event.current.type == EventType.Repaint)
                m_GenerateButtonRect = GUILayoutUtility.GetLastRect();

            if (pressed)
                PopupWindow.Show(m_GenerateButtonRect, m_GenerateGeometryMenuContents);

            SpriteMeshViewMode mode = m_SpriteMeshView.mode;

            if (GUILayout.Toggle(mode == SpriteMeshViewMode.Selection, Contents.selection, EditorStyles.toolbarButton))
                mode = SpriteMeshViewMode.Selection;

            if (GUILayout.Toggle(mode == SpriteMeshViewMode.CreateVertex, Contents.createVertex, EditorStyles.toolbarButton))
                mode = SpriteMeshViewMode.CreateVertex;

            if (GUILayout.Toggle(mode == SpriteMeshViewMode.CreateEdge, Contents.createEdge, EditorStyles.toolbarButton))
                mode = SpriteMeshViewMode.CreateEdge;

            if (GUILayout.Toggle(mode == SpriteMeshViewMode.SplitEdge, Contents.splitEdge, EditorStyles.toolbarButton))
                mode = SpriteMeshViewMode.SplitEdge;

            m_SpriteMeshView.mode = mode;
        }

        private void DoWeightsToolbar()
        {
            if (GUILayout.Button(Contents.auto, EditorStyles.toolbarButton))
            {
                Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, "Edit Weights");

                selectedSpriteMeshData.CalculateWeights(m_WeightGenerator, m_SpriteMeshCache.selection, kFilterTolerance);

                m_ColorsDirty = true;
                spriteEditor.SetDataModified();
            }

            if (GUILayout.Button(Contents.safe, EditorStyles.toolbarButton))
            {
                Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, "Edit Weights");

                selectedSpriteMeshData.CalculateWeightsSafe(m_WeightGenerator, m_SpriteMeshCache.selection, kFilterTolerance);

                m_ColorsDirty = true;
                spriteEditor.SetDataModified();
            }

            if (GUILayout.Button(Contents.normalize, EditorStyles.toolbarButton))
            {
                Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, "Edit Weights");

                selectedSpriteMeshData.NormalizeWeights(m_SpriteMeshCache.selection);

                m_ColorsDirty = true;
                spriteEditor.SetDataModified();
            }

            if (GUILayout.Button(Contents.clear, EditorStyles.toolbarButton))
            {
                Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, "Edit Weights");

                selectedSpriteMeshData.ClearWeights(m_SpriteMeshCache.selection);

                m_ColorsDirty = true;
                spriteEditor.SetDataModified();
            }

            GUILayout.Box(m_Styles.RGBIcon, m_Styles.preLabel);
            m_Opacity = GUILayout.HorizontalSlider(m_Opacity, 0.25f, 1f, m_Styles.preSlider, m_Styles.preSliderThumb, GUILayout.MinWidth(60));
            GUILayout.Box(m_Styles.BoneIcon, m_Styles.preLabel);
            m_BoneGUI.boneOpacity = GUILayout.HorizontalSlider(m_BoneGUI.boneOpacity, 0.25f, 1f, m_Styles.preSlider, m_Styles.preSliderThumb, GUILayout.MinWidth(60));
        }

        private void DoWindowsGUI()
        {
            if (selectedSpriteMeshData == null)
                return;

            if (m_SpriteMeshCache.mode == Mode.Geometry)
                DoGeometryWindows();
            else if (m_SpriteMeshCache.mode == Mode.Weights)
                DoWeightsWindows();
        }

        private void DoGeometryWindows()
        {
        }

        private void DoWeightsWindows()
        {
            if (m_InspectorWindow.windowGUICallback != null)
                m_InspectorWindow.OnWindowGUI(spriteEditor.windowDimension);

            m_WeightEditorWindow.OnWindowGUI(spriteEditor.windowDimension);
        }

        public Rect CalculateWeightEditorInspectorRect()
        {
            float height = m_SpriteMeshCache.selectedWeightTool == WeightTool.Slider ? m_SliderWeightTool.GetInspectorHeight() : m_BrushWeightTool.GetInspectorHeight();

            return new Rect(0, 0, 300f, height + MeshModuleUtility.kEditorLineHeight * 2f + 10f);
        }

        public void WeightEditorInspector()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            WeightTool weightTool = m_SpriteMeshCache.selectedWeightTool;

            EditorGUI.BeginChangeCheck();

            if (GUILayout.Toggle(weightTool == WeightTool.Slider, Contents.slider, EditorStyles.miniButtonLeft, GUILayout.Width(75f)))
                weightTool = WeightTool.Slider;

            if (GUILayout.Toggle(weightTool == WeightTool.Brush, Contents.brush, EditorStyles.miniButtonRight, GUILayout.Width(75f)))
                weightTool = WeightTool.Brush;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, "Change Weight Tool");

                if (weightTool == WeightTool.Brush)
                    m_SpriteMeshCache.selection.Clear();

                m_SpriteMeshCache.selectedWeightTool = weightTool;
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            if (m_SpriteMeshCache.selectedWeightTool == WeightTool.Brush)
            {
                EditorGUI.BeginChangeCheck();

                m_BrushWeightTool.OnInspectorGUI();

                if (EditorGUI.EndChangeCheck())
                {
                    CheckBoneSelectionChanged();
                }
            }
            else if (m_SpriteMeshCache.selectedWeightTool == WeightTool.Slider)
            {
                EditorGUI.BeginChangeCheck();

                m_SliderWeightTool.OnInspectorGUI();

                if (EditorGUI.EndChangeCheck())
                {
                    m_ColorsDirty = true;
                    spriteEditor.SetDataModified();

                    CheckBoneSelectionChanged();
                }
            }
        }

        private void CheckBoneSelectionChanged()
        {
            if (m_SpriteMeshCache.boneSelection.single != m_WeightEditor.boneIndex)
            {
                Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, "Select Bone");

                m_SpriteMeshCache.boneSelection.Clear();
                m_SpriteMeshCache.boneSelection.Select(m_WeightEditor.boneIndex, true);
            }
        }

        public override bool ApplyRevert(bool apply)
        {
            if (apply)
            {
                var meshDataProvider = spriteEditor.GetDataProvider<ISpriteMeshDataProvider>();
                var boneDataProvider = spriteEditor.GetDataProvider<ISpriteBoneDataProvider>();

                foreach (var spriteMeshData in m_SpriteMeshCache)
                {
                    List<Vertex2DMetaData> vmd = new List<Vertex2DMetaData>(spriteMeshData.vertices.Count);
                    foreach (var v in spriteMeshData.vertices)
                        vmd.Add(new Vertex2DMetaData() { position = v.position - spriteMeshData.frame.position, boneWeight = v.editableBoneWeight.ToBoneWeight(true) });

                    List<Vector2Int> emd = new List<Vector2Int>(spriteMeshData.edges.Count);
                    foreach (var e in spriteMeshData.edges)
                        emd.Add(new Vector2Int(e.index1, e.index2));

                    meshDataProvider.SetVertices(spriteMeshData.spriteID, vmd.ToArray());
                    meshDataProvider.SetIndices(spriteMeshData.spriteID, spriteMeshData.indices.ToArray());
                    meshDataProvider.SetEdges(spriteMeshData.spriteID, emd.ToArray());

                    List<SpriteBone> bones = boneDataProvider.GetBones(spriteMeshData.spriteID);
                    for (int i = 0; i < bones.Count; ++i)
                    {
                        SpriteBone original = bones[i];
                        SpriteBone bone = spriteMeshData.bones[i];
                        Vector3 position = original.position;
                        position.z = bone.position.z;
                        original.position = position;
                        bones[i] = original;
                    }

                    boneDataProvider.SetBones(spriteMeshData.spriteID, bones);
                }
            }

            return true;
        }

        #endregion

        void SetupElements()
        {
            m_SpriteMeshController.spriteMeshView = m_SpriteMeshView;
            m_SpriteMeshController.spriteMeshData = selectedSpriteMeshData;
            m_SpriteMeshController.selection = m_SpriteMeshCache.selection;
            m_SpriteMeshController.triangulator = m_Triangulator;

            if (m_SpriteMeshCache.mode == Mode.Weights && m_SpriteMeshCache.selectedWeightTool == WeightTool.Brush)
                m_SpriteMeshController.selection = m_BrushWeightTool.selection;

            m_SpriteMeshController.undoObject = m_UndoObject;
            m_SpriteMeshView.defaultControlID = defaultControlID;
            if (m_SpriteMeshCache.mode == Mode.Weights)
                m_SpriteMeshView.mode = SpriteMeshViewMode.Selection;

            m_BoneGUI.spriteMeshdata = selectedSpriteMeshData;
            m_BoneGUI.selection = m_SpriteMeshCache.boneSelection;
            m_BoneGUI.undoObject = m_UndoObject;
            m_BoneGUI.defaultControlID = defaultControlID;

            m_RectSelectionTool.vertices = null;
            m_RectSelectionTool.selection = null;
            m_UnselectTool.selection = null;

            if (selectedSpriteMeshData != null)
            {
                m_RectSelectionTool.vertices = selectedSpriteMeshData.vertices;
                m_RectSelectionTool.selection = m_SpriteMeshCache.selection;
                m_UnselectTool.selection = m_SpriteMeshCache.selection;
            }

            if (m_SpriteMeshCache.selection.Count == 0)
                m_UnselectTool.selection = m_SpriteMeshCache.boneSelection;

            m_WeightInspector.spriteMeshData = selectedSpriteMeshData;
            m_WeightInspector.selection = m_SpriteMeshCache.selection;
            m_BoneInspector.spriteMeshData = selectedSpriteMeshData;
            m_BoneInspector.selection = m_SpriteMeshCache.boneSelection;

            Rect inspectorRect = m_InspectorWindow.rect;
            if (m_SpriteMeshCache.selection.Count > 0)
            {
                m_InspectorWindow.windowGUICallback = DoWeightSelectionInspector;
                inspectorRect.height = m_WeightInspector.CalculateHeight(spriteEditor.windowDimension);
            }
            else if (m_SpriteMeshCache.boneSelection.Count == 1)
            {
                m_InspectorWindow.windowGUICallback = DoBoneSelectionInspector;
                inspectorRect.height = m_BoneInspector.CalculateHeight(spriteEditor.windowDimension);
            }
            else
                m_InspectorWindow.windowGUICallback = null;

            m_InspectorWindow.rect = inspectorRect;
            m_InspectorWindow.DockBottomLeft(spriteEditor.windowDimension, new Vector2(10f, -10f));

            m_WeightEditor.spriteMeshData = selectedSpriteMeshData;
            m_WeightEditor.undoObject = m_UndoObject;
            m_WeightEditor.boneIndex = m_SpriteMeshCache.boneSelection.single;
            m_WeightEditor.selection = m_SpriteMeshCache.selection;
            m_WeightEditor.emptySelectionEditsAll = true;
            m_BrushWeightTool.weightEditor = m_WeightEditor;
            m_SliderWeightTool.weightEditor = m_WeightEditor;

            m_WeightEditorWindow.rect = CalculateWeightEditorInspectorRect();
            m_WeightEditorWindow.DockBottomRight(spriteEditor.windowDimension, new Vector2(-10f, -10f));
        }

        private void InitializeMesh()
        {
            InvalidateMesh();

            m_Mesh = new Mesh();
            m_Mesh.MarkDynamic();
            m_Mesh.hideFlags = HideFlags.DontSave;
        }

        private void InitializeMaterials()
        {
            Debug.Assert(m_MaterialVertexColor == null);

            m_MaterialVertexColor = new Material(Shader.Find("Hidden/MeshModule-GUITextureClip"));
            m_MaterialVertexColor.hideFlags = HideFlags.DontSave;
        }

        private void InvalidateMesh()
        {
            if (m_Mesh)
                UnityEngine.Object.DestroyImmediate(m_Mesh);
        }

        private void InvalidateSpriteMeshCache()
        {
            if (m_SpriteMeshCache)
            {
                Undo.ClearUndo(m_SpriteMeshCache);
                UnityEngine.Object.DestroyImmediate(m_SpriteMeshCache);
            }
        }

        private void InvalidateMaterials()
        {
            if (m_MaterialVertexColor)
                UnityEngine.Object.DestroyImmediate(m_MaterialVertexColor);
        }

        private void Repaint()
        {
            spriteEditor.RequestRepaint();
        }

        private void DrawGizmos()
        {
            if (Event.current.type == EventType.Repaint)
            {
                var selected = spriteEditor.selectedSpriteRect;
                if (selected != null)
                {
                    CommonDrawingUtility.BeginLines(CommonDrawingUtility.kSpriteBorderColor);
                    CommonDrawingUtility.DrawBox(selected.rect);
                    CommonDrawingUtility.EndLines();
                }
            }
        }

        private void DrawTriangles()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            CommonDrawingUtility.DrawTriangleLines(m_Vertices, m_Indices, 1f, Color.white.AlphaMultiplied(0.35f));
        }

        private void DrawWeights()
        {
            Debug.Assert(m_MaterialVertexColor != null);

            if (Event.current.type != EventType.Repaint)
                return;

            m_MaterialVertexColor.SetColor("_Color", Color.white.AlphaMultiplied(m_Opacity));
            DrawMesh(m_MaterialVertexColor);
        }

        private void PrepareColors()
        {
            Debug.Assert(selectedSpriteMeshData != null);

            m_Colors.Clear();

            for (int i = 0; i < selectedSpriteMeshData.vertices.Count; ++i)
            {
                BoneWeight boneWeight = selectedSpriteMeshData.vertices[i].editableBoneWeight.ToBoneWeight(true);

                m_Colors.Add(CommonDrawingUtility.CalculateNiceColor(boneWeight.boneIndex0, kNiceColorCount) * boneWeight.weight0 +
                    CommonDrawingUtility.CalculateNiceColor(boneWeight.boneIndex1, kNiceColorCount) * boneWeight.weight1 +
                    CommonDrawingUtility.CalculateNiceColor(boneWeight.boneIndex2, kNiceColorCount) * boneWeight.weight2 +
                    CommonDrawingUtility.CalculateNiceColor(boneWeight.boneIndex3, kNiceColorCount) * boneWeight.weight3);
            }
        }

        private void PrepareMesh()
        {
            Debug.Assert(selectedSpriteMeshData != null);
            Debug.Assert(m_Mesh != null);

            m_MeshDirty |= (m_Vertices.Count != selectedSpriteMeshData.vertices.Count);

            if (m_MeshDirty)
            {
                m_Mesh.Clear();
                m_VerticesDirty = true;
                m_IndicesDirty = true;
                m_ColorsDirty = true;
            }

            if (m_VerticesDirty)
            {
                m_Vertices.Clear();

                for (int i = 0; i < selectedSpriteMeshData.vertices.Count; ++i)
                    m_Vertices.Add(selectedSpriteMeshData.vertices[i].position);

                m_Mesh.SetVertices(m_Vertices);
                m_VerticesDirty = false;
            }

            if (m_IndicesDirty)
            {
                m_Indices.Clear();

                for (int i = 0; i < selectedSpriteMeshData.indices.Count; ++i)
                {
                    int index = selectedSpriteMeshData.indices[i];
                    m_Indices.Add(index);
                }

                m_Mesh.SetTriangles(m_Indices, 0);
                m_IndicesDirty = false;
            }

            if (m_ColorsDirty)
            {
                PrepareColors();

                m_Mesh.SetColors(m_Colors);
                m_ColorsDirty = false;
            }

            m_MeshDirty = false;
        }

        private void DrawMesh(Material material)
        {
            Debug.Assert(m_Mesh != null);
            Debug.Assert(material != null);

            material.SetPass(0);

            Graphics.DrawMeshNow(m_Mesh, Handles.matrix * GUI.matrix);
        }

        private void OnGenerateGeometry()
        {
            Undo.RegisterCompleteObjectUndo(m_SpriteMeshCache, "Generate Geometry");

            m_SpriteMeshCache.selection.Clear();

            selectedSpriteMeshData.OutlineFromAlpha(m_OutlineGenerator, spriteEditor.GetDataProvider<ITextureDataProvider>(), m_GenerateGeometrySettings.outlineDetail, m_GenerateGeometrySettings.alphaTolerance);
            selectedSpriteMeshData.Triangulate(m_Triangulator);

            if (m_GenerateGeometrySettings.subdividePercent > 0f)
            {
                float largestAreaFactor = Mathf.Lerp(0.5f, 0.05f, m_GenerateGeometrySettings.subdividePercent / 100f);
                selectedSpriteMeshData.Subdivide(m_Triangulator, largestAreaFactor);
            }

            m_MeshDirty = true;
            spriteEditor.SetDataModified();
            Repaint();
        }

        private Styles m_Styles;
        private int m_PrevNearestControl = -1;
        private float m_Opacity = 1f;
        private SpriteMeshCache m_SpriteMeshCache;
        private ITriangulator m_Triangulator;
        private IWeightsGenerator m_WeightGenerator;
        private IOutlineGenerator m_OutlineGenerator;
        private UndoObject m_UndoObject;
        private Mesh m_Mesh;
        private Material m_MaterialVertexColor;
        private SpriteMeshController m_SpriteMeshController;
        private SpriteMeshView m_SpriteMeshView;
        private BoneGUI m_BoneGUI;
        private RectSelectionTool m_RectSelectionTool;
        private UnselectTool m_UnselectTool;
        private WeightEditor m_WeightEditor;
        private WeightInspector m_WeightInspector;
        private BoneInspector m_BoneInspector;
        private BrushWeightTool m_BrushWeightTool;
        private SliderWeightTool m_SliderWeightTool;
        private ModuleWindow m_WeightEditorWindow;
        private ModuleWindow m_InspectorWindow;
        private List<Vector3> m_Vertices = new List<Vector3>();
        private List<int> m_Indices = new List<int>();
        private List<Color> m_Colors = new List<Color>();
        private bool m_MeshDirty = true;
        private bool m_VerticesDirty = true;
        private bool m_IndicesDirty = true;
        private bool m_ColorsDirty = true;
        private GenerateGeometryMenuContents m_GenerateGeometryMenuContents = new GenerateGeometryMenuContents();
        private GenerateGeometrySettings m_GenerateGeometrySettings = new GenerateGeometrySettings();
        private Rect m_GenerateButtonRect;
    }
}
