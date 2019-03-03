using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor.U2D.Interface;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.U2D
{
    [Serializable]
    public class SerializableSelection : ISelection, ISerializationCallbackReceiver
    {
        public int Count
        {
            get { return m_Selection.Count; }
        }

        public void OnBeforeSerialize()
        {
            m_Keys = new List<int>(m_Selection).ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_Selection.Clear();
            m_Selection.UnionWith(m_Keys);
        }

        public int single
        {
            get
            {
                int index = -1;

                if (Count == 1)
                {
                    using (IEnumerator<int> enumerator = m_Selection.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                            index = enumerator.Current;
                    }
                }

                return index;
            }
        }

        public int any
        {
            get
            {
                int index = -1;

                if (Count > 0)
                {
                    using (IEnumerator<int> enumerator = m_Selection.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                            index = enumerator.Current;
                    }
                }

                return index;
            }
        }

        public void Clear()
        {
            m_Selection.Clear();
        }

        public void BeginSelection()
        {
            m_TemporalSelection.Clear();

            m_SelectionInProgress = true;
        }

        public void EndSelection(bool select)
        {
            m_SelectionInProgress = false;

            if (select)
                m_Selection.UnionWith(m_TemporalSelection);
            else
                m_Selection.ExceptWith(m_TemporalSelection);

            m_TemporalSelection.Clear();
        }

        public void Select(int index, bool select)
        {
            if (select)
                selection.Add(index);
            else if (IsSelected(index))
                selection.Remove(index);
        }

        public bool IsSelected(int index)
        {
            return m_Selection.Contains(index) || m_TemporalSelection.Contains(index);
        }

        HashSet<int> selection
        {
            get
            {
                if (m_SelectionInProgress)
                    return m_TemporalSelection;

                return m_Selection;
            }
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return m_Selection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)m_Selection.GetEnumerator();
        }

        [SerializeField]
        int[] m_Keys = new int[0];

        HashSet<int> m_Selection = new HashSet<int>();
        HashSet<int> m_TemporalSelection = new HashSet<int>();

        bool m_SelectionInProgress = false;
    }
}
