#undef SNAPSHOT_COLLECTIONS_AS_SERIALIZED_OBJECTS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace Unity.Profiling.Memory.UI
{
    public class MemoryProfilerWorkbench : ScriptableObject, IEnumerable<MemoryProfilerWorkbench.CollectionUIData>
    {
        [System.Serializable]
        public class CollectionUIData
        {
            public SnapshotCollection Collection;
            public GUIContent Name;
            public bool IsTemporary;
        }


        public int IndexOfCurrentCollectionInRenameMode
        {
            get
            {
                return m_IndexOfCollectionInRenamMode;
            }
        }

        public int CurrentCollectionIndex
        {
            get
            {
                Sanitize();
                return m_LastOpenedCollection = Mathf.Clamp(m_LastOpenedCollection, 0, m_Collections.Count);
            }
        }

        public CollectionUIData CurrentCollection
        {
            get
            {
                CollectionUIData Collection = m_Collections[CurrentCollectionIndex];
                if (Collection.Collection == null)
                {
                    return default(CollectionUIData);
                }
                return Collection;
            }
        }

        [SerializeField]
        List<CollectionUIData> m_Collections = new List<CollectionUIData>();

        [SerializeField]
        int m_LastOpenedCollection;

        [NonSerialized]
        int m_IndexOfCollectionInRenamMode = -1;

        const string k_SaveCollectionFilePanelText = "Save Collection";
        const string k_LoadCollectionFilePanelText = "Load Collection";
        const string k_DefaultNewCollectionName = "New Collection";
        const string k_CollectionTooltip = "Click to select, click again to rename.";
        const string k_DefaultCollectionName = "New Collection";
        const string k_CollectionFileExtention = k_CollectionSpecificFileExtention + ".asset";
        const string k_CollectionSpecificFileExtention = "collection";

        static GUIContent GetGUIContentForCollection(string collectionName = null)
        {
            return new GUIContent(string.IsNullOrEmpty(collectionName) ? k_DefaultNewCollectionName : collectionName, k_CollectionTooltip);
        }

        void OnValidate()
        {
            Sanitize();
        }

        void Sanitize()
        {
            if (m_Collections.Count == 0)
            {
                CreateTemporaryCollection();
            }

            for (int i = m_Collections.Count - 1; i >= 0; i--)
            {
                if (m_Collections[i].Collection == null)
                    m_Collections.RemoveAt(i);
            }

            m_LastOpenedCollection = Mathf.Clamp(m_LastOpenedCollection, 0, m_Collections.Count);
        }

        public void CreateTemporaryCollection()
        {
            m_Collections.Add(new CollectionUIData()
            {
#if SNAPSHOT_COLLECTIONS_AS_SERIALIZED_OBJECTS
                Collection = ScriptableObject.CreateInstance<Collection>(),
                Name = GetGUIContentForCollection(),
                IsTemporary = true,
#else
                Collection = new SnapshotCollection(),
                Name = GetGUIContentForCollection(),
                IsTemporary = false,
#endif
            });
        }

        public static void SaveCollection(CollectionUIData CollectionUIData)
        {
#if SNAPSHOT_COLLECTIONS_AS_SERIALIZED_OBJECTS
            Debug.Assert(CollectionUIData.Collection != null, "The Collection that you want to save does not exits!");

            string filePath = EditorUtility.SaveFilePanel(k_SaveCollectionFilePanelText, Application.dataPath, "Collection", k_CollectionFileExtention);
            if (filePath.Length != 0)
            {
                Debug.Assert(filePath.Contains(Application.dataPath), "Collections need to be stored within the project folder");
                filePath = "Assets" + filePath.Substring(Application.dataPath.Length);
                AssetDatabase.CreateAsset(CollectionUIData.Collection, filePath);
                CollectionUIData.Name = new GUIContent(Path.GetFileNameWithoutExtension(filePath), filePath);
                CollectionUIData.IsTemporary = false;
            }
            EditorGUIUtility.ExitGUI();
#endif
        }

        internal void LoadCollection()
        {
#if SNAPSHOT_COLLECTIONS_AS_SERIALIZED_OBJECTS
            string filePath = EditorUtility.OpenFilePanel(k_LoadCollectionFilePanelText, Application.dataPath, k_CollectionFileExtention);
            if (filePath.Length != 0)
            {
                Debug.Assert(filePath.Contains(Application.dataPath), "Collections need to be stored within the project folder");
                filePath = "Assets" + filePath.Substring(Application.dataPath.Length);
                SnapshotCollection Collection = AssetDatabase.LoadAssetAtPath<SnapshotCollection>(filePath);


                if (Collection != null)
                {
                    m_Collections.Add(new CollectionUIData()
                    {
                        Collection = Collection,
                        Name = new GUIContent(Collection.name, filePath),
                        IsTemporary = true
                    });
                }
            }
            EditorGUIUtility.ExitGUI();
#endif
        }

        public IEnumerator<CollectionUIData> GetEnumerator()
        {
            Sanitize();
            return new CollectionUIDataIterator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct CollectionUIDataIterator : IEnumerator<CollectionUIData>
        {
            public CollectionUIData Current
            {
                get
                {
                    return m_CollectionHolderToIterate.m_Collections[m_CurrentIndex];
                }
            }

            public bool IsCurrentlyOpenedCollection
            {
                get
                {
                    return m_CurrentIndex == m_CollectionHolderToIterate.CurrentCollectionIndex;
                }
            }
            public bool IsCollectionInRenameMode
            {
                get
                {
                    return m_CurrentIndex == m_CollectionHolderToIterate.IndexOfCurrentCollectionInRenameMode;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
            MemoryProfilerWorkbench m_CollectionHolderToIterate;
            int m_CurrentIndex;

            internal CollectionUIDataIterator(MemoryProfilerWorkbench CollectionHolderToIterate)
            {
                m_CollectionHolderToIterate = CollectionHolderToIterate;
                m_CurrentIndex = -1;
            }

            public void Dispose()
            {
                m_CollectionHolderToIterate = null;
            }

            public bool MoveNext()
            {
                return m_CollectionHolderToIterate.m_Collections != null && ++m_CurrentIndex < m_CollectionHolderToIterate.m_Collections.Count;
            }

            public void Reset()
            {
                m_CurrentIndex = -1;
            }

            internal void UnloadCurrentCollection()
            {
                m_CollectionHolderToIterate.m_Collections.RemoveAt(m_CurrentIndex--);
            }

            internal void OpenCurrentCollection()
            {
                m_CollectionHolderToIterate.m_LastOpenedCollection = m_CurrentIndex;
                ExitRenameMode();
            }

            internal void EnterRenameMode()
            {
                m_CollectionHolderToIterate.m_IndexOfCollectionInRenamMode = m_CurrentIndex;
            }

            internal void ExitRenameMode()
            {
                m_CollectionHolderToIterate.m_IndexOfCollectionInRenamMode = -1;
            }
        }
    }
}
