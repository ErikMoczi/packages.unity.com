using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEditor.Experimental.U2D.Common;
using System.Collections.Generic;

namespace UnityEditor.U2D
{
    [CustomEditor(typeof(SpriteShape)), CanEditMultipleObjects]
    public class SpriteShapeEditor : Editor, IAngleRangeCache
    {
        private static class Contents
        {
            public static readonly GUIContent fillTextureLabel = new GUIContent("Texture", "Fill texture used for Shape Fill.");
            public static readonly GUIContent fillPixelPerUnitLabel = new GUIContent("Pixel Per Unit", "Pixel Per Unit for Fill Texture.");
            public static readonly GUIContent fillScaleLabel = new GUIContent("Offset", "Determines Border Offset for Shape.");
            public static readonly GUIContent useSpriteBorderLabel = new GUIContent("Use Sprite Borders", "Draw Sprite Borders on discontinuities");
            public static readonly GUIContent cornerTypeLabel = new GUIContent("Corner Type", "Corner type sprite used.");
            public static readonly GUIContent bevelSizeLabel = new GUIContent("Bevel Size", "Length of the curve around the corners.");
            public static readonly GUIContent bevelCutoffLabel = new GUIContent("Bevel Cutoff", "Angle at which corners turn to bevels.");
            public static readonly GUIContent controlPointsLabel = new GUIContent("Control Points");
            public static readonly GUIContent fillLabel = new GUIContent("Fill");
            public static readonly GUIContent cornerLabel = new GUIContent("Corners");
            public static readonly GUIContent cornerListLabel = new GUIContent("Corner List");
            public static readonly GUIContent cornerSpriteTypeLabel = new GUIContent("Corner Sprite");
            public static readonly GUIContent angleRangesLabel = new GUIContent("Angle Ranges");
            public static readonly GUIContent spritesLabel = new GUIContent("Sprites");
            public static readonly GUIContent angleRangeLabel = new GUIContent("Angle Range ({0})");
            public static readonly GUIContent wrapModeErrorLabel = new GUIContent("Fill texture must have wrap modes set to Repeat. Please re-import.");

            public static readonly Color proBackgroundColor = new Color32(49, 77, 121, 255);
            public static readonly Color proBackgroundRangeColor = new Color32(25, 25, 25, 128);
            public static readonly Color proColor1 = new Color32(10, 46, 42, 255);
            public static readonly Color proColor2 = new Color32(33, 151, 138, 255);
            public static readonly Color defaultColor1 = new Color32(25, 61, 57, 255);
            public static readonly Color defaultColor2 = new Color32(47, 166, 153, 255);
            public static readonly Color defaultBackgroundColor = new Color32(64, 92, 136, 255);
        }

        EditorWindow m_CurrentInspectorWindow;

        private SerializedProperty m_FillTextureProp;
        private SerializedProperty m_WorldSpaceUVProp;
        private SerializedProperty m_AngleRangesProp;
        private SerializedProperty m_CornerSpritesProp;
        private SerializedProperty m_BevelCutoffProp;
        private SerializedProperty m_BevelSizeProp;
        private SerializedProperty m_FillOffsetProp;
        private SerializedProperty m_FillPixelPerUnitProp;
        private SerializedProperty m_UseSpriteBordersProp;

        private ReorderableList m_AngleRangeSpriteList = null;

        SpriteShape m_SpriteShape;
        [SerializeField]
        private float m_PreviewAngle = 0f;
        [SerializeField]
        private int m_SelectedIndex;
        private AnimBool m_FadeControlPoint;
        private AnimBool m_FadeAngleRange;
        private const int kInvalidMinimum = -1;
        private  Rect m_AngleRangeRect;
        private int m_OldNearestControl;
        private AngleRangeController controller;
        private AngleRange m_CurrentAngleRange;


        private Sprite m_PreviewSprite;
        private Mesh m_PreviewSpriteMesh;
        private Mesh previewSpriteMesh
        {
            get
            {
                if (m_PreviewSpriteMesh == null)
                {
                    m_PreviewSpriteMesh = new Mesh();
                    m_PreviewSpriteMesh.MarkDynamic();
                    m_PreviewSpriteMesh.hideFlags = HideFlags.DontSave;
                }

                return m_PreviewSpriteMesh;
            }
        }

