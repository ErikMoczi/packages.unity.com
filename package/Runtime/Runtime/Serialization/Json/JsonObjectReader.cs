

using System;
using System.Runtime.InteropServices;
using Unity.Properties.Serialization;

namespace Unity.Tiny.Serialization.Json
{
    /// <summary>
    /// Helper to read raw char buffer as json
    /// </summary>
    internal struct JsonObjectReader
    {
        public enum JsonToken
        {
            None = 0,
            BeginObject = 1, // {
            EndObject = 2, // }
            BeginArray = 3, // [
            EndArray = 4, // ]
            Number = 5,
            String = 6,
            True = 7,
            False = 8,
            Null = 9,
            ValueSeparator = 10,
            NameSeparator = 11
        }

        private readonly char[] m_Buffer;
        private readonly int m_Length;
        private int m_Position;
        private int m_Line;
        private int m_LastLinePosition;

        public char[] Buffer => m_Buffer;
        public int Length => m_Length;

        public int Position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public int Line
        {
            get { return m_Line; }
            set { m_Line = value; }
        }

        public JsonObjectReader(string json)
            : this(json.ToCharArray(), 0, json.Length)
        {
        }

        public JsonObjectReader(char[] buffer, int position, int length)
        {
            m_Buffer = buffer;
            m_Position = position;
            m_Length = length;
            m_Line = 0;
            m_LastLinePosition = 0;
        }

        public char ReadChar()
        {
            return m_Buffer[m_Position++];
        }

        public bool ReadBeginObject()
        {
            return ReadChar('{');
        }

        public bool ReadEndObject()
        {
            return ReadChar('}');
        }
        
        public bool ReadBeginArray()
        {
            return ReadChar('[');
        }

        public bool ReadEndArray()
        {
            return ReadChar(']');
        }

        public bool ReadValueSeparator()
        {
            return ReadChar(',');
        }

        public bool ReadNameSeparator()
        {
            return ReadChar(':');
        }
        
