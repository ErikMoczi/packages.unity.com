using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.XR.Management;

namespace UnityEditor.XR.Management
{
    class LoaderInfo : IEquatable<LoaderInfo>
    {
        public Type loaderType;
        public string assetName;
        public XRLoader instance;
        public bool isUsed;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LoaderInfo && Equals((LoaderInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (loaderType != null ? loaderType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (instance != null ? instance.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ isUsed.GetHashCode();
                return hashCode;
            }
        }

        public bool Equals(LoaderInfo other)
        {
            return other != null && Equals(loaderType, other.loaderType) && Equals(instance, other.instance) && isUsed == other.isUsed;
        }
    }

    class LoaderOrderUI
    {
        const string k_ErrorGettingProviderSubsystem = "Error attempting to get object data for xr manager loaders.";
        const string k_AtLeastOneLoaderInstance = "Must add at least one XRLoader instance.";

        ReorderableList m_OrderedList = null;
        List<LoaderInfo> m_LoadersInUse = new List<LoaderInfo>();
        List<LoaderInfo> m_LoadersNotInUse = new List<LoaderInfo>();
        SerializedProperty m_LoaderProperty;
        bool m_ShouldReload = false;

        public LoaderOrderUI(List<LoaderInfo> loaderInfos, SerializedProperty loaderProperty)
        {
            m_LoaderProperty = loaderProperty;

            foreach (var info in loaderInfos)
            {
                if (info.isUsed)
                {
                    m_LoadersInUse.Add(info);
                }
                else
                {
                    m_LoadersNotInUse.Add(info);
                }
            }
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            LoaderInfo info = m_LoadersInUse[index];
            var label = (info.instance == null) ? EditorGUIUtility.TrTextContent("Missing (XRLoader)") : EditorGUIUtility.TrTextContent(info.assetName);
            EditorGUI.LabelField(rect, label);
        }

        float GetElementHeight(int index)
        {
            return m_OrderedList.elementHeight;
        }

        void UpdateSerializedProperty()
        {
            if (m_LoaderProperty != null && m_LoaderProperty.isArray)
            {
                m_LoaderProperty.ClearArray();

                int index = 0;
                foreach (LoaderInfo info in m_LoadersInUse)
                {
                    m_LoaderProperty.InsertArrayElementAtIndex(index);
                    var prop = m_LoaderProperty.GetArrayElementAtIndex(index);
                    prop.objectReferenceValue = info.instance;
                    index++;
                }

                m_LoaderProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        void ReorderLoaderList(ReorderableList list)
        {
            UpdateSerializedProperty();
        }

        string TypeNameToString(Type type)
        {
            string[] words = Regex.Matches(type.Name, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)")
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();
            return string.Join(" ", words);
        }

        void DrawAddDropdown(Rect rect, ReorderableList list)
        {
            List<string> names = new List<string>();
            List<bool> enabled = new List<bool>();

            GenericMenu menu = new GenericMenu();

            int index = 0;
            foreach (var info in m_LoadersNotInUse)
            {
                string name = info.assetName;
                if (String.IsNullOrEmpty(name) && info.loaderType != null)
                {
                    name = TypeNameToString(info.loaderType);
                }

                if (info.instance == null)
                    name = name + " (create new)";

                menu.AddItem(new GUIContent(name), false, AddLoaderMenuSelected, index);
                index++;
            }

            menu.ShowAsContext();
        }

        void AddLoaderMenuSelected(object data)
        {
            int selected = (int)data;
            LoaderInfo info = m_LoadersNotInUse[selected];

            if (info.instance == null)
            {
                string newAssetName = String.Format("New {0}.asset", TypeNameToString(info.loaderType));
                XRLoader loader = ScriptableObject.CreateInstance(info.loaderType) as XRLoader;
                string assetPath = EditorUtility.SaveFilePanelInProject("Create new XR Loader asset",
                    newAssetName,
                    "asset",
                    "Please enter a name and location where the loader asset can be created at.");

                if (string.IsNullOrEmpty(assetPath))
                {
                    return;
                }

                info.instance = loader;
                info.assetName = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(loader, assetPath);
                m_ShouldReload = true;
            }

            m_LoadersNotInUse.Remove(info);
            info.isUsed = true;
            m_LoadersInUse.Add(info);
            UpdateSerializedProperty();
        }

        void RemoveInstanceFromList(ReorderableList list)
        {
            LoaderInfo info = m_LoadersInUse[list.index];
            m_LoadersInUse.Remove(info);

            if (info.loaderType != null)
            {
                info.isUsed = false;
                m_LoadersNotInUse.Add(info);
            }
            UpdateSerializedProperty();
        }

        public bool OnGUI()
        {
            if (m_LoaderProperty == null)
                return false;

            m_ShouldReload = false;
            if (m_OrderedList == null)
            {
                m_OrderedList = new ReorderableList(m_LoadersInUse, typeof(XRLoader), true, true, true, true);
                m_OrderedList.drawHeaderCallback = (rect) => GUI.Label(rect, EditorGUIUtility.TrTextContent("Loaders"), EditorStyles.label);
                m_OrderedList.drawElementCallback = (rect, index, isActive, isFocused) => DrawElementCallback(rect, index, isActive, isFocused);
                m_OrderedList.elementHeightCallback = (index) => GetElementHeight(index);
                m_OrderedList.onReorderCallback = (list) => ReorderLoaderList(list);
                m_OrderedList.onAddDropdownCallback = (rect, list) => DrawAddDropdown(rect, list);
                m_OrderedList.onRemoveCallback = (list) => RemoveInstanceFromList(list);
            }

            m_OrderedList.DoLayoutList();

            if (!m_LoadersInUse.Any() && !m_LoadersNotInUse.Any())
            {
                EditorGUILayout.HelpBox(k_AtLeastOneLoaderInstance, MessageType.Warning);
            }

            return m_ShouldReload;
        }
    }


    [CustomEditor(typeof(XRManager))]
    public class XRManagerEditor : Editor
    {
        // Simple class to give us updates when the asset database changes.
        class AssetCallbacks : AssetPostprocessor
        {
            static bool s_EditorUpdatable = false;
            public static System.Action Callback { get; set; }

            static AssetCallbacks()
            {
                if (!s_EditorUpdatable)
                {
                    EditorApplication.update += EditorUpdatable;
                }
                EditorApplication.projectChanged += EditorApplicationOnProjectChanged;
            }

            static void EditorApplicationOnProjectChanged()
            {
                if (Callback != null)
                    Callback.Invoke();
            }

            static void EditorUpdatable()
            {
                s_EditorUpdatable = true;
                EditorApplication.update -= EditorUpdatable;
                if (Callback != null)
                    Callback.Invoke();
            }
        }

        SerializedProperty m_AutomaticLoading = null;
        SerializedProperty m_AutomaticRunning = null;
        SerializedProperty m_LoaderList = null;

        static GUIContent k_AutoLoadLabel = EditorGUIUtility.TrTextContent("Automatic Loading");
        static GUIContent k_AutoRunLabel = EditorGUIUtility.TrTextContent("Automatic Running");

        List<LoaderInfo> m_AllLoaderInfos = new List<LoaderInfo>();

        LoaderOrderUI m_LoadOrderUI = null;
        bool m_MustReloadData = true;

        void AssetProcessorCallback()
        {
            m_MustReloadData = true;
        }

        void OnEnable()
        {
            AssetCallbacks.Callback = AssetProcessorCallback;
            ReloadData();
        }

        public void OnDisable()
        {
            AssetCallbacks.Callback = null;
        }

        void ReloadData()
        {
            if (m_LoaderList == null || m_LoaderList.serializedObject == null)
                return;

            m_LoadOrderUI = null;

            m_AllLoaderInfos.Clear();

            PopulateLoaderInfosFromCurrentAssignedLoaders();

            PopulateLoaderInfosFromUnassignedLoaders();

            m_MustReloadData = false;
        }

        void PopulateLoaderInfosFromUnassignedLoaders()
        {
            List<LoaderInfo> newInfos = new List<LoaderInfo>();

            GetAllKnownLoaderInfos(newInfos);
            MergeLoaderInfos(newInfos);
        }

        void MergeLoaderInfos(List<LoaderInfo> newInfos)
        {
            foreach (var info in newInfos)
            {
                bool addNew = true;
                if (info.instance != null)
                {
                    foreach (var li in m_AllLoaderInfos)
                    {
                        if (li.instance == info.instance)
                        {
                            if (!String.IsNullOrEmpty(info.assetName))
                                li.assetName = info.assetName;
                            addNew = false;
                            break;
                        }
                    }
                }

                if (addNew)
                {
                    m_AllLoaderInfos.Add(info);
                }
            }
        }

        static void GetAllKnownLoaderInfos(List<LoaderInfo> newInfos)
        {
            var loaderTypes = TypeLoaderExtensions.GetAllTypesWithInterface<XRLoader>();
            foreach (Type loaderType in loaderTypes)
            {
                // HACK: No need for people to see these loaders
                if (String.Compare("DummyLoader", loaderType.Name, StringComparison.OrdinalIgnoreCase) == 0 ||
                    String.Compare("SampleLoader", loaderType.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                var assets = AssetDatabase.FindAssets(String.Format("t:{0}", loaderType));
                if (!assets.Any())
                {
                    LoaderInfo info = new LoaderInfo();
                    info.loaderType = loaderType;
                    newInfos.Add(info);
                }
                else
                {
                    foreach (var asset in assets)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(asset);

                        LoaderInfo info = new LoaderInfo();
                        info.loaderType = loaderType;
                        info.instance = AssetDatabase.LoadAssetAtPath(path, loaderType) as XRLoader;
                        info.assetName = Path.GetFileNameWithoutExtension(path);
                        newInfos.Add(info);
                    }
                }
            }
        }

        string AssetNameFromInstance(UnityEngine.Object asset)
        {
            if (asset == null)
                return "";

            string assetPath = AssetDatabase.GetAssetPath(asset);
            return Path.GetFileNameWithoutExtension(assetPath);
        }

        void PopulateLoaderInfosFromCurrentAssignedLoaders()
        {
            for (int i = 0; i < m_LoaderList.arraySize; i++)
            {
                var prop = m_LoaderList.GetArrayElementAtIndex(i);

                LoaderInfo info = new LoaderInfo();
                info.loaderType = (prop.objectReferenceValue == null) ? null : prop.objectReferenceValue.GetType();
                info.assetName = AssetNameFromInstance(prop.objectReferenceValue);
                info.instance = prop.objectReferenceValue as XRLoader;
                info.isUsed = true;
                m_AllLoaderInfos.Add(info);
            }
        }

        void PopulateProperty(string propertyPath, ref SerializedProperty prop)
        {
            if (prop == null) prop = serializedObject.FindProperty(propertyPath);
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;

            PopulateProperty("m_AutomaticLoading", ref m_AutomaticLoading);
            PopulateProperty("m_AutomaticRunning", ref m_AutomaticRunning);
            PopulateProperty("m_Loaders", ref m_LoaderList);

            serializedObject.Update();

            if (m_MustReloadData)
                ReloadData();

            EditorGUILayout.PropertyField(m_AutomaticLoading, k_AutoLoadLabel);
            if (m_AutomaticLoading.boolValue)
                EditorGUILayout.PropertyField(m_AutomaticRunning, k_AutoRunLabel);

            if (m_LoadOrderUI == null) m_LoadOrderUI = new LoaderOrderUI(m_AllLoaderInfos, m_LoaderList);

            m_MustReloadData = m_LoadOrderUI.OnGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
