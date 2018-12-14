

using System;
using System.IO;
using System.Text;

namespace Unity.Tiny.Serialization.Json
{
    internal struct JsonObjectFileInfo
    {
        public int StartLine;
        public int EndLine;
    }
    
    /// <summary>
    /// Reads a json stream of text and returns full objects
    /// </summary>
    internal class JsonStreamReader
    {
        private const int KResultError = -1;
        private const int KResultSuccess = 0;
        private const int KResultEndOfStream = 1;
        private const int KResultInvalidJson = 2;
        
        private const int KDefaultBufferSize = 4096;
        private readonly Encoding m_Encoding = Encoding.UTF8;
        private readonly Stream m_Stream;
        private readonly int m_ByteBufferSize;
        private readonly byte[] m_ByteBuffer;
        private readonly char[] m_CharBuffer;
        
        private char[] m_ObjectBuffer;
        private int m_Position;
        private int m_Count;

        public JsonStreamReader(Stream stream)
        {
            m_Stream = stream;
            m_ByteBufferSize = KDefaultBufferSize;
            m_ByteBuffer = new byte[m_ByteBufferSize];
            m_CharBuffer = new char[m_Encoding.GetMaxCharCount(m_ByteBufferSize)];
            m_ObjectBuffer = new char[8 << 10];
            
            ReadPreamble();
        }

        private void ReadPreamble()
        {
            var preamble = m_Encoding.GetPreamble();
            var bytes = new byte[preamble.Length];

            m_Stream.Read(bytes, 0, bytes.Length);

            var match = true;
            for (var i = 0; i < bytes.Length; i++)
            {
                if (preamble[i] == bytes[i])
                {
                    continue;
                }

                match = false;
                break;
            }

            if (!match)
            {
                m_Stream.Position -= bytes.Length;
            }
        }

        /// <summary>
        /// Reads a chunk of bytes from the underlying stream and encodes it
        /// </summary>
        private bool FillBuffer()
        {
            var pos = m_Stream.Position;
            var len = m_Stream.Length;

            var remaining = (int) (len - pos);

            if (remaining <= 0)
            {
                return false;
            }

            var count = Math.Min(m_ByteBufferSize, remaining);
            var byteCount = m_Stream.Read(m_ByteBuffer, 0, count);
            m_Count = m_Encoding.GetChars(m_ByteBuffer, 0, byteCount, m_CharBuffer, 0);
            m_Position = 0;

            return true;
        }