        public List<AngleRange> angleRanges
        {
            get
            {
                Debug.Assert(m_SpriteShape != null);
                return m_SpriteShape.angleRanges;
            }
        }

        public int selectedIndex
        {
            get { return m_SelectedIndex; }
            set { m_SelectedIndex = value; }
        }

        public float previewAngle
        {
            get { return m_PreviewAngle; }
            set { m_PreviewAngle = value; }
        }

        public void RegisterUndo(string name)
        {
            Undo.RegisterCompleteObjectUndo(m_SpriteShape, name);
            Undo.RegisterCompleteObjectUndo(this, name);
            EditorUtility.SetDirty(m_SpriteShape);
        }

        public void OnEnable()
        {
            m_SpriteShape = target as SpriteShape;

            m_FillTextureProp = this.serializedObject.FindProperty("m_FillTexture");
            m_WorldSpaceUVProp = this.serializedObject.FindProperty("m_WorldSpaceUV");
            m_UseSpriteBordersProp = serializedObject.FindProperty("m_UseSpriteBorders");
            m_AngleRangesProp = this.serializedObject.FindProperty("m_Angles");
            m_CornerSpritesProp = this.serializedObject.FindProperty("m_CornerSprites");
            m_BevelCutoffProp = this.serializedObject.FindProperty("m_BevelCutoff");
            m_BevelSizeProp = this.serializedObject.FindProperty("m_BevelSize");
            m_FillOffsetProp = this.serializedObject.FindProperty("m_FillOffset");
            m_FillPixelPerUnitProp = this.serializedObject.FindProperty("m_FillPixelPerUnit");

            m_FadeControlPoint = new AnimBool(false);
            m_FadeControlPoint.valueChanged.AddListener(Repaint);

            m_FadeAngleRange = new AnimBool(false);
            m_FadeAngleRange.valueChanged.AddListener(Repaint);

            selectedIndex = SpriteShapeEditorUtility.GetRangeIndexFromAngle(angleRanges, m_PreviewAngle);

            SetupAngleRangeController();

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private void SetupAngleRangeController()
        {
            var radius = 125f;
            var angleOffset = -90f;
            var color1 = Contents.defaultColor1;
            var color2 = Contents.defaultColor2;

            if (!EditorGUIUtility.isProSkin)
            {
                color1 = Contents.proColor1;
                color2 = Contents.proColor2;
            }

            controller = new AngleRangeController();
            controller.view = new AngleRangeView();
            controller.cache = this;
            controller.radius = radius;
            controller.angleOffset = angleOffset;
            controller.gradientMin = color1;
            controller.gradientMid = color2;
            controller.gradientMax = color1;
            controller.snap = true;
            controller.OnSelectionChange += OnSelectionChange;

            OnSelectionChange();
        }

        private void OnSelectionChange()
        {
            CreateReorderableSpriteList();

            EditorApplication.delayCall += () =>
                {
                    m_CurrentAngleRange = controller.selectedAngleRange;
                };
        }

        private void OnDestroy()
        {
            m_FadeControlPoint.valueChanged.RemoveListener(Repaint);
            m_FadeAngleRange.valueChanged.RemoveListener(Repaint);

            if (m_CurrentInspectorWindow)
                m_CurrentInspectorWindow.wantsMouseMove = false;

            if (m_PreviewSpriteMesh)
                Object.DestroyImmediate(m_PreviewSpriteMesh);

            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void UndoRedoPerformed()
        {
            OnSelectionChange();
        }

        private void OnSelelectSpriteCallback(ReorderableList list)
        {
            if (selectedIndex >= 0)
            {
                SetPreviewSpriteIndexToSessionState(selectedIndex, list.index);
            }
        }

        private void OnRemoveSprite(ReorderableList list)
        {
            var count = list.count;
            var index = list.index;

            ReorderableList.defaultBehaviours.DoRemoveButton(list);

            if(list.count < count && list.count > 0)
            {
                list.index = Mathf.Clamp(index, 0, list.count - 1);
                OnSelelectSpriteCallback(list);
            }
        }

        private void DrawSpriteListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, Contents.spritesLabel);
            HandleAngleSpriteListGUI(rect);
        }

