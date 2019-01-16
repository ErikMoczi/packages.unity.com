using System;
using System.Xml;
using System.Collections.Generic;
using Unity.MemoryProfiler.Editor.Debuging;
using Unity.MemoryProfiler.Editor.Database.Operation;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    internal class ViewTable : ExpandTable
    {
        public ViewSchema ViewSchema;
        public Schema BaseSchema;


        public Builder.Node node;

        // if null, all child nodes are valid
        public int[] ValidChildNodeIndices;

        // Select statement not related to data entries.
        // is null when there are no local select.
        public SelectSet localSelectSet;

        // Select statement that drive the data entries when the data type is "Select"
        public SelectSet dataSelectSet;

        // context when parsing expressions and doing name look-up
        // keep information such as which parent's row this table originate from so expressions are
        // evaluated using that row's data.
        public Operation.ExpressionParsingContext expressionParsingContext;
        public Operation.ExpressionParsingContext parentExpressionParsingContext;

        // [Figure.1] Example structure of nodes, SelectSets and ExpressionParsingContext (EPC)
        //=============================================================================================================================================================
        // Node                 | SelectSet                  | Node 0     | Node 0_0      | Node 0_0_0    | Node 0_0_1    | Node 0_1      | Node 0_1_0    | Node 0_1_1
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        //   + Node 0
        //   |                    localSelectSet               EPC.row=-1   EPC.row=-1      EPC.row=-1      EPC.row-1       EPC.row=-1      EPC.row=-1      EPC.row=-1
        //   |                                                  /|\          /|\             /|\             /|\             /|\             /|\             /|\
        //   |                                                   |            |               |               |               |               |               |
        //   |                    dataSelectSet                EPC.row=-1   EPC.row=0       EPC.row=0       EPC.row=0       EPC.row=1       EPC.row=1       EPC.row=1
        //   |                                                               /|\             /|\             /|\             /|\             /|\             /|\
        //   |-+ Node 0_0                                                     |               |               |               |               |               |
        //   | |                  localSelectSet                            EPC.row=-1      EPC.row=-1      EPC.row=-1        |               |               |
        //   | |                                                             /|\             /|\             /|\              |               |               |
        //   | |                                                              |               |               |               |               |               |
        //   | |                  dataSelectSet                             EPC.row=-1      EPC.row=0       EPC.row=1         |               |               |
        //   | |                                                                             /|\             /|\              |               |               |
        //   | |- Node 0_0_0                                                                  |               |               |               |               |
        //   | |                  localSelectSet                                            EPC.row=-1        |               |               |               |
        //   | |                                                                             /|\              |               |               |               |
        //   | |                                                                              |               |               |               |               |
        //   | |                  dataSelectSet                                             EPC.row=-1        |               |               |               |
        //   | |                                                                                              |               |               |               |
        //   | |- Node 0_0_1                                                                                  |               |               |               |
        //   |                                                                                                |               |               |               |
        //   |                    localSelectSet                                                            EPC.row=-1        |               |               |
        //   |                                                                                               /|\              |               |               |
        //   |                                                                                                |               |               |               |
        //   |                    dataSelectSet                                                             EPC.row=-1        |               |               |
        //   |                                                                                                                |               |               |
        //   |-+ Node 0_1                                                                                                     |               |               |
        //     |                  localSelectSet                                                                            EPC.row=-1      EPC.row=-1      EPC.row=-1
        //     |                                                                                                             /|\             /|\             /|\
        //     |                                                                                                              |               |               |
        //     |                  dataSelectSet                                                                             EPC.row=-1      EPC.row=0       EPC.row=1
        //     |                                                                                                                             /|\             /|\
        //     |- Node 0_1_0                                                                                                                  |               |
        //     |                  localSelectSet                                                                                              |               |
        //     |                                                                                                                            EPC.row=-1        |
        //     |                                                                                                                             /|\              |
        //     |                  dataSelectSet                                                                                               |               |
        //     |                                                                                                                            EPC.row=-1        |
        //     |- Node 0_1_1                                                                                                                                  |
        //                        localSelectSet                                                                                                              |
        //                                                                                                                                                  EPC.row=-1
        //                                                                                                                                                   /|\
        //                        dataSelectSet                                                                                                               |
        //                                                                                                                                                  EPC.row=-1
        //=============================================================================================================================================================
        //
        //  Using this structure, name look-up are prioritized in this order:
        //==========================================================================
        //    Node         |  Select Set               |  Data used from select set
        //--------------------------------------------------------------------------
        //    Node 0
        //                    Node0.dataSelectSet         all
        //                    Node0.localSelectSet        all
        //    Node 0_0
        //                    Node0_0.dataSelectSet       all
        //                    Node0_0.localSelectSet      all
        //                    Node0.dataSelectSet         row 0
        //                    Node0.localSelectSet        all
        //    Node 0_1:
        //                    Node0_1.dataSelectSet       all
        //                    Node0_1.localSelectSet      all
        //                    Node0.dataSelectSet         row 1
        //                    Node0.localSelectSet        all
        //    Node 0_0_0
        //                    Node0_0_0.dataSelectSet     all
        //                    Node0_0_0.localSelectSet    all
        //                    Node0_0.dataSelectSet       row 0
        //                    Node0_0.localSelectSet      all
        //                    Node0.dataSelectSet         row 0
        //                    Node0.localSelectSet        all
        //    Node 0_0_1
        //                    Node0_0_1.dataSelectSet     all
        //                    Node0_0_1.localSelectSet    all
        //                    Node0_0.dataSelectSet       row 1
        //                    Node0_0.localSelectSet      all
        //                    Node0.dataSelectSet         row 0
        //                    Node0.localSelectSet        all
        //    Node 0_1_0
        //                    Node0_1_0.dataSelectSet     all
        //                    Node0_1_0.localSelectSet    all
        //                    Node0_1.dataSelectSet       row 0
        //                    Node0_1.localSelectSet      all
        //                    Node0.dataSelectSet         row 1
        //                    Node0.localSelectSet        all
        //    Node 0_1_1
        //                    Node0_1_1.dataSelectSet     all
        //                    Node0_1_1.localSelectSet    all
        //                    Node0_1.dataSelectSet       row 1
        //                    Node0_1.localSelectSet      all
        //                    Node0.dataSelectSet         row 1
        //                    Node0.localSelectSet        all
        //==========================================================================


        public ViewTable(ViewSchema viewSchema, Schema baseSchema)
            : base(viewSchema)
        {
            this.ViewSchema = viewSchema;
            this.BaseSchema = baseSchema;
        }

        public override string GetName() { return node.GetFullName(); }
        public override string GetDisplayName() { return node.GetFullName(); }


        private Table[] m_GroupTableCache;
        public override IUpdater BeginUpdate()
        {
            bool builtChildren = BuildChildren(); // must be done before base.BeginUpdate to initialize groups
            var updater = base.BeginUpdate();
            if(updater == null && builtChildren)
            {
                return new DefaultDirtyUpdater(this);
            }
            return updater;
        }

        public override bool ComputeRowCount()
        {
            return BuildChildren();
        }
        private bool BuildChildren()
        {
            if (!IsGroupInitialized())
            {
                var childCount = node.data.GetChildCount(this.ViewSchema, this, parentExpressionParsingContext);
                InitGroup(childCount);
                m_GroupTableCache = new Table[GetGroupCount()];
                return true;
            }
            return false;
        }

        public long GetNodeChildCount()
        {
            if (ValidChildNodeIndices != null)
            {
                return ValidChildNodeIndices.LongLength;
            }
            return node.data.child.Count;
        }

        public long GroupIndexToNodeChildIndex(long groupIndex)
        {
            if (ValidChildNodeIndices != null)
            {
                return ValidChildNodeIndices[groupIndex];
            }
            return groupIndex;
        }

        public bool IsGroupIndexInRange(long groupIndex)
        {
            if (ValidChildNodeIndices != null)
            {
                return DebugUtility.IsInValidRange(ValidChildNodeIndices, groupIndex);
            }
            return DebugUtility.IsInValidRange(node.data.child, groupIndex);
        }

        public override Table CreateGroupTable(long groupIndex)
        {
            BuildChildren();

            if (m_GroupTableCache[(int)groupIndex] != null) return m_GroupTableCache[(int)groupIndex];

            // Create expression parsing context with a fix row so the child can refer to this group index data and local select sets.
            Operation.ExpressionParsingContext curParent = parentExpressionParsingContext;
            if (localSelectSet != null)
            {
                curParent = new Operation.ExpressionParsingContext(curParent, localSelectSet);
            }
            if (dataSelectSet != null)
            {
                curParent = new Operation.ExpressionParsingContext(curParent, dataSelectSet);
                curParent.fixedRow = groupIndex;
            }
            int childIndexToBuild;
            switch (node.data.type)
            {
                case Builder.Node.Data.DataType.Node:
                    childIndexToBuild = (int)GroupIndexToNodeChildIndex(groupIndex);
                    break;
                case Builder.Node.Data.DataType.Select:
                    if (node.data.child.Count >= 1)
                    {
                        childIndexToBuild = 0;
                    }
                    else
                    {
                        return null;
                    }
                    break;
                case Builder.Node.Data.DataType.NoData:
                default:
                    return null;
            }

            m_GroupTableCache[(int)groupIndex] = node.data.child[childIndexToBuild].Build(this, groupIndex, ViewSchema, BaseSchema, curParent);

            // create default filter
            if (node.data.child[childIndexToBuild].defaultFilter != null)
            {
                m_GroupTableCache[(int)groupIndex] = node.data.child[childIndexToBuild].defaultFilter.CreateFilter(m_GroupTableCache[(int)groupIndex]);
            }

            return m_GroupTableCache[(int)groupIndex];
        }

        public override bool IsGroupExpandable(long groupIndex, int col)
        {
            switch (node.data.type)
            {
                case Builder.Node.Data.DataType.Node:
                    if (!IsGroupIndexInRange(groupIndex))
                    {
                        return false;
                    }
                    return node.data.child[(int)GroupIndexToNodeChildIndex(groupIndex)].data != null;
                case Builder.Node.Data.DataType.Select:
                    return node.data.child.Count > 0;
                case Builder.Node.Data.DataType.NoData:
                default:
                    return false;
            }
        }

        public override bool IsColumnExpandable(int col)
        {
            return col == 0;
        }

        public class Builder
        {
            public class Node
            {
                public Node parent;
                public string name;

                public Operation.MetaExpComparison condition; //Node will be present only if condition returns 'true' bool value

                public SelectSet.Builder localSelectSet = new SelectSet.Builder();

                // these column are value for the node's row in it's parent view table
                // or declarations of column that will be filled under the data class.
                public System.Collections.Generic.List<ViewColumn.Builder> column = new System.Collections.Generic.List<ViewColumn.Builder>();

                public Database.Operation.Filter.Filter defaultFilter;
                public Database.Operation.Filter.Sort defaultAllLevelSortFilter;

                public bool EvaluateCondition(ViewSchema vs, ViewTable parentViewTable, Operation.ExpressionParsingContext expressionParsingContext)
                {
                    if (condition == null) return true;
                    using (ScopeDebugContext.Func(() => { return "EvaluateCondition on Node '" + GetFullName() + "'"; }))
                    {
                        var option = new Operation.Expression.ParseIdentifierOption(vs, parentViewTable, true, true, null, expressionParsingContext);
                        option.formatError = (string s, Operation.Expression.ParseIdentifierOption opt) =>
                            {
                                string str = "Error while evaluating node condition.";
                                if (vs != null) str += " schema '" + vs.name + "'";
                                if (parentViewTable != null) str += " view table '" + parentViewTable.GetName() + "'";
                                return str + " : " + s;
                            };
                        var resolvedCondition = condition.Build(option);
                        if (resolvedCondition == null) return false;
                        return resolvedCondition.GetValue(0);
                    }
                }

                public class Data
                {
                    public enum DataType
                    {
                        NoData,
                        Node,
                        Select,
                    }
                    public DataType type = DataType.NoData;
                    public SelectSet.Builder dataSelectSet = new SelectSet.Builder();
                    public System.Collections.Generic.List<ViewColumn.Builder> column = new System.Collections.Generic.List<ViewColumn.Builder>();
                    public System.Collections.Generic.List<Node> child = new System.Collections.Generic.List<Node>();

                    public static Data LoadFromXML(Node node, XmlElement root)
                    {
                        Data data = new Data();
                        string strType = root.GetAttribute("type");
                        if (string.IsNullOrEmpty(strType))
                        {
                            data.type = DataType.Select;
                        }
                        else
                        {
                            switch (strType.ToLower())
                            {
                                case "node":
                                    data.type = DataType.Node;
                                    break;
                                case "select":
                                    data.type = DataType.Select;
                                    break;
                            }
                        }


                        foreach (XmlNode xNode in root.ChildNodes)
                        {
                            if (xNode.NodeType == XmlNodeType.Element)
                            {
                                XmlElement e = (XmlElement)xNode;
                                switch (e.Name)
                                {
                                    case "Column":
                                    {
                                        var c = ViewColumn.Builder.LoadFromXML(e);
                                        if (c != null)
                                        {
                                            data.column.Add(c);
                                        }
                                        break;
                                    }
                                    case "Node":
                                    {
                                        var childNode = Node.LoadFromXML(node, e);
                                        if (childNode != null) data.child.Add(childNode);
                                        break;
                                    }
                                    case "SelectSet":
                                    {
                                        data.dataSelectSet = SelectSet.Builder.LoadFromXML(e);
                                        break;
                                    }
                                    default:
                                        DebugUtility.LogInvalidXmlChild(root, e);
                                        break;
                                }
                            }
                        }
                        return data;
                    }

                    public long GetChildCount(ViewSchema vs, ViewTable vTable, Operation.ExpressionParsingContext expressionParsingContext)
                    {
                        switch (type)
                        {
                            case DataType.Node:
                                if (vTable.ValidChildNodeIndices != null)
                                {
                                    return vTable.ValidChildNodeIndices.LongLength;
                                }
                                else
                                {
                                    return child.Count;
                                }
                            case DataType.Select:
                                if (vTable.dataSelectSet.IsManyToMany())
                                {
                                    return 0;
                                }
                                else
                                {
                                    return vTable.dataSelectSet.GetMainRowCount();
                                }
                            case DataType.NoData:
                            default:
                                return 0;
                        }
                    }

                    public void Build(Node node, ViewTable vTable, ViewTable parent, ViewSchema vs, Database.Schema baseSchema, Operation.ExpressionParsingContext parentExpressionParsingContext, MetaTable metaTable)
                    {
                        // build selects
                        vTable.dataSelectSet = dataSelectSet.Build(vTable, vs, baseSchema);
                        if (vTable.dataSelectSet != null)
                        {
                            // add the select set to the expression parsing context hierarchy. see [Figure.1]
                            vTable.expressionParsingContext = new Operation.ExpressionParsingContext(vTable.expressionParsingContext, vTable.dataSelectSet);
                        }

                        // build columns
                        switch (type)
                        {
                            case Data.DataType.Node:
                                // these column are declarations
                                foreach (var colb in column)
                                {
                                    MetaColumn metaColumn = metaTable.GetColumnByName(colb.name);
                                    bool hadMetaColumn = metaColumn != null;

                                    colb.BuildOrUpdateDeclaration(ref metaColumn);

                                    // add the metacolum to the metatable if it just got created
                                    if (!hadMetaColumn)
                                    {
                                        metaTable.AddColumn(metaColumn);
                                    }
                                }

                                // for node type we need to build all child node right away as they defines the entries in this viewtable
                                var validChildNodeIndices = new List<int>();
                                int iValidChild = 0;
                                for (int iChild = 0; iChild != child.Count; ++iChild)
                                {
                                    var c = child[iChild];
                                    if (c.EvaluateCondition(vs, vTable, vTable.expressionParsingContext))
                                    {
                                        validChildNodeIndices.Add(iChild);
                                        c.BuildAsNode(vTable, (long)iValidChild, vs, baseSchema, vTable.expressionParsingContext);
                                        ++iValidChild;
                                    }
                                }
                                if (iValidChild != child.Count)
                                {
                                    vTable.ValidChildNodeIndices = validChildNodeIndices.ToArray();
                                }
                                else
                                {
                                    vTable.ValidChildNodeIndices = null;
                                }
                                break;
                            case DataType.Select:
                                // these columns are instances of ViewColumn. They have the result of select statement as entries
                                foreach (var colb in column)
                                {
                                    MetaColumn metaColumn = metaTable.GetColumnByName(colb.name);
                                    bool hadMetaColumn = metaColumn != null;

                                    var newColumn = colb.Build(node, vs, baseSchema, vTable, vTable.expressionParsingContext, ref metaColumn);

                                    // add the metacolum to the metatable if it just got created
                                    if (!hadMetaColumn)
                                    {
                                        metaTable.AddColumn(metaColumn);
                                    }

                                    vTable.SetColumn(metaColumn, newColumn.GetColumn());
                                }
                                break;
                        }
                    }
                }
                public Data data;


                public Node(Node parent)
                {
                    this.parent = parent;
                }

                public string GetFullName()
                {
                    if (parent != null)
                    {
                        return parent.GetFullName() + "." + name;
                    }
                    return name;
                }

                private string FormatErrorContextInfo(ViewSchema vs)
                {
                    string fullName = GetFullName();
                    if (vs != null) return "Error while building schema '" + vs.name + "' view table '" + fullName + "' ";
                    return "Error while building view table '" + fullName + "' ";
                }

                private MetaTable BuildOrGetMetaTable(ViewTable parentVTable, ViewTable buildingVTable)
                {
                    if (parentVTable == null)
                    {
                        //no parent view table mean we are building the root table and must create a meta table.
                        if (buildingVTable == null)
                        {
                            //we must be building a view table when building the root.
                            DebugUtility.LogError("Failed to build the root view table.");
                            MemoryProfilerAnalytics.AddMetaDatatoEvent<MemoryProfilerAnalytics.LoadViewXMLEvent>(8);
                        }

                        Database.MetaTable metaTable = new Database.MetaTable();
                        metaTable.name = name;
                        metaTable.displayName = name;
                        metaTable.defaultFilter = defaultFilter;
                        metaTable.defaultAllLevelSortFilter = defaultAllLevelSortFilter;
                        buildingVTable.m_Meta = metaTable;
                        return metaTable;
                    }
                    else
                    {
                        // if has a parent, use parent's meta table
                        if (buildingVTable != null)
                        {
                            buildingVTable.m_Meta = parentVTable.m_Meta;
                        }
                        return parentVTable.m_Meta;
                    }
                }

                private bool HasColumn(string name)
                {
                    foreach (var c in column)
                    {
                        if (name == c.name)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                // When building as node, the columns are interpreted as entries in the parent's view table.
                public void BuildAsNode(ViewTable parentViewTable, long row, ViewSchema vs, Database.Schema baseSchema, Operation.ExpressionParsingContext parentExpressionParsingContext)
                {
                    MetaTable metaTable = BuildOrGetMetaTable(parentViewTable, null);
                    if (localSelectSet.select.Count > 0)
                    {
                        DebugUtility.LogError("Node '" + GetFullName() + " ' cannot have any local select statement when the parent data type is 'node'. Ignoring all selects.");
                    }

                    //build columns
                    foreach (var colb in column)
                    {
                        MetaColumn metaColumn = metaTable.GetColumnByName(colb.name);
                        bool hadMetaColumn = metaColumn != null;

                        colb.BuildNodeValue(this, row, vs, baseSchema, parentViewTable, parentExpressionParsingContext, ref metaColumn);

                        // add the metacolum to the metatable if it just got created
                        if (!hadMetaColumn)
                        {
                            metaTable.AddColumn(metaColumn);
                        }
                    }

                    //Build missing column
                    for (int i = 0; i != metaTable.GetColumnCount(); ++i)
                    {
                        var metaColumn = metaTable.GetColumnByIndex(i);
                        if (!HasColumn(metaColumn.Name))
                        {
                            ViewColumn.Builder.BuildNodeValueDefault(this, row, vs, baseSchema, parentViewTable, parentExpressionParsingContext, metaColumn);
                        }
                    }
                }

                public ViewTable Build(ViewTable parent, long row, ViewSchema vs, Database.Schema baseSchema, Operation.ExpressionParsingContext parentExpressionParsingContext)
                {
                    // Check for usage error from data.
                    if (parent == null && String.IsNullOrEmpty(name))
                    {
                        DebugUtility.LogError(FormatErrorContextInfo(vs) + ": Table need a name");
                        return null;
                    }
                    using (ScopeDebugContext.Func(() => { return "View '" + GetFullName() + "'"; }))
                    {
                        // Check for usage error from code.
                        DebugUtility.CheckCondition(parent == null || (parent.node == this.parent), FormatErrorContextInfo(vs) + ": Parent ViewTable must points to the node's parent while building child node's ViewTable.");

                        if (data == null)
                        {
                            //no data
                            return null;
                        }

                        ViewTable vTable = new ViewTable(vs, baseSchema);
                        vTable.node = this;
                        vTable.parentExpressionParsingContext = parentExpressionParsingContext;
                        vTable.expressionParsingContext = parentExpressionParsingContext;

                        // If has local select set, create it and add it to the expression parsing context hierarchy. see [Figure.1]
                        vTable.localSelectSet = localSelectSet.Build(vTable, vs, baseSchema);
                        if (vTable.localSelectSet != null)
                        {
                            vTable.expressionParsingContext = new Operation.ExpressionParsingContext(vTable.expressionParsingContext, vTable.localSelectSet);
                        }

                        MetaTable metaTable = BuildOrGetMetaTable(parent, vTable);

                        //declare columns
                        foreach (var colb in column)
                        {
                            MetaColumn metaColumn = metaTable.GetColumnByName(colb.name);
                            bool hadMetaColumn = metaColumn != null;

                            colb.BuildOrUpdateDeclaration(ref metaColumn);

                            // add the metacolum to the metatable if it just got created
                            if (!hadMetaColumn)
                            {
                                metaTable.AddColumn(metaColumn);
                            }
                        }

                        data.Build(this, vTable, parent, vs, baseSchema, parentExpressionParsingContext, metaTable);


                        //Build missing column with default behavior
                        for (int i = 0; i != metaTable.GetColumnCount(); ++i)
                        {
                            var metaColumn = metaTable.GetColumnByIndex(i);
                            var column = vTable.GetColumnByIndex(i);

                            if (column == null)
                            {
                                if (metaColumn.DefaultMergeAlgorithm != null)
                                {
                                    //when we have a merge algorithm, set the entries as the result of each group's merge value.
                                    column = ViewColumn.Builder.BuildColumnNodeMerge(vTable, metaColumn, parentExpressionParsingContext);
                                }

                                vTable.SetColumn(metaColumn, column);
                            }
                        }

                        if (data.type == Data.DataType.Select && vTable.dataSelectSet.IsManyToMany())
                        {
                            DebugUtility.LogError("Cannot build the view table '" + vTable.GetName() + "' using a many-to-many select statement. Specify a row value for your select statement where condition(s).");
                            MemoryProfilerAnalytics.AddMetaDatatoEvent<MemoryProfilerAnalytics.LoadViewXMLEvent>(7);
                        }

                        return vTable;
                    }
                }

                public static Node LoadFromXML(Node parent, XmlElement root)
                {
                    Node node = new Node(parent);
                    node.name = root.GetAttribute("name");
                    using (ScopeDebugContext.Func(() => { return "Node '" + node.name + "'"; }))
                    {
                        foreach (XmlNode xNode in root.ChildNodes)
                        {
                            if (xNode.NodeType == XmlNodeType.Element)
                            {
                                XmlElement e = (XmlElement)xNode;
                                switch (e.Name)
                                {
                                    case "Column":
                                        var c = ViewColumn.Builder.LoadFromXML(e);
                                        if (c != null)
                                        {
                                            node.column.Add(c);
                                        }
                                        break;
                                    case "Data":
                                        node.data = Data.LoadFromXML(node, e);
                                        break;
                                    case "Filter":
                                        LoadFilterFromXML(node, e);
                                        break;
                                    case "SelectSet":
                                        node.localSelectSet = SelectSet.Builder.LoadFromXML(e);
                                        break;
                                    case "Condition":
                                        node.condition = Operation.MetaExpComparison.LoadFromXML(e);
                                        break;
                                    default:
                                        DebugUtility.LogInvalidXmlChild(root, e);
                                        break;
                                }
                            }
                        }
                        return node;
                    }
                }

                public static Operation.Filter.Sort LoadSortFilterFromXML(Node node, XmlElement root)
                {
                    Database.Operation.Filter.Sort f = new Operation.Filter.Sort();
                    // if the element has children, do not process it as a sort level.
                    if (root.ChildNodes.Count == 0)
                    {
                        string colName;
                        string strOrder;
                        if (DebugUtility.TryGetMandatoryXmlAttribute(root, "column", out colName)
                            && DebugUtility.TryGetMandatoryXmlAttribute(root, "order", out strOrder))
                        {
                            SortOrder order = SortOrderString.StringToSortOrder(strOrder, SortOrder.Ascending);
                            f.SortLevel.Add(new Operation.Filter.Sort.LevelByName(colName, order));
                        }
                    }

                    //Process children as sort level
                    foreach (XmlNode xNode in root.ChildNodes)
                    {
                        if (xNode.NodeType == XmlNodeType.Element)
                        {
                            XmlElement e = (XmlElement)xNode;
                            if (e.Name == "Level")
                            {
                                string colNameL;
                                string strOrderL;
                                if (DebugUtility.TryGetMandatoryXmlAttribute(e, "column", out colNameL)
                                    && DebugUtility.TryGetMandatoryXmlAttribute(e, "order", out strOrderL))
                                {
                                    var orderL = SortOrderString.StringToSortOrder(strOrderL, SortOrder.Ascending);
                                    f.SortLevel.Add(new Operation.Filter.Sort.LevelByName(colNameL, orderL));
                                }
                            }
                            else
                            {
                                DebugUtility.LogInvalidXmlChild(root, e);
                            }
                        }
                    }
                    if (f.SortLevel.Count == 0) return null;
                    return f;
                }

                public static Operation.Filter.Group LoadGroupFilterFromXML(Node node, XmlElement root)
                {
                    string colName;
                    if (DebugUtility.TryGetMandatoryXmlAttribute(root, "column", out colName))
                    {
                        var order = SortOrderString.StringToSortOrder(root.GetAttribute("order"), SortOrder.Ascending);
                        Operation.Filter.Group g = new Operation.Filter.GroupByColumnName(colName, order);
                        g.SubGroupFilter = LoadSubFilterFromXML(node, root);
                        return g;
                    }
                    return null;
                }

                public static Operation.Filter.Filter LoadSubFilterFromXML(Node node, XmlElement root)
                {
                    Operation.Filter.Multi multi = null;
                    Operation.Filter.Filter lastFilter = null;
                    Operation.Filter.Sort sortFilter = null;
                    foreach (XmlNode xNode in root.ChildNodes)
                    {
                        if (xNode.NodeType == XmlNodeType.Element)
                        {
                            XmlElement e = (XmlElement)xNode;
                            Operation.Filter.Filter filter = null;
                            if (e.Name == "Group")
                            {
                                filter = LoadGroupFilterFromXML(node, e);
                            }
                            else if (e.Name == "Sort")
                            {
                                sortFilter = LoadSortFilterFromXML(node, e);
                                if (node.defaultAllLevelSortFilter != null)
                                {
                                    Operation.Filter.DefaultSort ds = new Operation.Filter.DefaultSort(node.defaultAllLevelSortFilter, sortFilter);
                                    filter = ds;
                                }
                                else
                                {
                                    filter = sortFilter;
                                }
                            }
                            else if (e.Name == "DefaultSort")
                            {
                                //skip this element as it is processed by LoadFilterFromXML
                            }
                            else
                            {
                                DebugUtility.LogInvalidXmlChild(root, e);
                            }

                            if (lastFilter != null)
                            {
                                if (multi == null) multi = new Operation.Filter.Multi();
                                multi.filters.Add(lastFilter);
                            }
                            lastFilter = filter;
                        }
                    }

                    //add all level sort filter if we haven't already
                    if (sortFilter == null && node.defaultAllLevelSortFilter != null)
                    {
                        Operation.Filter.DefaultSort ds = new Operation.Filter.DefaultSort(node.defaultAllLevelSortFilter, null);

                        //must use the multi filter if we have a lastFilter or already have a multi filter.
                        //we could have a multi filter and still have lastFilter == null if the lastfilter creation failed.
                        if (lastFilter != null || multi != null)
                        {
                            if (multi == null) multi = new Operation.Filter.Multi();
                            if (lastFilter != null) multi.filters.Add(lastFilter);
                            multi.filters.Add(ds);
                            if (multi.filters.Count > 1) return multi;
                        }
                        return ds;
                    }
                    else
                    {
                        //we have a sort filter already or we do not have a all-level sort filter
                        if (multi != null)
                        {
                            if (lastFilter != null) multi.filters.Add(lastFilter);
                            if (multi.filters.Count > 1) return multi;
                        }
                        return lastFilter;
                    }
                }

                public static void LoadFilterFromXML(Node node, XmlElement root)
                {
                    foreach (XmlNode xNode in root.ChildNodes)
                    {
                        if (xNode.NodeType == XmlNodeType.Element)
                        {
                            XmlElement e = (XmlElement)xNode;
                            if (e.Name == "DefaultSort")
                            {
                                node.defaultAllLevelSortFilter = LoadSortFilterFromXML(node, e);
                            }
                        }
                    }
                    if (node.defaultAllLevelSortFilter == null)
                    {
                        if (node.parent != null)
                        {
                            node.defaultAllLevelSortFilter = node.parent.defaultAllLevelSortFilter;
                        }
                        else
                        {
                            //create an empty sort filter so it can be used by the UI
                            node.defaultAllLevelSortFilter = new Operation.Filter.Sort();
                        }
                    }
                    node.defaultFilter = LoadSubFilterFromXML(node, root);
                }
            }

            public Node rootNode;

            public ViewTable Build(ViewSchema vs, Schema baseSchema)
            {
                return rootNode.Build(null, 0, vs, baseSchema, null);
            }

            public static Builder LoadFromXML(XmlElement root)
            {
                Builder b = new Builder();

                b.rootNode = Node.LoadFromXML(null, root);

                return b;
            }
        }
    }
}
