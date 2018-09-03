using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEditor.Experimental.U2D.Common;

namespace UnityEditor.U2D
{
    [CustomEditor(typeof(SpriteShape)), CanEditMultipleObjects]
    public class SpriteShapeEditor : Editor
    {
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
        private int m_SelectedAngleRange;
		private AnimBool m_FadeControlPoint;
		private AnimBool m_FadeAngleRange;
        private const int kInvalidMinimum = -1;
		private  Rect m_HoverRepaintRect;
		private int m_OldNearestControl;

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

            public static readonly Color proBackgroundColor = new Color32(49, 77, 121, 255);
            public static readonly Color proBackgroundRangeColor = new Color32(25, 25, 25, 128);
            public static readonly Color proColor1 = new Color32(10, 46, 42, 255);
            public static readonly Color proColor2 = new Color32(33, 151, 138, 255);
            public static readonly Color defaultColor1 = new Color32(25, 61, 57, 255);
            public static readonly Color defaultColor2 = new Color32(47, 166, 153, 255);
            public static readonly Color defaultBackgroundColor = new Color32(64, 92, 136, 255);
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

            m_FadeAngleRange = new AnimBool(m_SelectedAngleRange >= 0 && m_AngleRangesProp.arraySize > m_SelectedAngleRange);
            m_FadeAngleRange.valueChanged.AddListener(Repaint);

			m_SelectedAngleRange = SpriteShapeEditorUtility.GetRangeIndexFromAngle(m_SpriteShape, m_PreviewAngle);
            CreateReorderableSpriteList();

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        private void OnDisable()
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
			m_SelectedAngleRange = SpriteShapeEditorUtility.GetRangeIndexFromAngle(m_SpriteShape, m_PreviewAngle);
            CreateReorderableSpriteList();
        }