        private void DrawSpriteListElement(Rect rect, int index, bool selected, bool focused)
        {
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            var sprite = m_AngleRangesProp.GetArrayElementAtIndex(selectedIndex).FindPropertyRelative("m_Sprites").GetArrayElementAtIndex(index);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, sprite, GUIContent.none);
            if(EditorGUI.EndChangeCheck())
            {
                m_AngleRangeSpriteList.index = index;
                OnSelelectSpriteCallback(m_AngleRangeSpriteList);
            }
        }

        public void DrawHeader(GUIContent content)
        {
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
        }

        private void SetPreviewSpriteIndexToSessionState(int rangeIndex, int index)
        {
            SessionState.SetInt(m_CurrentInspectorWindow.GetInstanceID() + "/" + target.GetInstanceID() + "/" + rangeIndex, index);
        }

        private int GetPreviewSpriteIndexFromSessionState(int rangeIndex)
        {
            return SessionState.GetInt(m_CurrentInspectorWindow.GetInstanceID() + "/" + target.GetInstanceID() + "/" + rangeIndex, 0);
        }

        public override void OnInspectorGUI()
        {
            m_CurrentInspectorWindow = InternalEditorBridge.GetCurrentInspectorWindow();

            if (m_CurrentInspectorWindow)
                m_CurrentInspectorWindow.wantsMouseMove = GUIUtility.hotControl == 0;

            serializedObject.Update();

            EditorGUILayout.Space();
            DrawHeader(Contents.controlPointsLabel);
            EditorGUILayout.PropertyField(m_UseSpriteBordersProp, Contents.useSpriteBorderLabel);
            EditorGUILayout.Slider(m_BevelCutoffProp, 0f, 180f, Contents.bevelCutoffLabel);
            EditorGUILayout.Slider(m_BevelSizeProp, 0.0f, 0.5f, Contents.bevelSizeLabel);


            EditorGUILayout.Space();
            DrawHeader(Contents.fillLabel);
            EditorGUILayout.PropertyField(m_FillTextureProp, Contents.fillTextureLabel);
            EditorGUILayout.PropertyField(m_FillPixelPerUnitProp, Contents.fillPixelPerUnitLabel);
            EditorGUILayout.PropertyField(m_WorldSpaceUVProp);
            EditorGUILayout.Slider(m_FillOffsetProp, -0.5f, 0.5f, Contents.fillScaleLabel);


            if (m_FillTextureProp.objectReferenceValue != null)
            {
                var fillTex = m_FillTextureProp.objectReferenceValue as Texture2D;
                if (fillTex.wrapModeU != TextureWrapMode.Repeat || fillTex.wrapModeV != TextureWrapMode.Repeat)
                    EditorGUILayout.HelpBox(Contents.wrapModeErrorLabel.text, MessageType.Info);
            }

            EditorGUILayout.Space();
            DrawHeader(Contents.angleRangesLabel);
            DoRangesGUI();

            if (targets.Length == 1)
            {
                m_FadeAngleRange.target = m_CurrentAngleRange != null;
                if (EditorGUILayout.BeginFadeGroup(m_FadeAngleRange.faded))
                {
                    if (m_FadeAngleRange.target)
                        DoRangeInspector();
                }

                EditorGUILayout.EndFadeGroup();

                DoCreateRangeButton();
            }

            EditorGUILayout.Space();
            DrawHeader(Contents.cornerLabel);

            EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth + 20f;

            for (int i = 0; i < m_CornerSpritesProp.arraySize; ++i)
            {
                var m_CornerProp = m_CornerSpritesProp.GetArrayElementAtIndex(i);
                var m_CornerType = m_CornerProp.FindPropertyRelative("m_CornerType");
                var m_CornerSprite = m_CornerProp.FindPropertyRelative("m_Sprites").GetArrayElementAtIndex(0);

                EditorGUILayout.PropertyField(m_CornerSprite, new GUIContent(m_CornerType.enumDisplayNames[m_CornerType.intValue]));
            }

            EditorGUIUtility.labelWidth = 0;

            serializedObject.ApplyModifiedProperties();

            HandleRepaintOnHover();

            controller.view.DoCreateRangeTooltip();
        }

