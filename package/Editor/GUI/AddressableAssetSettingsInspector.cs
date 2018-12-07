using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets
{
    [CustomEditor(typeof(AddressableAssetSettings))]
    class AddressableAssetSettingsInspector : Editor
    {
        AddressableAssetSettings m_AasTarget;

        [FormerlySerializedAs("m_generalFoldout")]
        [SerializeField]
        bool m_GeneralFoldout = true;
        [FormerlySerializedAs("m_groupFoldout")]
        [SerializeField]
        bool m_GroupFoldout = true;
        [FormerlySerializedAs("m_profilesFoldout")]
        [SerializeField]
        bool m_ProfilesFoldout = true;
        [FormerlySerializedAs("m_labelsFoldout")]
        [SerializeField]
        bool m_LabelsFoldout = true;
        [FormerlySerializedAs("m_dataBuildersFoldout")]
        [SerializeField]
        bool m_DataBuildersFoldout = true;
        [FormerlySerializedAs("m_schemaTemplatesFoldout")]
        [SerializeField]
        bool m_SchemaTemplatesFoldout = true;
        [FormerlySerializedAs("m_initObjectsFoldout")]
        [SerializeField]
        bool m_InitObjectsFoldout = true;

        [FormerlySerializedAs("m_profileEntriesRL")]
        [SerializeField]
        ReorderableList m_ProfileEntriesRl;
        [FormerlySerializedAs("m_labelNamesRL")]
        [SerializeField]
        ReorderableList m_LabelNamesRl;
        [FormerlySerializedAs("m_dataBuildersRL")]
        [SerializeField]
        ReorderableList m_DataBuildersRl;
        [FormerlySerializedAs("m_schemaTemplatesRL")]
        [SerializeField]
        ReorderableList m_SchemaTemplatesRl;
        [FormerlySerializedAs("m_initObjectsRL")]
        [SerializeField]
        ReorderableList m_InitObjectsRl;

        [FormerlySerializedAs("m_currentProfileIndex")]
        [SerializeField]
        int m_CurrentProfileIndex = -1;

        void OnEnable()
        {
            m_AasTarget = target as AddressableAssetSettings;
            if (m_AasTarget == null)
                return;

            var names = m_AasTarget.profileSettings.profileEntryNames;
            m_ProfileEntriesRl = new ReorderableList(names, typeof(AddressableAssetProfileSettings.ProfileIdData), true, true, true, true);
            m_ProfileEntriesRl.drawElementCallback = DrawProfileEntriesCallback;
            m_ProfileEntriesRl.drawHeaderCallback = DrawProfileEntriesHeader;
            m_ProfileEntriesRl.onAddCallback = OnAddProfileEntry;
            m_ProfileEntriesRl.onRemoveCallback = OnRemoveProfileEntry;

            var labels = m_AasTarget.labelTable.labelNames;
            m_LabelNamesRl = new ReorderableList(labels, typeof(string), true, true, true, true);
            m_LabelNamesRl.drawElementCallback = DrawLabelNamesCallback;
            m_LabelNamesRl.drawHeaderCallback = DrawLabelNamesHeader;
            m_LabelNamesRl.onAddDropdownCallback = OnAddLabel;
            m_LabelNamesRl.onRemoveCallback = OnRemoveLabel;

            m_DataBuildersRl = new ReorderableList(m_AasTarget.DataBuilders, typeof(ScriptableObject), true, true, true, true);
            m_DataBuildersRl.drawElementCallback = DrawDataBuilderCallback;
            m_DataBuildersRl.drawHeaderCallback = DrawDataBuilderHeader;
            m_DataBuildersRl.onAddDropdownCallback = OnAddDataBuilder;
            m_DataBuildersRl.onRemoveCallback = OnRemoveDataBuilder;

            m_SchemaTemplatesRl = new ReorderableList(m_AasTarget.SchemaTemplates, typeof(ScriptableObject), true, true, true, true);
            m_SchemaTemplatesRl.drawElementCallback = DrawSchemaTemplateCallback;
            m_SchemaTemplatesRl.drawHeaderCallback = DrawSchemaTemplateHeader;
            m_SchemaTemplatesRl.onAddDropdownCallback = OnAddSchemaTemplate;
            m_SchemaTemplatesRl.onRemoveCallback = OnRemoveSchemaTemplate;

            m_InitObjectsRl = new ReorderableList(m_AasTarget.InitializationObjects, typeof(ScriptableObject), true, true, true, true);
            m_InitObjectsRl.drawElementCallback = DrawInitializationObjectCallback;
            m_InitObjectsRl.drawHeaderCallback = DrawInitializationObjectHeader;
            m_InitObjectsRl.onAddDropdownCallback = OnAddInitializationObject;
            m_InitObjectsRl.onRemoveCallback = OnRemoveInitializationObject;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_GeneralFoldout = EditorGUILayout.Foldout(m_GeneralFoldout, "General");
            if (m_GeneralFoldout)
            {
                ProjectConfigData.postProfilerEvents = EditorGUILayout.Toggle("Send Profiler Events", ProjectConfigData.postProfilerEvents);
                m_AasTarget.buildSettings.LogResourceManagerExceptions = EditorGUILayout.Toggle("Log Resource Manager Exceptions", m_AasTarget.buildSettings.LogResourceManagerExceptions);
            }
            GUILayout.Space(6);
            m_GroupFoldout = EditorGUILayout.Foldout(m_GroupFoldout, "Groups");
            if (m_GroupFoldout)
            {
                EditorGUILayout.HelpBox("Group data is modified on the group asset, and the groups list is altered from the Addressables window.  The list below is presented for ease of finding group assets, not for direct editing.", MessageType.None);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < m_AasTarget.groups.Count; i++)
                    {
                        var newObject = EditorGUILayout.ObjectField(m_AasTarget.groups[i], typeof(AddressableAssetGroup), false);
                        if (newObject != m_AasTarget.groups[i] && newObject is AddressableAssetGroup)
                        {
                            m_AasTarget.groups[i] = newObject as AddressableAssetGroup;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.Space(6);
            m_ProfilesFoldout = EditorGUILayout.Foldout(m_ProfilesFoldout, "Profiles");
            if (m_ProfilesFoldout)
            {
                if (m_AasTarget.profileSettings.profiles.Count > 0)
                {
                    if (m_CurrentProfileIndex < 0 || m_CurrentProfileIndex >= m_AasTarget.profileSettings.profiles.Count)
                        m_CurrentProfileIndex = 0;
                    var profileNames = m_AasTarget.profileSettings.GetAllProfileNames();
                    m_CurrentProfileIndex = EditorGUILayout.Popup("Profile To Edit", m_CurrentProfileIndex, profileNames.ToArray());

                    EditorGUI.indentLevel++;
                    bool doAdd;
                    bool doRemove;
                    bool cannotEdit = m_CurrentProfileIndex==0;// profileNames[m_CurrentProfileIndex] == AddressableAssetProfileSettings.k_RootProfileName;
                    using (new EditorGUI.DisabledScope(cannotEdit))
                    {
                        var newName = EditorGUILayout.DelayedTextField("EventName", profileNames[m_CurrentProfileIndex]);
                        if (newName != profileNames[m_CurrentProfileIndex])
                        {
                            var profile = m_AasTarget.profileSettings.profiles[m_CurrentProfileIndex];
                            profile.profileName = newName;
                            m_AasTarget.SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, profile.id, true);
                        }
                    }
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    doAdd = GUILayout.Button("+");
                    using (new EditorGUI.DisabledScope(cannotEdit))
                    {

                        doRemove = GUILayout.Button("-");

                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                    var rect = GUILayoutUtility.GetRect(0, m_ProfileEntriesRl.GetHeight());
                    rect.width -= 20;
                    rect.x += 20;
                    m_ProfileEntriesRl.DoList(rect);
                    
                    if (doAdd)
                    {
                        var uniqueProfileName = m_AasTarget.profileSettings.GetUniqueProfileName("New Profile");
                        if (!string.IsNullOrEmpty(uniqueProfileName))
                            m_AasTarget.profileSettings.AddProfile(uniqueProfileName, string.Empty);
                        m_CurrentProfileIndex = m_AasTarget.profileSettings.profiles.Count - 1;
                    }
                    else if (doRemove)
                    {
                        var prof = m_AasTarget.profileSettings.profiles[m_CurrentProfileIndex];
                        if (prof != null)
                        {
                            m_AasTarget.profileSettings.RemoveProfile(prof.id);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No valid profiles found");
                }

            }
            GUILayout.Space(6);
            m_LabelsFoldout = EditorGUILayout.Foldout(m_LabelsFoldout, "Labels");
            if (m_LabelsFoldout)
                m_LabelNamesRl.DoLayoutList();

            GUILayout.Space(6);
            m_DataBuildersFoldout = EditorGUILayout.Foldout(m_DataBuildersFoldout, "Data Builders");
            if (m_DataBuildersFoldout)
                m_DataBuildersRl.DoLayoutList();

            GUILayout.Space(6);
            m_SchemaTemplatesFoldout = EditorGUILayout.Foldout(m_SchemaTemplatesFoldout, "Group Schema Templates");
            if (m_SchemaTemplatesFoldout)
                m_SchemaTemplatesRl.DoLayoutList();

            GUILayout.Space(6);
            m_InitObjectsFoldout = EditorGUILayout.Foldout(m_InitObjectsFoldout, "Initialization Objects");
            if (m_InitObjectsFoldout)
                m_InitObjectsRl.DoLayoutList();


            serializedObject.ApplyModifiedProperties();
        }

        void DrawProfileEntriesHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Profile Entries");
        }

        void DrawProfileEntriesCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            float halfW = rect.width * 0.4f;
            var currentEntry = m_AasTarget.profileSettings.profileEntryNames[index];
            var newName = EditorGUI.DelayedTextField(new Rect(rect.x, rect.y, halfW, rect.height), currentEntry.ProfileName);
            if (newName != currentEntry.ProfileName)
                currentEntry.SetName(newName, m_AasTarget.profileSettings);

            var currProfile = m_AasTarget.profileSettings.profiles[m_CurrentProfileIndex];
            var oldValue = m_AasTarget.profileSettings.GetValueById(currProfile.id, currentEntry.Id);
            var newValue = EditorGUI.TextField(new Rect(rect.x + halfW, rect.y, rect.width - halfW, rect.height), oldValue);
            if (oldValue != newValue)
            {
                m_AasTarget.profileSettings.SetValue(currProfile.id, currentEntry.ProfileName, newValue);
            }
        }

        void OnAddProfileEntry(ReorderableList list)
        {
            var uniqueProfileEntryName = m_AasTarget.profileSettings.GetUniqueProfileEntryName("New Entry");
            if (!string.IsNullOrEmpty(uniqueProfileEntryName))
                m_AasTarget.profileSettings.CreateValue(uniqueProfileEntryName, "");
        }

        void OnRemoveProfileEntry(ReorderableList list)
        {
            if (list.index >= 0 && list.index < m_AasTarget.profileSettings.profileEntryNames.Count)
            {
                var entry = m_AasTarget.profileSettings.profileEntryNames[list.index];
                if (entry != null)
                    m_AasTarget.profileSettings.RemoveValue(entry.Id);
            }
        }

        void DrawLabelNamesHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Labels");
        }

        void DrawLabelNamesCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var oldName = m_AasTarget.labelTable.labelNames[index];
            EditorGUI.LabelField(rect, oldName);

        }

        void OnRemoveLabel(ReorderableList list)
        {
            m_AasTarget.RemoveLabel(m_AasTarget.labelTable.labelNames[list.index]);
        }

        void OnAddLabel(Rect buttonRect, ReorderableList list)
        {
            buttonRect.x -= 400;
            buttonRect.y -= 13;

            PopupWindow.Show(buttonRect, new LabelNamePopup(m_LabelNamesRl.elementHeight, m_AasTarget));
        }

        class LabelNamePopup : PopupWindowContent
        {
            internal float rowHeight;
            internal string name;
            internal bool needsFocus = true;
            internal AddressableAssetSettings settings;

            public LabelNamePopup(float rowHeight, AddressableAssetSettings settings)
            {
                this.rowHeight = rowHeight;
                this.settings = settings;
                name = this.settings.labelTable.GetUniqueLabelName("New Label");
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(400, rowHeight * 3);
            }

            public override void OnGUI(Rect windowRect)
            {
                GUILayout.Space(5);
                Event evt = Event.current;
                bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
                GUI.SetNextControlName("LabelName");
                name = EditorGUILayout.TextField("New Tag EventName", name);
                if (needsFocus)
                {
                    needsFocus = false;
                    EditorGUI.FocusTextInControl("LabelName");
                }

                GUI.enabled = name.Length != 0;
                if (GUILayout.Button("Save") || hitEnter)
                {
                    if (string.IsNullOrEmpty(name))
                        Debug.LogError("Cannot add empty label to Addressables label list");
                    else if (name != settings.labelTable.GetUniqueLabelName(name))
                        Debug.LogError("Label name '" + name + "' is already in the labels list.");
                    else
                        settings.AddLabel(name);

                    editorWindow.Close();
                }
            }
        }

        void DrawDataBuilderHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Data Builders");
        }

        void DrawDataBuilderCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var so = m_AasTarget.DataBuilders[index];
            var builder = so as IDataBuilder;
            var label = builder == null ? "" : builder.Name;
            var nb = EditorGUI.ObjectField(rect, label, so, typeof(ScriptableObject), false) as ScriptableObject;
            if (nb != so)
                m_AasTarget.SetDataBuilderAtIndex(index, nb as IDataBuilder);
        }

        void OnRemoveDataBuilder(ReorderableList list)
        {
            m_AasTarget.RemoveDataBuilder(list.index);
        }

        void OnAddDataBuilder(Rect buttonRect, ReorderableList list)
        {
            var assetPath = EditorUtility.OpenFilePanelWithFilters("Data Builder", "Assets", new[] {"Data Builder", "asset" });
            if (string.IsNullOrEmpty(assetPath))
                return;
            var builder = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath.Substring(assetPath.IndexOf("Assets/")));
            if (!typeof(IDataBuilder).IsAssignableFrom(builder.GetType()))
            {
                Debug.LogWarningFormat("Asset at {0} does not implement the IDataBuilder interface.", assetPath);
                return;
            }
            m_AasTarget.AddDataBuilder(builder as IDataBuilder);
        }

        void DrawSchemaTemplateHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Group Schema Templates");
        }

        void DrawSchemaTemplateCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var template = m_AasTarget.SchemaTemplates[index];
            GUI.Label(rect, template.DisplayName);
        }

        void OnRemoveSchemaTemplate(ReorderableList list)
        {
            m_AasTarget.RemoveSchemaTemplate(list.index);
        }

        void OnAddSchemaTemplate(Rect buttonRect, ReorderableList list)
        {
            buttonRect.x -= 400;
            buttonRect.y -= 13;

            PopupWindow.Show(buttonRect, new NewScehemaTemplatePopup(m_LabelNamesRl.elementHeight, m_AasTarget));
        }

        class NewScehemaTemplatePopup : PopupWindowContent
        {
            internal float rowHeight;
            internal string name = "";
            internal string description = "";
            internal bool needsFocus = true;
            internal AddressableAssetSettings settings;
            List<Type> m_SchemaTypes;
            List<Type> m_SelectedTypes = new List<Type>();
            public NewScehemaTemplatePopup(float rowHeight, AddressableAssetSettings settings)
            {
                this.rowHeight = rowHeight;
                this.settings = settings;
                m_SchemaTypes = AddressableAssetUtility.GetTypes<AddressableAssetGroupSchema>();
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(400, rowHeight * (4 + m_SelectedTypes.Count));
            }

            public override void OnGUI(Rect windowRect)
            {
                GUILayout.Space(5);
                Event evt = Event.current;
                bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
                GUI.SetNextControlName("LabelName");
                name = EditorGUILayout.TextField("Schema Template EventName", name);
                description = EditorGUILayout.TextField("Description", description);
                if (needsFocus)
                {
                    needsFocus = false;
                    EditorGUI.FocusTextInControl("LabelName");
                }
                for (int i = 0; i < m_SelectedTypes.Count; i++)
                {
                    var schema = m_SelectedTypes[i];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(schema.Name);
                    if (GUILayout.Button("X", GUILayout.Width(40)))
                    {
                        m_SelectedTypes.Remove(schema);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (EditorGUILayout.DropdownButton(new GUIContent("Add Schema", "Add new schema to this group."), FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    for (int i = 0; i < m_SchemaTypes.Count; i++)
                    {
                        var type = m_SchemaTypes[i];
                        menu.AddItem(new GUIContent(type.Name, ""), false, OnAddSchema, type);
                    }
                    menu.ShowAsContext();
                }

                GUI.enabled = name.Length != 0;
                if (GUILayout.Button("Save") || hitEnter)
                {
                    if (string.IsNullOrEmpty(name))
                        Debug.LogError("Schema template must have a valid name.");
                    else
                        settings.AddSchemaTemplate(name, description, m_SelectedTypes.ToArray());

                    editorWindow.Close();
                }
            }
            void OnAddSchema(object context)
            {
                m_SelectedTypes.Add(context as Type);
            }
        }

        void DrawInitializationObjectHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Initialization Objects");
        }

        void DrawInitializationObjectCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var so = m_AasTarget.InitializationObjects[index];
            var initObj = so as IObjectInitializationDataProvider;
            var label = initObj == null ? "" : initObj.Name;
            var nb = EditorGUI.ObjectField(rect, label, so, typeof(ScriptableObject), false) as ScriptableObject;
            if (nb != so)
                m_AasTarget.SetInitializationObjectAtIndex(index, nb as IObjectInitializationDataProvider);
        }

        void OnRemoveInitializationObject(ReorderableList list)
        {
            m_AasTarget.RemoveInitializationObject(list.index);
        }

        void OnAddInitializationObject(Rect buttonRect, ReorderableList list)
        {
            var assetPath = EditorUtility.OpenFilePanelWithFilters("Initialization Object", "Assets", new[] { "Initialization Object", "asset" });
            if (string.IsNullOrEmpty(assetPath))
                return;
            var initObj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath.Substring(assetPath.IndexOf("Assets/")));
            if (!typeof(IObjectInitializationDataProvider).IsAssignableFrom(initObj.GetType()))
            {
                Debug.LogWarningFormat("Asset at {0} does not implement the IObjectInitializationDataProvider interface.", assetPath);
                return;
            }
            m_AasTarget.AddInitializationObject(initObj as IObjectInitializationDataProvider);
        }

    }

}
