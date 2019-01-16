using System;

namespace UnityEngine.Experimental.Input.Utilities
{
    // Work with substrings without actually allocating strings.
    internal struct Substring : IComparable<Substring>
    {
        internal string m_String;
        internal int m_Index;
        internal int m_Length;

        public bool isEmpty
        {
            get { return m_Length == 0; }
        }

        public Substring(string str)
        {
            m_String = str;
            m_Index = 0;
            if (str != null)
                m_Length = str.Length;
            else
                m_Length = 0;
        }

        public Substring(string str, int index, int length)
        {
            Debug.Assert(str == null || index < str.Length);
            Debug.Assert(str != null || length == 0);

            m_String = str;
            m_Index = index;
            m_Length = length;
        }

        public override bool Equals(object obj)
        {
            if (obj is Substring)
                return Equals((Substring)obj);

            var str = obj as string;
            if (str != null)
                return Equals(str);

            return false;
        }

        public bool Equals(string other)
        {
            if (string.IsNullOrEmpty(other))
            {
                return m_Length == 0;
            }

            if (other.Length != m_Length)
                return false;

            for (var i = 0; i < m_Length; ++i)
                if (other[i] != m_String[m_Index + i])
                    return false;

            return true;
        }

        public bool Equals(Substring other)
        {
            return CompareTo(other) == 0;
        }

        public bool Equals(InternedString other)
        {
            if (length != other.length)
                return false;

            return string.Compare(m_String, m_Index, other.ToString(), 0, length,
                StringComparison.OrdinalIgnoreCase) == 0;
        }

        public int CompareTo(Substring other)
        {
            if (length != other.length)
            {
                if (length < other.length)
                    return -1;
                return 1;
            }

            return string.Compare(m_String, m_Index, other.m_String, other.m_Index, m_Length);
        }

        public override string ToString()
        {
            if (m_String == null)
                return string.Empty;

            return m_String.Substring(m_Index, m_Length);
        }

        public override int GetHashCode()
        {
            if (m_String == null)
                return 0;

            if (m_Index == 0 && m_Length == m_String.Length)
                return m_String.GetHashCode();

            ////FIXME: this is bad... shouldn't allocate
            return ToString().GetHashCode();
        }

        public static bool operator==(Substring a, Substring b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(Substring a, Substring b)
        {
            return !a.Equals(b);
        }

        public static bool operator==(Substring a, InternedString b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(Substring a, InternedString b)
        {
            return !a.Equals(b);
        }

        public static bool operator==(InternedString a, Substring b)
        {
            return b.Equals(a);
        }

        public static bool operator!=(InternedString a, Substring b)
        {
            return !b.Equals(a);
        }

        public static implicit operator Substring(string s)
        {
            return new Substring(s);
        }

        public int length
        {
            get { return m_Length; }
        }

        public int index
        {
            get { return m_Index; }
        }

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Length)
                    throw new IndexOutOfRangeException();
                return m_String[m_Index + index];
            }
        }
    }
}
