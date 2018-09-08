using System.Xml;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.MemoryProfiler.Editor.Debuging;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    public class ViewScheme : Database.Scheme
    {
        public string name = "<unknown>";
        public Database.Scheme baseScheme;
        public ViewTable[] tables;
        public System.Collections.Generic.Dictionary<string, ViewTable> tablesByName = new System.Collections.Generic.Dictionary<string, ViewTable>();

        public override string GetDisplayName()
        {
            return baseScheme.GetDisplayName();
        }

        public override bool OwnsTable(Table table)
        {
            if (table.scheme == this) return true;
            if (System.Array.IndexOf(tables, table) >= 0) return true;
            return baseScheme.OwnsTable(table);
        }

        public override long GetTableCount()
        {
            if (baseScheme != null)
            {
                return baseScheme.GetTableCount() + tables.Length;
            }
            return tables.Length;
        }

        public override Table GetTableByIndex(long index)
        {
            if (baseScheme != null && index < baseScheme.GetTableCount())
            {
                return baseScheme.GetTableByIndex(index);
            }
            else
            {
                index -= baseScheme.GetTableCount();
                return tables[index];
            }
        }

        public override Table GetTableByName(string name)
        {
            ViewTable vt;
            if (tablesByName.TryGetValue(name, out vt))
            {
                return vt;
            }
            if (baseScheme != null)
            {
                return baseScheme.GetTableByName(name);
            }
            return null;
        }

        public override Table GetTableByName(string name, ParameterSet param)
        {
            ViewTable vt;
            if (tablesByName.TryGetValue(name, out vt))
            {
                return vt;
            }
            if (baseScheme != null)
            {
                return baseScheme.GetTableByName(name, param);
            }
            return null;
        }

        public class Builder
        {
            public string name;
            protected System.Collections.Generic.List<ViewTable.Builder> viewTable = new System.Collections.Generic.List<ViewTable.Builder>();

            public ViewTable.Builder AddTable()
            {
                ViewTable.Builder t = new ViewTable.Builder();
                viewTable.Add(t);
                return t;
            }

            public ViewScheme Build(Database.Scheme baseScheme)
            {
                ViewScheme vs = new ViewScheme();
                vs.baseScheme = baseScheme;
                vs.tables = new ViewTable[viewTable.Count];
                vs.name = name;
                int i = 0;
                foreach (var tBuilder in viewTable)
                {
                    var table = tBuilder.Build(vs, baseScheme);
                    vs.tables[i] = table;
                    vs.tablesByName.Add(table.GetName(), table);
                    ++i;
                }
                return vs;
            }

            public static Builder LoadFromXML(XmlElement root)
            {
                Builder b = new Builder();
                b.name = root.GetAttribute("name");
                using (ScopeDebugContext.Func(() => { return "ViewSchema '" + b.name + "'"; }))
                {
                    foreach (XmlNode node in root.ChildNodes)
                    {
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            XmlElement e = (XmlElement)node;
                            if (e.Name == "View")
                            {
                                var v = ViewTable.Builder.LoadFromXML(e);
                                if (v != null)
                                {
                                    b.viewTable.Add(v);
                                }
                            }
                        }
                    }
                    return b;
                }
            }

            /// <summary>
            /// Load an XML file from the editor built-in resources
            /// </summary>
            /// <param name="assetPath"></param>
            /// <returns></returns>
            public static Builder LoadFromInternalTextAsset(string assetPath)
            {
                var res = EditorGUIUtility.Load(assetPath) as TextAsset;
                if (res == null)
                    return null;

                MemoryStream assetstream = new MemoryStream(res.bytes);
                XmlReader xmlReader = XmlReader.Create(assetstream);

                XmlDocument doc = new XmlDocument();
                doc.Load(xmlReader);
                return LoadFromXML(doc.DocumentElement);
            }

            public static Builder LoadFromXMLFile(string filename)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filename);
                return LoadFromXML(doc.DocumentElement);
            }
        }
    }
}
