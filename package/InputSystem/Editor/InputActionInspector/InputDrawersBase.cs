#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    internal abstract class InputDrawersBase : PropertyDrawer
    {
        private static class Styles
        {
            public static GUIStyle actionTreeBackground = new GUIStyle("Label");
            public static GUIStyle columnHeaderLabel = new GUIStyle(EditorStyles.toolbar);

            static Styles()
            {
                actionTreeBackground.normal.background =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        InputActionTreeBase.ResourcesPath + "actionTreeBackground.png");
                actionTreeBackground.border = new RectOffset(3, 3, 3, 3);

                columnHeaderLabel.alignment = TextAnchor.MiddleLeft;
                columnHeaderLabel.fontStyle = FontStyle.Bold;
                columnHeaderLabel.padding.left = 10;
            }
        }

        protected InspectorTree m_Tree;
        private InputActionCopyPasteUtility m_CopyPasteUtility;

        protected readonly GUIContent m_BindingGUI = EditorGUIUtility.TrTextContent("Binding");
        protected readonly GUIContent m_ActionGUI = EditorGUIUtility.TrTextContent("Action");
        protected readonly GUIContent m_CompositeGUI = EditorGUIUtility.TrTextContent("Composite");
        private readonly GUIContent m_PlusIconContent = EditorGUIUtility.IconContent("Toolbar Plus");
        private readonly GUIContent m_MinusIconContent = EditorGUIUtility.IconContent("Toolbar Minus");

        protected InputDrawersBase()
        {
            Undo.undoRedoPerformed += OnUndoRedoCallback;
        }

        private void OnUndoRedoCallback()
        {
            if (m_Tree == null)
            {
                //TODO how to unregister it in a better way?
                Undo.undoRedoPerformed -= OnUndoRedoCallback;
                return;
            }
            // Force tree rebuild
            m_Tree = null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitTreeIfNeeded(property);
            if (m_Tree.totalHeight == 0)
            {
                return Styles.columnHeaderLabel.fixedHeight + 10;
            }
            return Styles.columnHeaderLabel.fixedHeight + EditorGUIUtility.standardVerticalSpacing + m_Tree.totalHeight;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SetActionNameIfNotSet(property);

            var labelRect = position;
            EditorGUI.LabelField(labelRect, GUIContent.none, Styles.actionTreeBackground);
            var headerRect = new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width - 2, labelRect.height - 2);
            EditorGUI.LabelField(headerRect, label, Styles.columnHeaderLabel);

            labelRect.x = labelRect.width - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            labelRect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (GUI.Button(labelRect, m_PlusIconContent, GUIStyle.none))
            {
                OpenAddMenu(property);
            }

            labelRect.x += labelRect.width;
            if (GUI.Button(labelRect, m_MinusIconContent, GUIStyle.none))
            {
                EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Delete"));
            }

            InitTreeIfNeeded(property);

            position.y += Styles.columnHeaderLabel.fixedHeight;
            var treeRect = new Rect(position.x + 1, position.y + 1,
                position.width - 2, position.height - Styles.columnHeaderLabel.fixedHeight - 2);

            m_Tree.OnGUI(treeRect);

            if (m_Tree.HasFocus())
            {
                if (Event.current.type == EventType.ValidateCommand)
                {
                    if (InputActionCopyPasteUtility.IsValidCommand(Event.current.commandName))
                        Event.current.Use();
                }
                else if (Event.current.type == EventType.ExecuteCommand)
                {
                    m_CopyPasteUtility.HandleCommandEvent(Event.current.commandName);
                }
            }

            EditorGUI.EndProperty();
        }

        private void InitTreeIfNeeded(SerializedProperty property)
        {
            if (m_Tree == null)
            {
                m_Tree = CreateTree(property);
                m_Tree.OnContextClick = OnContextClick;
                m_CopyPasteUtility = new InputActionCopyPasteUtility(m_Tree);
            }
        }

        private void OnContextClick(SerializedProperty serializedProperty)
        {
            var menu = new GenericMenu();
            m_CopyPasteUtility.AddOptionsToMenu(menu);
            menu.ShowAsContext();
        }

        private void SetActionNameIfNotSet(SerializedProperty actionProperty)
        {
            var nameProperty = actionProperty.FindPropertyRelative("m_Name");
            if (!string.IsNullOrEmpty(nameProperty.stringValue))
                return;

            var suffix = GetSuffix();
            var name = actionProperty.displayName;
            if (name.EndsWith(suffix))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }
            nameProperty.stringValue = name;

            // Don't apply. Let's apply it as a side-effect whenever something about
            // the action in the UI is changed.
        }

        protected abstract void OpenAddMenu(SerializedProperty property);
        protected abstract InspectorTree CreateTree(SerializedProperty property);
        protected abstract string GetSuffix();
    }
}
#endif // UNITY_EDITOR
