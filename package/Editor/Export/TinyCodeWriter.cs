

using System;
using System.Text;

namespace Unity.Tiny
{
    internal enum CodeBraceLayout
    {
        EndOfLine,
        EndOfLineSpace,
        NextLine,
        NextLineIndent
    }

    internal class CodeStyle
    {
        public string Indent { get; set; }
        public CodeBraceLayout codeBraceLayout { get; set; }
        public string BeginBrace { get; set; }
        public string EndBrace { get; set; }
        public string NewLine { get; set; }

        public static CodeStyle JavaScript => new CodeStyle()
        {
            Indent = "    ",
            codeBraceLayout = CodeBraceLayout.EndOfLineSpace,
            BeginBrace = "{",
            EndBrace = "}",
            NewLine = Environment.NewLine
        };
        
        public static CodeStyle CSharp => new CodeStyle()
        {
            Indent = "    ",
            codeBraceLayout = CodeBraceLayout.NextLine,
            BeginBrace = "{",
            EndBrace = "}",
            NewLine = Environment.NewLine
        };
    }

    internal class CodeWriterScope : IDisposable
    {
        private Action m_Disposed;

        public CodeWriterScope(Action disposed)
        {
            m_Disposed = disposed;
        }

        public void Dispose()
        {
            if (null == m_Disposed)
            {
                return;
            }

            m_Disposed.Invoke();
            m_Disposed = null;
        }
    }

    internal class TinyCodeWriter
    {
        private readonly StringBuilder m_StringBuilder;
        public StringBuilder Builder => m_StringBuilder;

        private int m_Indent;

        public CodeStyle CodeStyle { get; set; }

        public int Length
        {
            get { return m_StringBuilder.Length; }
            set { m_StringBuilder.Length = value; }
        }

        public TinyCodeWriter()
            : this(CodeStyle.JavaScript)
        { }

        public TinyCodeWriter(CodeStyle codeStyle)
        {
            m_StringBuilder = new StringBuilder();
            CodeStyle = codeStyle;
        }

        public TinyCodeWriter Prepend(string value)
        {
            m_StringBuilder.Insert(0, value);
            return this;
        }
        
        public TinyCodeWriter WriteRaw(string value)
        {
            m_StringBuilder.Append(value);
            return this;
        }

        public TinyCodeWriter Line()
        {
            m_StringBuilder.Append(CodeStyle.NewLine);
            return this;
        }

        public TinyCodeWriter LineFormat(string format, params object[] args)
        {
            return Line(string.Format(format, args));
        }

        public TinyCodeWriter Line(string content)
        {
            WriteIndent();
            m_StringBuilder.Append(content);
            m_StringBuilder.Append(CodeStyle.NewLine);
            return this;
        }

        public CodeWriterScope Scope(string content, bool endLine = true)
        {
            return Scope(content, CodeStyle.codeBraceLayout, endLine);
        }

        public CodeWriterScope Scope(string content, CodeBraceLayout layout, bool endLine = true)
        {
            WriteIndent();

            m_StringBuilder.Append(content);

            WriteBeginScope(layout);

            return new CodeWriterScope(() =>
            {
                WriteEndScope(layout, endLine);
            });
        }

        public void WriteBeginScope(CodeBraceLayout layout)
        {
            switch (layout)
            {
                case CodeBraceLayout.EndOfLine:
                    m_StringBuilder.Append(CodeStyle.BeginBrace);
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    break;
                case CodeBraceLayout.EndOfLineSpace:
                    m_StringBuilder.AppendFormat(" {0}", CodeStyle.BeginBrace);
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    break;
                case CodeBraceLayout.NextLine:
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    WriteIndent();
                    m_StringBuilder.Append(CodeStyle.BeginBrace);
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    break;
                case CodeBraceLayout.NextLineIndent:
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    IncrementIndent();
                    WriteIndent();
                    m_StringBuilder.Append(CodeStyle.BeginBrace);
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    break;
            }
            IncrementIndent();
        }

        public void WriteEndScope(CodeBraceLayout layout, bool endLine)
        {
            switch (layout)
            {
                case CodeBraceLayout.EndOfLine:
                case CodeBraceLayout.EndOfLineSpace:
                case CodeBraceLayout.NextLine:
                    DecrementIndent();
                    WriteIndent();
                    WriteRaw(CodeStyle.EndBrace);
                    break;
                case CodeBraceLayout.NextLineIndent:
                    DecrementIndent();
                    WriteRaw(CodeStyle.EndBrace);
                    WriteIndent();
                    DecrementIndent();
                    break;
            }

            if (endLine)
            {
                WriteRaw(CodeStyle.NewLine);
            }
        }

        public void IncrementIndent()
        {
            m_Indent++;
        }

        public void DecrementIndent()
        {
            if (m_Indent > 0)
            {
                m_Indent--;
            }
        }

        public void Clear()
        {
            m_StringBuilder.Length = 0;
        }

        public void WriteIndent()
        {
            for (var i = 0; i < m_Indent; i++)
            {
                m_StringBuilder.Append(CodeStyle.Indent);
            }
        }

        public override string ToString()
        {
            return m_StringBuilder.ToString();
        }

        public string Substring(int begin, int end = 0)
        {
            return m_StringBuilder.ToString(begin, end == 0 ? m_StringBuilder.Length - begin : end);
        }
    }
}