        private void OnSelelectSpriteCallback(ReorderableList list)
        {
            if (m_SelectedAngleRange >= 0)
            {
                SetPreviewSpriteIndexToSessionState(m_SelectedAngleRange, list.index);
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
            var sprite = m_AngleRangesProp.GetArrayElementAtIndex(m_SelectedAngleRange).FindPropertyRelative("m_Sprites").GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, sprite, GUIContent.none);
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
            bool deleteSelected = false;

            m_CurrentInspectorWindow = InternalEditorBridge.GetCurrentInspectorWindow();

            if (m_CurrentInspectorWindow)
                m_CurrentInspectorWindow.wantsMouseMove = GUIUtility.hotControl == 0;

            serializedObject.Update();

            EditorGUILayout.Space();
            DrawHeader(Contents.controlPointsLabel);
			EditorGUILayout.PropertyField(m_UseSpriteBordersProp, Contents.useSpriteBorderLabel);
            EditorGUILayout.Slider(m_BevelCutoffProp, 0f, 180f, Contents.bevelCutoffLabel);
            EditorGUILayout.Slider(m_BevelSizeProp, 0.05f, 0.5f, Contents.bevelSizeLabel);


            EditorGUILayout.Space();
            DrawHeader(Contents.fillLabel);
            EditorGUILayout.PropertyField(m_FillTextureProp, Contents.fillTextureLabel);
            EditorGUILayout.PropertyField(m_FillPixelPerUnitProp, Contents.fillPixelPerUnitLabel);
            EditorGUILayout.PropertyField(m_WorldSpaceUVProp);
            EditorGUILayout.Slider(m_FillOffsetProp, -0.5f, 0.5f, Contents.fillScaleLabel);

            EditorGUILayout.Space();
            DrawHeader(Contents.angleRangesLabel);
            DoRangesGUI();

            m_FadeAngleRange.target = (m_SelectedAngleRange >= 0 && m_SelectedAngleRange < m_AngleRangesProp.arraySize);
            if (EditorGUILayout.BeginFadeGroup(m_FadeAngleRange.faded))
            {
                if (m_FadeAngleRange.target && m_AngleRangeSpriteList != null)
                {
                    SerializedProperty selectedRangeProp = m_AngleRangesProp.GetArrayElementAtIndex(m_SelectedAngleRange);
                    SerializedProperty startProp = selectedRangeProp.FindPropertyRelative("m_Start");
                    SerializedProperty endProp = selectedRangeProp.FindPropertyRelative("m_End");

                    float prevStart = startProp.floatValue;
                    float prevEnd = endProp.floatValue;

                    DrawHeader(new GUIContent(string.Format(Contents.angleRangeLabel.text, (prevEnd - prevStart))));

                    EditorGUIUtility.labelWidth = 0f;
                    EditorGUI.BeginChangeCheck();

					RangeField(selectedRangeProp);

                    if (EditorGUI.EndChangeCheck())
                    {
						AngleRangeGUI.ValidateRange(m_AngleRangesProp, m_SelectedAngleRange, prevStart, prevEnd);

                        if (startProp.floatValue == endProp.floatValue)
                        {
                            deleteSelected = true;
                        }
                        else
                        {
                            Undo.RegisterCompleteObjectUndo(this, Undo.GetCurrentGroupName());
                            m_PreviewAngle = Mathf.Clamp(m_PreviewAngle, startProp.floatValue, endProp.floatValue);
                        }
                    }

                    EditorGUILayout.Space();

                    m_AngleRangeSpriteList.DoLayoutList();
                }
            }

            EditorGUILayout.EndFadeGroup();

            if (m_SelectedAngleRange == -1)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Create Range", GUILayout.MaxWidth(100f)))
                {
					m_SelectedAngleRange = AngleRangeGUI.HandleAddRangeFromAngle(m_AngleRangesProp, m_PreviewAngle, m_SelectedAngleRange);
                    CreateReorderableSpriteList();
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            DrawHeader(Contents.cornerLabel);

            EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth + 20f;

            for (int i = 0; i < m_CornerSpritesProp.arraySize; ++i)
            {
                SerializedProperty m_CornerProp = m_CornerSpritesProp.GetArrayElementAtIndex(i);
                SerializedProperty m_CornerType = m_CornerProp.FindPropertyRelative("m_CornerType");
                SerializedProperty m_CornerSprite = m_CornerProp.FindPropertyRelative("m_Sprites").GetArrayElementAtIndex(0);

                EditorGUILayout.PropertyField(m_CornerSprite, new GUIContent(m_CornerType.enumDisplayNames[m_CornerType.intValue]));
            }

            EditorGUIUtility.labelWidth = 0;

            serializedObject.ApplyModifiedProperties();

            HandleRepaintOnHover();

            if (deleteSelected)
            {
                m_AngleRangesProp.DeleteArrayElementAtIndex(m_SelectedAngleRange);
                m_SelectedAngleRange = -1;
            }
        }

		private void RangeField(SerializedProperty selectedRangeProp)
		{
			SerializedProperty startProp = selectedRangeProp.FindPropertyRelative("m_Start");
			SerializedProperty endProp = selectedRangeProp.FindPropertyRelative("m_End");
			SerializedProperty orderProp = selectedRangeProp.FindPropertyRelative("m_Order");

			int[] values = new int[] { Mathf.RoundToInt(-startProp.floatValue), Mathf.RoundToInt(-endProp.floatValue), orderProp.intValue };
			GUIContent[] labels = new GUIContent[] { new GUIContent("Start"), new GUIContent("End"), new GUIContent("Order")  };

			Rect position = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
			EditorGUI.BeginChangeCheck();
			SpriteShapeEditorGUI.MultiDelayedIntField(position, labels, values, 40f);
			if(EditorGUI.EndChangeCheck())
			{
				startProp.floatValue = -1f * values[0];
				endProp.floatValue = -1f * values[1];
				orderProp.intValue = values[2];
			}
		}

        private void HandleAngleSpriteListGUI(Rect rect)
        {
            var currentEvent = Event.current;
            var usedEvent = false;
            SerializedProperty sprites = m_AngleRangesProp.GetArrayElementAtIndex(m_SelectedAngleRange).FindPropertyRelative("m_Sprites");
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
                                    SerializedProperty spriteProp = sprites.GetArrayElementAtIndex(sprites.arraySize - 1);
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
            bool canRepaint = m_CurrentInspectorWindow && m_CurrentInspectorWindow.wantsMouseMove && GUIUtility.hotControl == 0 && m_HoverRepaintRect.Contains(Event.current.mousePosition);

            if (canRepaint && Event.current.type == EventType.Layout)
            {
                if (HandleUtility.nearestControl != m_OldNearestControl)
                    HandleUtility.Repaint();
            }

            m_OldNearestControl = HandleUtility.nearestControl;
        }

        private void DoRangesGUI()
        {
            int selectedAngle = m_SelectedAngleRange;
            bool selectedAngleChanged = false;
            float radius = 125f;
            float angleOffset = -90f;
            Color backgroundColor = Contents.proBackgroundColor;
            Color backgroundRangeColor = Contents.proBackgroundRangeColor;
            Color color1 = Contents.proColor1;
            Color color2 = Contents.proColor2;

            if (!EditorGUIUtility.isProSkin)
            {
                color1 = Contents.defaultColor1;
                color2 = Contents.defaultColor2;
                backgroundColor = Contents.defaultBackgroundColor;
                backgroundRangeColor.a = 0.1f;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            Rect rect = EditorGUILayout.GetControlRect(false, radius * 2f);

            if (Event.current.type == EventType.Repaint)
                m_HoverRepaintRect = rect;

            Color c = Handles.color;
            Handles.color = backgroundRangeColor;
			SpriteShapeHandleUtility.DrawSolidArc(rect.center, Vector3.forward, Vector3.right, 360f, radius, AngleRangeGUI.kRangeWidth);
            Handles.color = backgroundColor;
			Handles.DrawSolidDisc(rect.center, Vector3.forward, radius - AngleRangeGUI.kRangeWidth + 1f);
            Handles.color = c;

            if (!m_AngleRangesProp.hasMultipleDifferentValues)
            {
				SpriteShapeHandleUtility.DrawTextureArc(
                    m_FillTextureProp.objectReferenceValue as Texture, m_FillPixelPerUnitProp.floatValue,
                    rect.center, Vector3.forward, Quaternion.AngleAxis(m_PreviewAngle, Vector3.forward) * Vector3.right, 180f,
					radius - AngleRangeGUI.kRangeWidth);

				Vector2 rectSize = Vector2.one * (radius - AngleRangeGUI.kRangeWidth) * 2f;
                rectSize.y *= 0.33f;
                Rect spriteRect = new Rect(rect.center - rectSize * 0.5f, rectSize);
                DrawSpritePreview(spriteRect);

                HandleSpritePreviewCycle(spriteRect);

                int previewControlId = GUIUtility.GetControlID("PreviewAngle".GetHashCode(), FocusType.Passive);

                if (GUIUtility.hotControl == previewControlId)
					selectedAngle = SpriteShapeEditorUtility.GetRangeIndexFromAngle(m_SpriteShape, m_PreviewAngle);

                if (m_AngleRangesProp.arraySize == 0)
                    selectedAngle = -1;

                EditorGUI.BeginChangeCheck();

				int newSelected = AngleRangeGUI.AngleRangeListField(rect, m_AngleRangesProp, selectedAngle, angleOffset, radius, true, color1, color2, color1);
				newSelected = AngleRangeGUI.HandleAddRange(rect, m_AngleRangesProp, newSelected, radius, angleOffset);
				newSelected = AngleRangeGUI.HandleRemoveRange(m_AngleRangesProp, newSelected);

                if (EditorGUI.EndChangeCheck())
                {
                    if (newSelected >= 0 && newSelected < m_AngleRangesProp.arraySize)
                    {
                        Undo.RegisterCompleteObjectUndo(this, Undo.GetCurrentGroupName());
                        SerializedProperty selectedRangeProp = m_AngleRangesProp.GetArrayElementAtIndex(newSelected);
                        SerializedProperty startProp = selectedRangeProp.FindPropertyRelative("m_Start");
                        SerializedProperty endProp = selectedRangeProp.FindPropertyRelative("m_End");

                        if (Event.current.type == EventType.MouseDown)
                        {
							AngleRangeGUI.AngleFieldState state = AngleRangeGUI.GetAngleFieldState(previewControlId);
							m_PreviewAngle = SpriteShapeHandleUtility.PosToAngle(Event.current.mousePosition, state.rect.center, -angleOffset);
                            m_PreviewAngle = Mathf.Repeat(m_PreviewAngle - startProp.floatValue, 360f) + startProp.floatValue;
                            HandleUtility.nearestControl = previewControlId;
                        }

                        float angleBeforeClamp = m_PreviewAngle;

                        m_PreviewAngle = Mathf.Clamp(m_PreviewAngle, startProp.floatValue, endProp.floatValue);

                        float rangeLength = endProp.floatValue - startProp.floatValue;
                        float distanceToStart = startProp.floatValue - angleBeforeClamp;
                        float distanceToEnd = endProp.floatValue - angleBeforeClamp;
                        float angleDelta = Mathf.Abs(angleBeforeClamp - m_PreviewAngle);

                        if (distanceToStart < distanceToEnd && angleDelta > rangeLength)
                        {
                            if (m_PreviewAngle == endProp.floatValue)
                                m_PreviewAngle = startProp.floatValue;
                            else if (m_PreviewAngle == startProp.floatValue)
                                m_PreviewAngle = endProp.floatValue;
                        }
                    }

                    selectedAngleChanged = newSelected != m_SelectedAngleRange;
                }

                EditorGUI.BeginChangeCheck();

				float newPreviewAngle = AngleRangeGUI.AngleField(rect, previewControlId, m_PreviewAngle, angleOffset, Vector2.down * 7.5f, m_PreviewAngle, 15f, radius - AngleRangeGUI.kRangeWidth, true, true, false, SpriteShapeHandleUtility.PlayHeadCap);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(this, Undo.GetCurrentGroupName());
					newSelected = SpriteShapeEditorUtility.GetRangeIndexFromAngle(m_SpriteShape, newPreviewAngle);
					m_PreviewAngle = newPreviewAngle;
                    selectedAngleChanged = newSelected != m_SelectedAngleRange;
                }

                m_SelectedAngleRange = newSelected;

                if (m_SelectedAngleRange >= 0 && m_SelectedAngleRange < m_AngleRangesProp.arraySize)
                {
                    SerializedProperty selectedRangeProp = m_AngleRangesProp.GetArrayElementAtIndex(m_SelectedAngleRange);
                    SerializedProperty startProp = selectedRangeProp.FindPropertyRelative("m_Start");

                    m_PreviewAngle = Mathf.Repeat(m_PreviewAngle - startProp.floatValue, 360f) + startProp.floatValue;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (selectedAngleChanged)
                CreateReorderableSpriteList();
        }

        private void CreateReorderableSpriteList()
        {
            m_AngleRangeSpriteList = null;

            if (m_SelectedAngleRange != kInvalidMinimum && m_SelectedAngleRange < m_AngleRangesProp.arraySize)
            {
                SerializedObject angleRange = m_AngleRangesProp.GetArrayElementAtIndex(m_SelectedAngleRange).serializedObject;
                SerializedProperty sprites = m_AngleRangesProp.GetArrayElementAtIndex(m_SelectedAngleRange).FindPropertyRelative("m_Sprites");
                m_AngleRangeSpriteList = new ReorderableList(angleRange, sprites)
                {
                    drawElementCallback = DrawSpriteListElement,
                    drawHeaderCallback = DrawSpriteListHeader,
                    onSelectCallback = OnSelelectSpriteCallback,
                    elementHeight = EditorGUIUtility.singleLineHeight + 6f
                };
            }
        }

        private void DrawSpritePreview(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

			Material material = EditorSpriteGUIUtility.spriteMaterial;

			int rangeIndex = SpriteShapeEditorUtility.GetRangeIndexFromAngle(m_SpriteShape, m_PreviewAngle);
            int selectedSpriteIndex = GetPreviewSpriteIndexFromSessionState(rangeIndex);

			Sprite sprite = SpriteShapeEditorUtility.GetSpriteFromAngle(m_SpriteShape, m_PreviewAngle, selectedSpriteIndex);

            if (!sprite)
                return;

            if (m_PreviewSprite != sprite)
            {
                m_PreviewSprite = sprite;
				EditorSpriteGUIUtility.DrawSpriteInRectPrepare(rect, sprite, EditorSpriteGUIUtility.FitMode.Tiled, true, true, previewSpriteMesh);
            }

			material.mainTexture = EditorSpriteGUIUtility.GetOriginalSpriteTexture(sprite);

            GUI.BeginClip(rect);
			EditorSpriteGUIUtility.DrawMesh(previewSpriteMesh, material, rect.size * 0.5f, Quaternion.AngleAxis(m_PreviewAngle, Vector3.forward), new Vector3(1f, -1f, 1f));
            GUI.EndClip();
        }

        private void HandleSpritePreviewCycle(Rect rect)
        {
            if (m_AngleRangesProp.arraySize == 0)
                return;

            Event ev = Event.current;

			int rangeIndex = SpriteShapeEditorUtility.GetRangeIndexFromAngle(m_SpriteShape, m_PreviewAngle);
            int spriteIndex = GetPreviewSpriteIndexFromSessionState(rangeIndex);

            if (rangeIndex != kInvalidMinimum)
            {
                SerializedProperty spritesProp = m_AngleRangesProp.GetArrayElementAtIndex(rangeIndex).FindPropertyRelative("m_Sprites");

                if (ev.type == EventType.MouseDown && ev.button == 0 && HandleUtility.nearestControl == 0 &&
                    ContainsPosition(rect, ev.mousePosition, m_PreviewAngle) && spriteIndex != kInvalidMinimum && spritesProp.arraySize > 0)
                {
                    spriteIndex = Mathf.RoundToInt(Mathf.Repeat(spriteIndex + 1f, spritesProp.arraySize));
                    SetPreviewSpriteIndexToSessionState(rangeIndex, spriteIndex);

                    if (rangeIndex == m_SelectedAngleRange && m_AngleRangeSpriteList != null)
                        m_AngleRangeSpriteList.index = spriteIndex;

                    ev.Use();
                }
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
