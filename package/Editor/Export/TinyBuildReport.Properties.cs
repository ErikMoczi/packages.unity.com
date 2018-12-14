
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyBuildReport : IPropertyContainer
    {
        private static ClassPropertyBag<TinyBuildReport> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyBuildReport>();
        }

        static TinyBuildReport()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }


        public partial class Item : IPropertyContainer
        {
            public static ValueClassProperty<Item, string> NameProperty { get; private set; }
            public static ValueClassProperty<Item, long> SizeProperty { get; private set; }
            public static ValueClassProperty<Item, long> CompressedSizeProperty { get; private set; }
            public static ValueClassProperty<Item, UnityEngine.Object> ObjectProperty { get; private set; }

            private static ClassPropertyBag<Item> s_PropertyBag { get; set; }

            public IPropertyBag PropertyBag => s_PropertyBag;
            public IVersionStorage VersionStorage => null;

            private static void InitializeProperties()
            {
                NameProperty = new ValueClassProperty<Item, string>(
                    "Name"
                    ,c => c.m_Name
                    ,(c, v) => c.m_Name = v
                );

                SizeProperty = new ValueClassProperty<Item, long>(
                    "Size"
                    ,c => c.m_Size
                    ,(c, v) => c.m_Size = v
                );

                CompressedSizeProperty = new ValueClassProperty<Item, long>(
                    "CompressedSize"
                    ,c => c.m_CompressedSize
                    ,(c, v) => c.m_CompressedSize = v
                );

                ObjectProperty = new ValueClassProperty<Item, UnityEngine.Object>(
                    "Object"
                    ,c => c.m_Object
                    ,(c, v) => c.m_Object = v
                );
            }

            /// <summary>
            /// Implement this partial method to initialize custom properties
            /// </summary>
            static partial void InitializeCustomProperties();

            private static void InitializePropertyBag()
            {
                s_PropertyBag = new ClassPropertyBag<Item>(
                    NameProperty,
                    SizeProperty,
                    CompressedSizeProperty,
                    ObjectProperty
                );
            }

            static Item()
            {
                InitializeProperties();
                InitializeCustomProperties();
                InitializePropertyBag();
            }

            private string m_Name;
            private long m_Size;
            private long m_CompressedSize;
            private UnityEngine.Object m_Object;

            public string Name
            {
                get { return NameProperty.GetValue(this); }
                set { NameProperty.SetValue(this, value); }
            }

            public long Size
            {
                get { return SizeProperty.GetValue(this); }
                set { SizeProperty.SetValue(this, value); }
            }

            public long CompressedSize
            {
                get { return CompressedSizeProperty.GetValue(this); }
                set { CompressedSizeProperty.SetValue(this, value); }
            }

            public UnityEngine.Object Object
            {
                get { return ObjectProperty.GetValue(this); }
                set { ObjectProperty.SetValue(this, value); }
            }
        }

        public partial class TreeNode : IPropertyContainer
        {
            public static ClassValueClassProperty<TreeNode, Item> ItemProperty { get; private set; }
            public static ClassListClassProperty<TreeNode, TreeNode> ChildrenProperty { get; private set; }

            private static ClassPropertyBag<TreeNode> s_PropertyBag { get; set; }

            public IPropertyBag PropertyBag => s_PropertyBag;
            public IVersionStorage VersionStorage => null;

            private static void InitializeProperties()
            {
                ItemProperty = new ClassValueClassProperty<TreeNode, Item>(
                    "Item"
                    ,c => c.m_Item
                    ,(c, v) => c.m_Item = v
                );

                ChildrenProperty = new ClassListClassProperty<TreeNode, TreeNode>(
                    "Children"
                    ,c => c.m_Children
                    ,c => new TreeNode()
                );
            }

            /// <summary>
            /// Implement this partial method to initialize custom properties
            /// </summary>
            static partial void InitializeCustomProperties();

            private static void InitializePropertyBag()
            {
                s_PropertyBag = new ClassPropertyBag<TreeNode>(
                    ItemProperty,
                    ChildrenProperty
                );
            }

            static TreeNode()
            {
                InitializeProperties();
                InitializeCustomProperties();
                InitializePropertyBag();
            }

            private Item m_Item;
            private readonly List<TreeNode> m_Children = new List<TreeNode>();

            public Item Item
            {
                get { return ItemProperty.GetValue(this); }
                set { ItemProperty.SetValue(this, value); }
            }

            public PropertyList<TreeNode, TreeNode> Children => new PropertyList<TreeNode, TreeNode>(ChildrenProperty, this);
        }    }
}