        /// <summary>
        /// Returns true while the reader is in an object
        /// </summary>
        public bool ReadInObject()
        {
            SkipWhiteSpace();
            
            if (m_Position >= m_Length)
            {
                return false;
            }

            switch (m_Buffer[m_Position])
            {
                case ',':
                    m_Position++;
                    return true;
                case '}':
                    m_Position++;
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// Returns true while the reader is in an array 
        /// </summary>
        public bool ReadInArray()
        {
            SkipWhiteSpace();
            
            if (m_Position >= m_Length)
            {
                return false;
            }

            switch (m_Buffer[m_Position])
            {
                case ',':
                    m_Position++;
                    return true;
                case ']':
                    m_Position++;
                    return false;
            }

            return true;
        }

        private bool ReadChar(char c)
        {
            SkipWhiteSpace();

            if (m_Position >= m_Length || m_Buffer[m_Position] != c)
            {
                return false;
            }

            m_Position++;
            return true;
        }

        private bool ReadStringWithValidation(string str)
        {
            SkipWhiteSpace();
            for (var i = 0; i < str.Length; i++)
            {
                if (m_Position >= m_Length)
                {
                    return false;
                }

                if (m_Buffer[m_Position] != str[i])
                {
                    return false;
                }

                m_Position++;
            }
            
            return true;
        }
        
        /// <summary>
        /// Reads the property name and skips the next name separator
        /// </summary>
        /// <returns></returns>
        public string ReadPropertyName()
        {
            var name = ReadString();
            ReadNameSeparator();
            return name;
        }

        /// <summary>
        /// Reads the property name and skips the next name separator
        /// </summary>
        /// <returns></returns>
        public ArraySegment<char> ReadPropertyNameSegment()
        {
            var segment = ReadStringSegment();
            ReadNameSeparator();
            return segment;
        }

        public void ReadUntilValueSeparator()
        {
            SkipWhiteSpace();
            
            var depth = 0;

            for (var i = m_Position; i < m_Length; i++)
            {
                if (m_Buffer[i] == ',' || m_Buffer[i] == '}' || m_Buffer[i] == ']')
                {
                    if (m_Buffer[i] == ',' || depth == 0)
                    {
                        m_Position = i;
                        return;
                    }
                }
                else if (m_Buffer[i] == '{')
                {
                    depth++;
                }
                else if (m_Buffer[i] == '[')
                {
                    depth++;
                }
            }

            m_Position = m_Length;
        }

        public void ReadUntilBeginObject()
        { 
            for (var i = m_Position; i < m_Length; i++)
            {
                if (m_Buffer[i] == '{')
                {
                    m_Position = i + 1;
                    break;
                }
            }

            m_Position = m_Length;
        }

        public ArraySegment<char> ReadStringSegment()
        {
            SkipWhiteSpace();

            if (m_Buffer[m_Position] != '\"')
            {
                throw new Exception($"JsonObjectReader Unexpected '{m_Buffer[m_Position]}' token. Expected '\"' Line {Line}");
            }

            var begin = ++m_Position;

            for (; m_Position < m_Length; m_Position++)
            {
                if (m_Buffer[m_Position] == '\"' && m_Buffer[m_Position - 1] != '\\')
                {
                    break;
                }
            }

            var segment = new ArraySegment<char>(m_Buffer, begin, m_Position - begin);

            ++m_Position;

            return segment;
        }

        public string ReadString()
        {
            SkipWhiteSpace();

            if (m_Buffer[m_Position] != '\"')
            {
                throw new Exception($"JsonObjectReader Expected '\"' but found '{m_Buffer[m_Position]}'. Line=[{Line}] Character=[{m_Position - m_LastLinePosition}]");
            }

            var begin = ++m_Position;

            for (; m_Position < m_Length; m_Position++)
            {
                if (m_Buffer[m_Position] == '\n')
                {
                    m_LastLinePosition = m_Position;
                    m_Line++;
                }
                
                if (m_Buffer[m_Position] == '\"' && m_Buffer[m_Position - 1] != '\\')
                {
                    break;
                }
            }

            var str = new string(m_Buffer, begin, m_Position - begin);

            ++m_Position;

            return Properties.Serialization.Json.DecodeJsonString(str);
        }
        
        public ushort ReadUInt16()
        {
            return Convert.ToUInt16(ReadNumber());
        }
        
        public uint ReadUInt32()
        {
            return Convert.ToUInt32(ReadNumber());
        }
        
        public ulong ReadUInt64()
        {
            return Convert.ToUInt64(ReadNumber());
        }
        
        public int ReadInt32()
        {
            return Convert.ToInt32(ReadNumber());
        }

        public long ReadInt64()
        {
            return Convert.ToInt64(ReadNumber());
        }
        
        public float ReadFloat32()
        {
            return Convert.ToSingle(ReadNumber());
        }
        
        public double ReadFloat64()
        {
            return Convert.ToDouble(ReadNumber());
        } 
        
        public bool ReadBoolean()
        {
            SkipWhiteSpace();

            var c = m_Buffer[m_Position];

            if (c == 't' || c == 'T')
            {
                // Read the remaining characters with validation
                if (!ReadStringWithValidation("true"))
                {
                    throw new Exception($"Unexpected '{m_Buffer[m_Position]}' token when parsing boolean.");
                }
                return true;
            }
            
            if (c == 'f' || c == 'F')
            {
                if (!ReadStringWithValidation("false"))
                {
                    throw new Exception($"Unexpected '{m_Buffer[m_Position]}' token when parsing boolean.");
                }
                
                return false;
            }
            
            throw new Exception($"Unexpected '{c}' token when parsing boolean. Expected 't' or 'f'");
        }

        private bool EqualsStringSegment(string str, ArraySegment<char> segment)
        {
            if (segment.Count != str.Length)
            {
                return false;
            }

            var array = segment.Array;
            var offset = segment.Offset;
            for (var i = 0; i < str.Length; i++)
            {
                if (array[offset + i] != str[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        public string ReadNumber()
        {
            SkipWhiteSpace();

            var begin = m_Position;
            
            for (; m_Position < m_Length; m_Position++)
            {
                var c = m_Buffer[m_Position];

                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                    case '+':
                    case '.':
                    case 'e':
                    case 'E':
                        continue;
                }

                break;
            }

            return new string(m_Buffer, begin, m_Position - begin);
        }
        
        public ArraySegment<char> ReadNumberSegment()
        {
            SkipWhiteSpace();

            var begin = m_Position;
            
            for (; m_Position < m_Length; m_Position++)
            {
                var c = m_Buffer[m_Position];

                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                    case '+':
                    case '.':
                    case 'e':
                    case 'E':
                        continue;
                }

                break;
            }

            return new ArraySegment<char>(m_Buffer, begin, m_Position - begin);
        }

        public JsonToken GetCurrentJsonToken()
        {
            SkipWhiteSpace();

            if (m_Position >= m_Length)
            {
                return JsonToken.None;
            }

            switch (m_Buffer[m_Position])
            {
                case '{': return JsonToken.BeginObject;
                case '}': return JsonToken.EndObject;
                case '[': return JsonToken.BeginArray;
                case ']': return JsonToken.EndArray;
                case 't': return JsonToken.True;
                case 'f': return JsonToken.False;
                case 'n': return JsonToken.Null;
                case ',': return JsonToken.ValueSeparator;
                case ':': return JsonToken.NameSeparator;
                case '\"': return JsonToken.String;
                case '0': return JsonToken.Number;
                case '1': return JsonToken.Number;
                case '2': return JsonToken.Number;
                case '3': return JsonToken.Number;
                case '4': return JsonToken.Number;
                case '5': return JsonToken.Number;
                case '6': return JsonToken.Number;
                case '7': return JsonToken.Number;
                case '8': return JsonToken.Number;
                case '9': return JsonToken.Number;
                case '.': return JsonToken.Number;
                case '-': return JsonToken.Number;
                case '+': return JsonToken.Number;
                case 'e': return JsonToken.Number;
                case 'E': return JsonToken.Number;
                default: return JsonToken.None;
            }
        }

        private void SkipWhiteSpace()
        {
            for (var i = m_Position; i < m_Length; i++)
            {
                switch (m_Buffer[i])
                {
                    case ' ':
                    case '\t':
                    case '\r':
                        continue;
                    case '/':
                        if (!SkipCStyleComments(ref i))
                        {
                            m_Position = i;
                            return;
                        }                            
                        continue;
                    case '\n':
                        // Track line information for debugging purposes
                        m_LastLinePosition = i;
                        m_Line++;
                        continue;
                }

                m_Position = i;
                return;
            }

            m_Position = m_Length;
        }

        private bool SkipCStyleComments(ref int i)
        {
            // Track the start of the comment in-case something goes wrong
            var start = i;
            var startLine = m_Line;
            var startLastLinePosition = m_LastLinePosition;
            
            // There is only one character left in the stream
            // This can not possibly be a valid comment
            if (i + 1 >= m_Length)
            {
                // Error out and return the current character '/'
                return false;
            }

            // Single line comment
            if (m_Buffer[i] == '/' && m_Buffer[i + 1] == '/')
            {
                // Skip the begin comment scope "//"
                i += 2;

                for (; i < m_Length; i++)
                {
                    // Ignore everything up until the first end line
                    if (m_Buffer[i] != '\n')
                    {
                        continue;
                    }
                    
                    // Track line information for debugging purposes
                    m_LastLinePosition = i;
                    m_Line++;
                    break;
                }
                
                // End of single line comment OR end of stream
                return true;
            }

            // Multi line comment
            if (m_Buffer[i] == '/' && m_Buffer[i + 1] == '*')
            {
                // Skip the begin comment scope "/*"
                i += 2;
                
                for (; i < m_Length - 1; i++)
                {
                    if (m_Buffer[i] == '\n')
                    {
                        // Track line information for debugging purposes
                        m_LastLinePosition = i;
                        m_Line++;
                        continue;
                    }
                    
                    // Ignore everything up until the end comment scope
                    if (m_Buffer[i] != '*' || m_Buffer[i + 1] != '/')
                    {
                        continue;
                    }
                    
                    // Skip the end comment scope "*/"
                    i++;
                    return true;
                }
            }
            
            // We did not find the end of the comment line
            // Error out and log the error at the correct position
            i = start;
            m_Line = startLine;
            m_LastLinePosition = startLastLinePosition;
            return false;
        }
    }
}