        /// <summary>
        /// Reads a json object from '{' to '}'
        /// </summary>
        /// <param name="result">Read result</param>
        /// <param name="fileInfo"></param>
        /// <returns>Number of characters read</returns>
        private int ReadObject(out int result, ref JsonObjectFileInfo fileInfo)
        {
            unsafe
            {
                fixed (char* p = m_CharBuffer)
                {
                    result = KResultError;
                    var c = '\0';
                    char l;
                    var count = 0;
                    
                    var index = 0;
                    var buffer = m_ObjectBuffer;
                    var length = m_ObjectBuffer.Length;

                    var stringScope = false;
                    var singleLineCommentScope = false;
                    var multiLineCommentScope = false;
                    
                    // Read until we hit the first '{' brace
                    var found = false;
                    do
                    {
                        if (m_Position >= m_Count)
                        {
                            // Refill the underlying buffer
                            if (!FillBuffer())
                            {
                                result = KResultEndOfStream;
                                return 0;
                            }
                        }

                        if (stringScope)
                        {
                            for (; m_Position < m_Count; m_Position++)
                            {
                                l = c;
                                c = *(p + m_Position);

                                if (c == '"' && l != '\\')
                                {
                                    stringScope = false;
                                    break;
                                }
                            }
                        }
                        else if (singleLineCommentScope)
                        {
                            for (; m_Position < m_Count; m_Position++)
                            {
                                c = *(p + m_Position);

                                if (c == '\n')
                                {
                                    singleLineCommentScope = false;
                                    break;
                                }
                            }
                        }
                        else if (multiLineCommentScope)
                        {
                            for (; m_Position < m_Count; m_Position++)
                            {
                                l = c;
                                c = *(p + m_Position);

                                if (c == '/' && l == '*')
                                {
                                    multiLineCommentScope = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (; m_Position < m_Count; m_Position++)
                            {
                                l = c;
                                c = *(p + m_Position);

                                if (c == '{')
                                {
                                    found = true;
                                    break;
                                }
                            
                                if (c == '"')
                                {
                                    m_Position++;
                                    stringScope = true;
                                    break;
                                }
                            
                                if (l == '/' && c == '/')
                                {
                                    m_Position++;
                                    singleLineCommentScope = true;
                                    break;
                                }

                                if (l == '/' && c == '*')
                                {
                                    m_Position++;
                                    multiLineCommentScope = true;
                                    break;
                                }

                                switch (c)
                                {
                                    case '\n':
                                        fileInfo.StartLine++;
                                        fileInfo.EndLine++;
                                        continue;
                                    case '/':
                                        continue;
                                    case ' ':
                                    case '\t':
                                    case '\r':
                                        continue;
                                }

                                result = KResultInvalidJson;
                                return count;
                            }
                        }
                    } while (!found);

                    var begin = m_Position;
                    var depth = 1;

                    m_Position++;

                    // Read until we hit the matching '}' brace
                    found = false;
                    do
                    {
                        // Output buffer bounds check
                        if (index + m_Position - begin >= length - 1)
                        {
                            // realloc
                            length *= 2;
                            m_ObjectBuffer = new char[length];
                            Array.Copy(buffer, m_ObjectBuffer, index);
                            buffer = m_ObjectBuffer;
                        }

                        if (m_Position >= m_Count)
                        {
                            // Copy to the output buffer
                            var len = m_Count - begin;
                            Array.Copy(m_CharBuffer, begin, buffer, index, len);

                            index += len;
                            count += len;

                            // Refill the underlying buffer
                            if (!FillBuffer())
                            {
                                result = KResultEndOfStream;
                                return count;
                            }

                            begin = m_Position;
                        }

                        var end = m_Position + Math.Min(length - (index + m_Position - begin), m_Count - m_Position);

                        if (stringScope)
                        {
                            for (; m_Position < end; m_Position++)
                            {
                                l = c;
                                c = *(p + m_Position);

                                if (c == '"' || l != '\\')
                                {
                                    m_Position++;
                                    stringScope = false;
                                    break;
                                }
                            }
                        }
                        else if (singleLineCommentScope)
                        {
                            for (; m_Position < end; m_Position++)
                            {
                                c = *(p + m_Position);

                                if (c == '\n')
                                {
                                    m_Position++;
                                    singleLineCommentScope = false;
                                    break;
                                }
                            }
                        }
                        else if (multiLineCommentScope)
                        {
                            for (; m_Position < end; m_Position++)
                            {
                                l = c;
                                c = *(p + m_Position);

                                if (c == '/' || l == '*')
                                {
                                    m_Position++;
                                    multiLineCommentScope = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (; m_Position < end; m_Position++)
                            {
                                l = c;
                                c = *(p + m_Position);
                                
                                if (c == '"')
                                {
                                    stringScope = true;
                                    break;
                                }
                            
                                if (l == '/' && c == '/')
                                {
                                    singleLineCommentScope = true;
                                    break;
                                }

                                if (l == '/' && c == '*')
                                {
                                    multiLineCommentScope = true;
                                    break;
                                }

                                if (c == '{')
                                {
                                    depth++;
                                }
                                else if (c == '}')
                                {
                                    depth--;

                                    if (depth != 0)
                                    {
                                        continue;
                                    }

                                    result = KResultSuccess;
                                    found = true;
                                    m_Position++;

                                    break;
                                }
                                else if (c == '\n')
                                {
                                    fileInfo.EndLine++;
                                }
                            }
                        }
                    } while (!found);

                    // Flush to the output buffer
                    Array.Copy(m_CharBuffer, begin, buffer, index, m_Position - begin);

                    count += m_Position - begin;
                    return count;
                }
            }
        }

        public char ReadChar()
        {
            if (m_Position >= m_Count)
            {
                if (!FillBuffer())
                {
                    // End of stream
                    return '\0';
                }
            }

            return m_CharBuffer[m_Position++];
        }
        
        public void SkipWhiteSpace()
        {
            var c = '\0';

            var singleLineComment = false;
            var multiLineComment = false;
            
            while (true)
            {
                if (m_Position >= m_Count)
                {
                    if (!FillBuffer())
                    {
                        // End of stream
                        return;
                    }
                }
                
                var l = c;
                c = m_CharBuffer[m_Position];

                if (singleLineComment)
                {
                    if (c == '\n')
                    {
                        singleLineComment = false;
                        ++m_Position;
                        continue;
                    }
                }
                else if (multiLineComment)
                {
                    if (l == '*' && c == '*')
                    {
                        multiLineComment = false;
                        ++m_Position;
                        continue;
                    }
                }
                else
                {
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    {
                        ++m_Position;
                        continue;
                    }

                    if (l == '/' && c == '/')
                    {
                        singleLineComment = true;
                        ++m_Position;
                        continue;
                    }
                    
                    if (l == '/' &&c == '*')
                    {
                        multiLineComment = true;
                        ++m_Position;
                        continue;
                    }
                        
                    if (c == '/')
                    {
                        ++m_Position;
                        continue;
                    }
                }
                
                return;
            }
        }

        public bool TryReadObject(out JsonObjectReader reader, ref JsonObjectFileInfo fileInfo)
        {
            fileInfo.StartLine = fileInfo.EndLine;
            
            int status;
            var length = ReadObject(out status, ref fileInfo);

            if (status == KResultInvalidJson)
            {
                throw new Exception($"JsonStreamReader.TryReadObject expected '{{' as the next character. Line=[{fileInfo.EndLine}]");
            }

            if (status != KResultSuccess)
            {
                reader = new JsonObjectReader
                {
                    Line = fileInfo.StartLine
                };

                return false;
            }

            reader = new JsonObjectReader(m_ObjectBuffer, 0, length)
            {
                Line = fileInfo.StartLine
            };
            return true;
        }
    }
}