        private void DoRangeInspector()
        {
            Debug.Assert(m_CurrentAngleRange != null);

            var start = m_CurrentAngleRange.start;
            var end = m_CurrentAngleRange.end;
            var order = m_CurrentAngleRange.order;

            DrawHeader(new GUIContent(string.Format(Contents.angleRangeLabel.text, (end - start))));

            EditorGUIUtility.labelWidth = 0f;
            EditorGUI.BeginChangeCheck();

            RangeField(ref start, ref end, ref order);

            if (EditorGUI.EndChangeCheck())
            {
                RegisterUndo("Set Range");

                m_CurrentAngleRange.order = order;
                controller.SetRange(m_CurrentAngleRange, start, end);

                if (start >= end)
                    controller.RemoveInvalidRanges();
            }

            EditorGUILayout.Space();

            if (m_AngleRangeSpriteList != null)
                m_AngleRangeSpriteList.DoLayoutList();
        }

        private void DoCreateRangeButton()
        {
            if (selectedIndex != kInvalidMinimum)
                return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create Range", GUILayout.MaxWidth(100f)))
            {
                RegisterUndo("Create Range");
                controller.CreateRange();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void RangeField(ref float start, ref float end, ref int order)
        {
            var values = new int[] { Mathf.RoundToInt(-start), Mathf.RoundToInt(-end), order };
            var labels = new GUIContent[] { new GUIContent("Start"), new GUIContent("End"), new GUIContent("Order")  };

            var position = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            SpriteShapeEditorGUI.MultiDelayedIntField(position, labels, values, 40f);
            if (EditorGUI.EndChangeCheck())
            {
                start = -1f * values[0];
                end = -1f * values[1];
                order = values[2];
            }
        }

        private void HandleAngleSpriteListGUI(Rect rect)
        {
            if (m_CurrentAngleRange == null)
                return;

            var currentEvent = Event.current;
            var usedEvent = false;
            var sprites = m_AngleRangesProp.GetArrayElementAtIndex(selectedIndex).FindPropertyRelative("m_Sprites");
            switch (currentEvent.type)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (rect.Contains(currentEvent.mousePosition) && GUI.enabled)
                    {
                        // Check each single object, so we can add multiple objects in a single drag.
                        var didAcceptDrag = false;
                        var references = DragAndDrop.objectReferences;
                        foreach (var obj in references)
                        {
                            if (obj is Sprite)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                if (currentEvent.type == EventType.DragPerform)
                                {
                                    sprites.InsertArrayElementAtIndex(sprites.arraySize);
                                    var spriteProp = sprites.GetArrayElementAtIndex(sprites.arraySize - 1);
                                    spriteProp.objectReferenceValue = obj;
                                    didAcceptDrag = true;
                                    DragAndDrop.activeControlID = 0;
                                }
                            }
                        }

                        serializedObject.ApplyModifiedProperties();

                        if (didAcceptDrag)
                        {
                            GUI.changed = true;
                            DragAndDrop.AcceptDrag();
                            usedEvent = true;
                        }
                    }
                    break;
            }

            if (usedEvent)
                currentEvent.Use();
        }

        private void HandleRepaintOnHover()
        {
            bool canRepaint = m_CurrentInspectorWindow && m_CurrentInspectorWindow.wantsMouseMove && GUIUtility.hotControl == 0 && m_AngleRangeRect.Contains(Event.current.mousePosition);

            if (canRepaint && Event.current.type == EventType.Layout)
            {
                if (HandleUtility.nearestControl != m_OldNearestControl)
                    HandleUtility.Repaint();
            }

            m_OldNearestControl = HandleUtility.nearestControl;
        }

