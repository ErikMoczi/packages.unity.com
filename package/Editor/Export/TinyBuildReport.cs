

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.Properties;
using Unity.Properties.Serialization;
using Unity.Tiny.Serialization;
using Unity.Tiny.Serialization.Json;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    internal partial class TinyBuildReport
    {
        public const string ProjectNode = "Project";
        public const string RuntimeNode = "Runtime";
        public const string AssetsNode = "Assets";
        public const string CodeNode = "Code";

        private static readonly byte[] s_Buffer = new byte[64 * 1024];

        public TreeNode Root { get; private set; }

        internal TinyBuildReport(string name)
        {
            Root = new TreeNode(name);
        }

        private TinyBuildReport()
        {
        }

        public static TinyBuildReport FromJson(string json)
        {
            return new TinyBuildReport
            {
                Root = TreeNode.FromJson(json)
            };
        }

        public void Update()
        {
            Root.Update();
        }

        public partial class TreeNode
        {
            public TreeNode Parent { get; set; }

            public TreeNode Root
            {
                get
                {
                    if (Parent == null)
                    {
                        return this;
                    }

                    var node = Parent;
                    while (node.Parent != null)
                    {
                        node = node.Parent;
                    }

                    return node;
                }
            }

            private TreeNode(TreeNode parent, Item item)
            {
                // Note: parent CAN be null (for root node)
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                Item = item;
                Parent = parent;
            }

            private TreeNode() :
                this(null, new Item())
            {
            }

            private TreeNode(TreeNode parent) :
                this(parent, new Item())
            {
            }

            public TreeNode(string name) :
                this(null, new Item(name, 0, null))
            {
            }

            public void Reset()
            {
                Item = new Item();
            }

            public void Reset(string name, long size = 0, Object obj = null)
            {
                Item = new Item(name, size, obj);
            }

            public void Reset(string name, byte[] bytes, Object obj = null)
            {
                Item = new Item(name, bytes, obj);
            }

            public void Reset(FileInfo file, Object obj = null)
            {
                Item = new Item(file, obj);
            }

            private TreeNode AddChild(TreeNode node)
            {
                Children.Add(node);
                return node;
            }

            public TreeNode GetChild(string name)
            {
                Assert.IsFalse(string.IsNullOrEmpty(name));
                return Children.FirstOrDefault(c => string.Equals(c.Item?.Name, name));
            }

            public TreeNode GetOrAddChild(string name, long size = 0, Object obj = null)
            {
                var child = GetChild(name);
                return child ?? AddChild(new TreeNode(this, new Item(name, size, obj)));
            }

            public TreeNode AddChild()
            {
                return AddChild(new TreeNode(this, new Item()));
            }

            public TreeNode AddChild(string name, long size = 0, Object obj = null)
            {
                return AddChild(new TreeNode(this, new Item(name, size, obj)));
            }

            public TreeNode AddChild(string name, byte[] bytes, Object obj = null)
            {
                return AddChild(new TreeNode(this, new Item(name, bytes, obj)));
            }

            public TreeNode AddChild(FileInfo file, Object obj = null)
            {
                return AddChild(new TreeNode(this, new Item(file, obj)));
            }

            public void Update()
            {
                UpdateRecursive(this);
            }

            public static string ToJson(TreeNode node)
            {
                return JsonBackEnd.Persist(node);
            }

            public override string ToString()
            {
                return ToJson(this);
            }

            public static TreeNode FromJson(string json)
            {
                if (string.IsNullOrEmpty(json))
                    throw new ArgumentNullException(nameof(json));

                object nodeObject;
                Json.TryDeserializeObject(json, out nodeObject);
                var nodeDictionary = nodeObject as IDictionary<string, object>;
                if (nodeDictionary == null)
                    throw new NullReferenceException("nodeDictionary");

                var node = new TreeNode();
                FromDictionary(node, nodeDictionary);
                UpdateRecursive(node);
                return node;
            }

            private static void FromDictionary(TreeNode node, IDictionary<string, object> nodeDictionary)
            {
                var itemDictionary = Parser.GetValue(nodeDictionary, ItemProperty.Name) as IDictionary<string, object>;
                if (itemDictionary != null)
                {
                    node.Item = Item.FromDictionary(itemDictionary);
                }

                var childrenList = Parser.GetValue(nodeDictionary, ChildrenProperty.Name) as IList<object>;
                if (childrenList != null)
                {
                    foreach (IDictionary<string, object> childDictionary in childrenList)
                    {
                        var child = new TreeNode(node);
                        FromDictionary(child, childDictionary);
                        node.AddChild(child);
                    }
                }
            }

            private static void UpdateRecursive(TreeNode node)
            {
                // Compute children values
                long childrenSize = 0, childrenCompressedSize = 0;
                if (node.Children.Count > 0)
                {
                    foreach (var child in node.Children)
                    {
                        UpdateRecursive(child);
                        childrenSize += child.Item.Size;
                        childrenCompressedSize += child.Item.CompressedSize;
                    }
                }

                // Update item
                if (node.Item != null)
                {
                    node.Item.ChildrenSize = childrenSize;
                    node.Item.ChildrenCompressedSize = childrenCompressedSize;

                    // No size, fallback to children size
                    if (node.Item.Size == 0)
                    {
                        node.Item.Size = node.Item.ChildrenSize;
                    }

                    // No compressed size, fallback to compressed children size
                    if (node.Item.CompressedSize == 0)
                    {
                        // No compressed children size, fallback to size
                        if (node.Item.ChildrenCompressedSize == 0)
                        {
                            node.Item.CompressedSize = node.Item.Size;
                        }
                        else
                        {
                            node.Item.CompressedSize = node.Item.ChildrenCompressedSize;
                        }
                    }
                }
            }
        }

        public partial class Item
        {
            public long ChildrenSize { get; set; }
            public long ChildrenCompressedSize { get; set; }

            public Item(string name, long size, Object obj)
            {
                Name = name;
                Size = size;
                Object = obj;
            }

            public Item() :
                this(null, 0, null)
            {
            }

            public Item(string name, byte[] bytes, Object obj) :
                this(name, bytes?.Length ?? 0, obj)
            {
                if (bytes == null)
                {
                    throw new ArgumentNullException(nameof(bytes));
                }

                if (bytes.Length > 0)
                {
                    CompressedSize = GetCompressedSize(bytes);
                    if (CompressedSize == 0)
                    {
                        throw new Exception("GetCompressedSize(bytes)");
                    }
                }
            }

            public Item(FileInfo file, Object obj) :
                this(Persistence.GetPathRelativeToProjectPath(file.FullName), file?.Length ?? 0, obj)
            {
                if (file == null)
                {
                    throw new ArgumentNullException(nameof(file));
                }

                if (!file.Exists)
                {
                    throw new FileNotFoundException("file");
                }

                if (file.Exists && file.Length > 0)
                {
                    CompressedSize = GetCompressedSize(file);
                    if (CompressedSize == 0)
                    {
                        throw new Exception("GetCompressedSize(file)");
                    }
                }
            }

            public static Item FromDictionary(IDictionary<string, object> dictionary)
            {
                var item = new Item
                {
                    Name = Parser.GetValue<string>(dictionary, NameProperty.Name),
                    Size = Parser.ParseLong(Parser.GetValue(dictionary, SizeProperty.Name)),
                    CompressedSize = Parser.ParseLong(Parser.GetValue(dictionary, CompressedSizeProperty.Name)),
                    Object = Parser.ParseUnityObject(Parser.GetValue(dictionary, ObjectProperty.Name))
                };
                return item;
            }

            private class NullStream : Stream
            {
                private long m_Position;
                private long m_Length;

                public override bool CanRead => false;
                public override bool CanSeek => true;
                public override bool CanWrite => true;
                public override long Length => m_Length;

                public override long Position
                {
                    get { return m_Position; }
                    set { m_Position = value; }
                }

                public override void Flush()
                {
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    throw new NotImplementedException();
                }

                public override int ReadByte()
                {
                    throw new NotImplementedException();
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            m_Position = offset;
                            break;
                        case SeekOrigin.Current:
                            m_Position += offset;
                            break;
                        case SeekOrigin.End:
                            m_Position = m_Length + offset;
                            break;
                    }

                    return m_Position;
                }

                public override void SetLength(long value)
                {
                    m_Length = value;
                }

                public override string ToString()
                {
                    throw new NotImplementedException();
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    m_Length += count;
                    m_Position += count;
                }

                public override void WriteByte(byte value)
                {
                    m_Length++;
                    m_Position++;
                }
            }

            private static long GetStreamCompressedSize(Stream stream)
            {
                using (var nullStream = new NullStream())
                {
                    using (var compressionStream = new GZipStream(nullStream, CompressionMode.Compress, true))
                    {
                        // Copy to compression stream without allocating
                        int read;
                        while ((read = stream.Read(s_Buffer, 0, s_Buffer.Length)) != 0)
                        {
                            compressionStream.Write(s_Buffer, 0, read);
                        }
                    }

                    return nullStream.Length;
                }
            }

            private long GetCompressedSize(byte[] data)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                if (data.Length == 0)
                {
                    return 0;
                }

                using (var stream = new MemoryStream(data))
                {
                    return GetStreamCompressedSize(stream);
                }
            }

            private long GetCompressedSize(FileInfo file)
            {
                if (file == null)
                {
                    throw new ArgumentNullException(nameof(file));
                }

                if (!file.Exists)
                {
                    throw new FileNotFoundException("file");
                }

                if (file.Length == 0)
                {
                    return 0;
                }

                using (var stream = File.OpenRead(file.FullName))
                {
                    return GetStreamCompressedSize(stream);
                }
            }
        }
    }
}

