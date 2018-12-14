

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Unity.Properties;

namespace Unity.Tiny
{
    internal class PropertyPathBuilder
    {
        private struct Part
        {
            public string name;
            public int index;
        }

        private Stack<Part> m_Stack;
        private StringBuilder m_CachedStringBuilder;

        public const int DefaultListIndex = -1;
        public const int DefaultInitialCapacity = 4;

        public PropertyPathBuilder()
        : this(DefaultInitialCapacity)
        {
        }

        public PropertyPathBuilder(int initialCapacity)
        {
            m_Stack = new Stack<Part>(initialCapacity);
        }

        public void PushProperty(IProperty property)
        {
            m_Stack.Push(new Part() { name = property.Name, index = DefaultListIndex });
        }

        public void PushListItem(int index)
        {
            m_Stack.Push(new Part() { name = null, index = index });
        }

        public void Pop()
        {
            m_Stack.Pop();
        }

        public int Depth
        {
            get { return m_Stack.Count; }
        }

        public bool IsListItem
        {
            get { return m_Stack.Count > 0 && m_Stack.Peek().index != DefaultListIndex; }
        }

        public int ListIndex
        {
            get { return m_Stack.Count == 0 ? DefaultListIndex : m_Stack.Peek().index; }
        }

        public string PropertyName
        {
            get { return m_Stack.Count == 0 ? null : m_Stack.Peek().name; }
        }

        public override string ToString()
        {
            // @TODO: Use the cached version.
            //        This can be problematic in a debugger ;)
            //        @TODO: Remove next line.
            if (null == m_CachedStringBuilder){}

            var sb = m_CachedStringBuilder = new StringBuilder(m_Stack.Count * 16);
            foreach (var part in m_Stack.Reverse())
            {
                if (part.name == null)
                {
                    sb.Append("[" + part.index + "]");
                }
                else
                {
                    if (sb.Length > 0)
                        sb.Append('.');
                    sb.Append(part.name);
                }
            }
            return sb.ToString();
        }
    }
}

