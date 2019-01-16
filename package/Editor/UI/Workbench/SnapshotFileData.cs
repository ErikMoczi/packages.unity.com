using System;
using System.IO;
using Unity.MemoryProfiler.Editor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine.Profiling.Memory.Experimental;

namespace Unity.MemoryProfiler.Editor
{
    internal class SnapshotFileGUIData
    {
        internal GUIContent name;
        internal GUIContent metaInfo;
        internal GUIContent date;
        internal GUIContent platform;
        internal RuntimePlatform runtimePlatform = (RuntimePlatform)(-1);
        internal Texture2D texture;
        internal DynamicVisualElements dynamicVisualElements;

        internal struct DynamicVisualElements
        {
            internal VisualElement snapshotListItem;
            internal VisualElement optionDropdownButton;
            internal Button openButton;
            internal Button closeButton;
            internal Label snapshotNameLabel;
            internal Label snapshotDateLabel;
            internal TextField snapshotRenameField;
        }

        public enum State
        {
            Closed,
            Open,
            InView,
        }

        const string k_OpenClassName = "snapshotIsOpen";
        const string k_InViewClassName = "snapshotIsInView";

        public State CurrentState
        {
            get
            {
                return m_CurrentState;
            }
            set
            {
                if (value != m_CurrentState)
                {
                    switch (value)
                    {
                        case State.Closed:
                            if (m_CurrentState == State.InView)
                            {
                                dynamicVisualElements.snapshotNameLabel.RemoveFromClassList(k_InViewClassName);
                                dynamicVisualElements.snapshotDateLabel.RemoveFromClassList(k_InViewClassName);
                            }
                            else if (m_CurrentState == State.Open)
                            {
                                dynamicVisualElements.snapshotNameLabel.RemoveFromClassList(k_OpenClassName);
                                dynamicVisualElements.snapshotDateLabel.RemoveFromClassList(k_OpenClassName);
                            }
                            break;
                        case State.Open:
                            if (m_CurrentState == State.InView)
                            {
                                dynamicVisualElements.snapshotNameLabel.RemoveFromClassList(k_InViewClassName);
                                dynamicVisualElements.snapshotDateLabel.RemoveFromClassList(k_InViewClassName);
                            }
                            dynamicVisualElements.snapshotNameLabel.AddToClassList(k_OpenClassName);
                            dynamicVisualElements.snapshotDateLabel.AddToClassList(k_OpenClassName);
                            break;
                        case State.InView:
                            if (m_CurrentState == State.Open)
                            {
                                dynamicVisualElements.snapshotNameLabel.RemoveFromClassList(k_OpenClassName);
                                dynamicVisualElements.snapshotDateLabel.RemoveFromClassList(k_OpenClassName);
                            }
                            dynamicVisualElements.snapshotNameLabel.AddToClassList(k_InViewClassName);
                            dynamicVisualElements.snapshotDateLabel.AddToClassList(k_InViewClassName);
                            break;
                        default:
                            break;
                    }
                    m_CurrentState = value;
                }
            }
        }
        State m_CurrentState = State.Closed;

        public bool RenamingFieldVisible
        {
            get
            {
                return m_RenamingFieldVisible;
            }
            set
            {
                if (value != m_RenamingFieldVisible)
                {
                    dynamicVisualElements.snapshotRenameField.visible = value;
                    dynamicVisualElements.snapshotNameLabel.visible = !value;
                    // no opening or option meddling while renaming!
                    dynamicVisualElements.openButton.SetEnabled(!value);
                    dynamicVisualElements.optionDropdownButton.SetEnabled(!value);
                    m_RenamingFieldVisible = value;
                    dynamicVisualElements.snapshotRenameField.SetValueWithoutNotify(dynamicVisualElements.snapshotNameLabel.text);
                    if (value)
                    {
                        dynamicVisualElements.snapshotRenameField.Focus();
                    }
                }
            }
        }
        bool m_RenamingFieldVisible;
        private DateTime recordDate;

        public SnapshotFileGUIData(DateTime recordDate)
        {
            UtcDateTime = recordDate;
        }

        public readonly DateTime UtcDateTime;
        public GUIContent Name { get { return name; } }
        public GUIContent MetaContent { get { return metaInfo; } }
        public GUIContent MetaPlatform { get { return platform; } }
        public GUIContent SnapshotDate { get { return date; } }
        public Texture MetaScreenshot { get { return texture; } }
    }

    internal class SnapshotFileData
    {
        public FileInfo FileInfo;
        SnapshotFileGUIData m_GuiData;

        public SnapshotFileGUIData GuiData { get { return m_GuiData; } }

        public SnapshotFileData(FileInfo info)
        {
            FileInfo = info;
            using (var snapshot = LoadSnapshot())
            {
                MetaData snapshotMetadata = snapshot.metadata;

                m_GuiData = new SnapshotFileGUIData(snapshot.recordDate);

                m_GuiData.name = new GUIContent(Path.GetFileNameWithoutExtension(FileInfo.Name));
                m_GuiData.metaInfo = new GUIContent(snapshotMetadata.content);
                m_GuiData.platform = new GUIContent(snapshotMetadata.platform);

                RuntimePlatform runtimePlatform;
                if (TryGetRuntimePlatform(snapshotMetadata.platform, out runtimePlatform))
                    m_GuiData.runtimePlatform = runtimePlatform;

                m_GuiData.date = new GUIContent(m_GuiData.UtcDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
                m_GuiData.texture = snapshotMetadata.screenshot;
            }
        }

        bool TryGetRuntimePlatform(string platformName, out RuntimePlatform runtimePlatform)
        {
            bool success = (!string.IsNullOrEmpty(platformName)) && Enum.IsDefined(typeof(RuntimePlatform), platformName);
            if (success)
                runtimePlatform = (RuntimePlatform)Enum.Parse(typeof(RuntimePlatform), platformName);
            else
                runtimePlatform = default(RuntimePlatform);
            return success;
        }

        public PackedMemorySnapshot LoadSnapshot()
        {
            UnityEngine.Assertions.Assert.IsTrue(FileInfo.Exists);
            return PackedMemorySnapshot.Load(FileInfo.FullName);
        }
    }
}
