#undef WORKSPACES_AS_SERIALIZED_OBJECTS
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using UnityEditor.Profiling.Memory.Experimental;

namespace Unity.Profiling.Memory.UI
{
#if SNAPSHOT_COLLECTIONSS_AS_SERIALIZED_OBJECTS
    public class SnapshotCollection : ScriptableObject
#else
    [System.Serializable]
    public class SnapshotCollection : IEnumerable<SnapshotCollection.SnapshotUIData>
#endif
    {
        [System.Serializable]
        public class SnapshotUIData
        {
            public enum State
            {
                Listed,
                Open,
                OpenInDiffAsFirst,
                OpenInDiffAsSecond,
            }
            [NonSerialized]
            public State CurrentState;
            [NonSerialized]
            PackedMemorySnapshot m_Snapshot;
            public string Path;
            public GUIContent Name;
            public GUIContent MetaInfo;
            public GUIContent FileSize;
            public Texture2D PreviewImage;

            public SnapshotUIData(PackedMemorySnapshot snapshot)
            {
                m_Snapshot = snapshot;
            }

            public struct Enumerator : IEnumerator<SnapshotUIData>
            {
                private SnapshotCollection m_SnapshotCollection;
                private int m_CurrentIndex;

                public Enumerator(SnapshotCollection snapshotCollection)
                {
                    this.m_SnapshotCollection = snapshotCollection;
                    m_CurrentIndex = -1;
                }

                public SnapshotUIData Current
                {
                    get
                    {
                        return m_SnapshotCollection.m_SnapshotUIData[m_CurrentIndex];
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Dispose()
                {
                    m_SnapshotCollection = null;
                }

                public bool MoveNext()
                {
                    return m_SnapshotCollection.m_SnapshotUIData != null && ++m_CurrentIndex < m_SnapshotCollection.m_SnapshotUIData.Count;
                }

                public void Reset()
                {
                    m_CurrentIndex = -1;
                }

                public void UnloadCurrent()
                {
                    m_SnapshotCollection.m_SnapshotUIData.RemoveAt(m_CurrentIndex--);
                }

                public SnapshotUIData OpenCurrent(out PackedMemorySnapshot snapshot)
                {
                    snapshot = m_SnapshotCollection.m_SnapshotUIData[m_CurrentIndex].m_Snapshot;
                    if (snapshot == null)
                    {
                        int index = m_CurrentIndex;
                        snapshot = m_SnapshotCollection.LoadCapture(m_SnapshotCollection.m_SnapshotUIData[m_CurrentIndex].Path, ref index);
                        m_SnapshotCollection.m_SnapshotUIData[m_CurrentIndex].m_Snapshot = snapshot;
                    }
                    return m_SnapshotCollection.m_SnapshotUIData[m_CurrentIndex];
                }

                internal bool EqualsCurrentSnapshot(PackedMemorySnapshot snapshot)
                {
                    if (Current.m_Snapshot == null)
                        return false;
                    return Current.m_Snapshot == snapshot;
                }
            }
        }

        static class Content
        {
            public const string LoadSnapshotFilePanelText = "Load Snapshot";
            public const string SaveSnapshotFilePanelText = "Save Snapshot";
            public static readonly GUIContent Unsaved = new GUIContent("<Unsaved>");
            public static readonly GUIContent InvalidPath = new GUIContent("<Invalid Path>");
        }
        const string k_SnapshotFileExtension = "snap";

        static Texture2D s_PreviewTextureFallback;

        static Texture2D PreviewTextureFallback
        {
            get
            {
                if (s_PreviewTextureFallback == null)
                    s_PreviewTextureFallback = new Texture2D(100, 100);
                return s_PreviewTextureFallback;
            }
        }

        public int SnapshotCount
        {
            get
            {
                return m_SnapshotUIData.Count;
            }
        }

        [SerializeField]
        List<SnapshotUIData> m_SnapshotUIData = new List<SnapshotUIData>();

        public SnapshotUIData LoadCapture(out PackedMemorySnapshot snapshot)
        {
            string filePath = EditorUtility.OpenFilePanel(Content.LoadSnapshotFilePanelText, "", k_SnapshotFileExtension);
            int index = -1;
            snapshot = LoadCapture(filePath, ref index);
            return m_SnapshotUIData[index];
        }

        public SnapshotUIData AddCapture(PackedMemorySnapshot snapshot, string filePath)
        {
            Texture2D preview = null;
            string snapshotFileName = Path.GetFileNameWithoutExtension(filePath);

            var metaData = snapshot.metadata;
            if (metaData.screenshot != null)
            {
                preview = metaData.screenshot;
            }
            else
                preview = PreviewTextureFallback;


            GUIContent fileSize;
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo == null)
                {
                    fileSize = Content.InvalidPath;
                }
                else
                {
                    fileSize = new GUIContent(new FileInfo(filePath).Length / (1024 * 1024) + "MB");
                }
            }
            else
            {
                fileSize = Content.Unsaved;
            }
            m_SnapshotUIData.Add(
                new SnapshotUIData(snapshot)
            {
                Path = filePath,
                Name = new GUIContent(snapshotFileName),
                MetaInfo = new GUIContent("Standalone Mono 4.6", snapshot.recordDate.ToShortDateString()),
                FileSize = fileSize,
                PreviewImage = preview,
            }
                );
            return m_SnapshotUIData[m_SnapshotUIData.Count - 1];
        }

        private PackedMemorySnapshot LoadCapture(string filePath, ref int indexInList)
        {
            if (filePath.Length != 0)
            {
                var snapshot = PackedMemorySnapshot.Load(filePath);
                if (snapshot != null && indexInList < 0)
                {
                    AddCapture(snapshot, filePath);
                    indexInList = m_SnapshotUIData.Count - 1;
                }
                return snapshot;
            }
            return null;
        }

        public IEnumerator<SnapshotUIData> GetEnumerator()
        {
            return new SnapshotUIData.Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
