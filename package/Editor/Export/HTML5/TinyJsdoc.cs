

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
	internal static class TinyJsdoc
	{
	    /// <inheritdoc />
	    /// <summary>
	    /// Helper class for comment scoping and jsdoc names
	    /// </summary>
	    internal class Writer : IDisposable
	    {
	        private readonly TinyCodeWriter m_Writer;

	        public Writer(TinyCodeWriter writer)
	        {
	            m_Writer = writer;
	            m_Writer.Line("/**");
	        }
        
	        public void Line(string content)
	        {
	            m_Writer.LineFormat(" * {0}", content);
	        }
		    
		    public void Namespace()
		    {
			    Line("@namespace");
		    }

	        public void Type(string name)
	        {
	            Line($"@type {{{name}}}");
	        }
	        
	        public void Enum(string name)
	        {
	            Line($"@enum {{{name}}}");
	        } 
		    
		    public void Class()
		    {
			    Line("@class");
		    }
		    
		    public void Method()
		    {
			    Line("@method");
		    }
		    
		    public void Extends(string name)
		    {
			    Line($"@extends {name}");
		    }
		    
		    public void Name(string name)
	        {
	            Line($"@name {name}");
	        }
	        
	        public void Desc(string desc)
	        {
	            if (string.IsNullOrEmpty(desc))
	            {
		            return;
	            }
		        
	            Line($"@desc {desc}");
	        }
		    
		    public void Classdesc(string desc)
		    {
			    if (string.IsNullOrEmpty(desc))
			    {
				    return;
			    }
		        
			    Line($"@classdesc {desc}");
		    }
	        
	        public void Readonly()
	        {
	            Line("@readonly");
	        }

		    public void Property(string type, string name, string desc)
		    {
			    Line($"@property {{{type}}} {name} {desc}");
		    }
		    
		    public void Returns(string type, string desc = "")
		    {
			    Line($"@returns {{{type}}} {desc}");
		    }
		    
		    public void Param(string type, string name, string desc = "")
		    {
			    Line($"@param {{{type}}} {name} {desc}");
		    }

	        public void Dispose()
	        {
	            m_Writer.Line(" */");
	        }
	    }
	    
		public static void WriteNamespace(TinyCodeWriter writer, string desc = "")
		{
			using (var w = new Writer(writer))
			{
				w.Namespace();
				w.Desc(desc);
			}
		}
	    
		public static void WriteType(TinyCodeWriter writer, string type, string desc = "")
		{
			using (var w = new Writer(writer))
			{
				w.Type(type);
				w.Desc(desc);
			}
		}
	}
}

