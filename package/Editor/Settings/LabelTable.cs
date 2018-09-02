using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    [Serializable]
    internal class LabelTable
    {
        [SerializeField]
        private List<string> m_labelNames = new List<string>(new string[] { "default" });

        internal List<string> labelNames { get { return m_labelNames; } }
        private const int kNameCountCap = 3;
        
        internal void AddLabelName(string name)
        {
            if(!m_labelNames.Contains(name))
            { 
                m_labelNames.Add(name);
            }
        }

        internal string GetUniqueLabelName(string name)
        {
            var newName = name;
            int counter = 1;
            while (counter < 100)
            {
                if (!m_labelNames.Contains(newName))
                    return newName;
                newName = name + counter.ToString();
                counter++;
            }
            return string.Empty;
        }

        internal bool RemoveLabelName(string name)
        {
            return m_labelNames.Remove(name);
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        internal string GetString(HashSet<string> val, float width) //TODO - use width to add the "..." in the right place.
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int counter = 0;
            foreach (var v in m_labelNames)
            {
                if (val.Contains(v))
                {
                    if (counter >= kNameCountCap)
                    {
                        sb.Append("...");
                        break;
                    }
                    else
                    {
                        if (counter > 0)
                            sb.Append(", ");
                        sb.Append(v);
                        counter++;
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        internal long GetMask(HashSet<string> maskSet)
        {
            if (maskSet.Count == 0)
                return 0;
            long one = 1;
            long val = 0;
            for (int i = 0; i < m_labelNames.Count; i++)
                if (maskSet.Contains(m_labelNames[i]))
                    val |= (long)(one << i);
            return val;
        }
    }
}