        private void DoRangesGUI()
        {
            var radius = controller.radius;

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            var rect = EditorGUILayout.GetControlRect(false, radius * 2f);

            if (Event.current.type == EventType.Repaint)
                m_AngleRangeRect = rect;

            {   //Draw background
                var backgroundColor = Contents.proBackgroundColor;
                var backgroundRangeColor = Contents.proBackgroundRangeColor;

                if (!EditorGUIUtility.isProSkin)
                {
                    backgroundColor = Contents.defaultBackgroundColor;
                    backgroundRangeColor.a = 0.1f;
                }
                var c = Handles.color;
                Handles.color = backgroundRangeColor;
                SpriteShapeHandleUtility.DrawSolidArc(rect.center, Vector3.forward, Vector3.right, 360f, radius, AngleRangeGUI.kRangeWidth);
                Handles.color = backgroundColor;
                Handles.DrawSolidDisc(rect.center, Vector3.forward, radius - AngleRangeGUI.kRangeWidth + 1f);
                Handles.color = c;
            }

            if (targets.Length == 1)
            {
                {   //Draw fill texture and sprite preview
                    SpriteShapeHandleUtility.DrawTextureArc(
                        m_FillTextureProp.objectReferenceValue as Texture, m_FillPixelPerUnitProp.floatValue,
                        rect.center, Vector3.forward, Quaternion.AngleAxis(m_PreviewAngle, Vector3.forward) * Vector3.right, 180f,
                        radius - AngleRangeGUI.kRangeWidth);

                    var rectSize = Vector2.one * (radius - AngleRangeGUI.kRangeWidth) * 2f;
                    rectSize.y *= 0.33f;
                    var spriteRect = new Rect(rect.center - rectSize * 0.5f, rectSize);
                    DrawSpritePreview(spriteRect);
                    HandleSpritePreviewCycle(spriteRect);
                }

                controller.rect = m_AngleRangeRect;
                controller.OnGUI();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        private void CreateReorderableSpriteList()
        {
            m_AngleRangeSpriteList = null;

            serializedObject.UpdateIfRequiredOrScript();

            Debug.Assert(angleRanges.Count == m_AngleRangesProp.arraySize);
            Debug.Assert(selectedIndex < angleRanges.Count);

            if (targets.Length == 1 && selectedIndex != kInvalidMinimum)
            {
                var spritesProp = m_AngleRangesProp.GetArrayElementAtIndex(selectedIndex).FindPropertyRelative("m_Sprites");
                m_AngleRangeSpriteList = new ReorderableList(spritesProp.serializedObject, spritesProp)
                {
                    drawElementCallback = DrawSpriteListElement,
                    drawHeaderCallback = DrawSpriteListHeader,
                    onSelectCallback = OnSelelectSpriteCallback,
                    onRemoveCallback = OnRemoveSprite,
                    elementHeight = EditorGUIUtility.singleLineHeight + 6f
                };
            }
        }

        private void DrawSpritePreview(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (selectedIndex == kInvalidMinimum)
                return;

            var sprites = angleRanges[selectedIndex].sprites;

            if (sprites.Count == 0)
                return;

            var selectedSpriteIndex = GetPreviewSpriteIndexFromSessionState(selectedIndex);

            if (selectedSpriteIndex == kInvalidMinimum)
                return;

            var sprite = sprites[selectedSpriteIndex];

            if (sprite == null)
                return;

            if (m_PreviewSprite != sprite)
            {
                m_PreviewSprite = sprite;
                EditorSpriteGUIUtility.DrawSpriteInRectPrepare(rect, sprite, EditorSpriteGUIUtility.FitMode.Tiled, true, true, previewSpriteMesh);
            }

            var material = EditorSpriteGUIUtility.spriteMaterial;
            material.mainTexture = EditorSpriteGUIUtility.GetOriginalSpriteTexture(sprite);

            GUI.BeginClip(rect);
            EditorSpriteGUIUtility.DrawMesh(previewSpriteMesh, material, rect.size * 0.5f, Quaternion.AngleAxis(m_PreviewAngle, Vector3.forward), new Vector3(1f, -1f, 1f));
            GUI.EndClip();
        }

        private void HandleSpritePreviewCycle(Rect rect)
        {
            if (selectedIndex == kInvalidMinimum)
                return;

            Debug.Assert(m_AngleRangeSpriteList != null);

            var spriteIndex = GetPreviewSpriteIndexFromSessionState(selectedIndex);
            var sprites = angleRanges[selectedIndex].sprites;

            var ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && HandleUtility.nearestControl == 0 &&
                ContainsPosition(rect, ev.mousePosition, m_PreviewAngle) && spriteIndex != kInvalidMinimum && sprites.Count > 0)
            {
                spriteIndex = Mathf.RoundToInt(Mathf.Repeat(spriteIndex + 1f, sprites.Count));
                SetPreviewSpriteIndexToSessionState(selectedIndex, spriteIndex);

                m_AngleRangeSpriteList.GrabKeyboardFocus();
                m_AngleRangeSpriteList.index = spriteIndex;

                ev.Use();
            }
        }

        private bool ContainsPosition(Rect rect, Vector2 position, float angle)
        {
            Vector2 delta = position - rect.center;
            position = (Vector2)(Quaternion.AngleAxis(-angle, Vector3.forward) * (Vector3)delta) + rect.center;
            return rect.Contains(position);
        }
    }
}
