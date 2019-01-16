using System;
using System.Xml;
using Unity.MemoryProfiler.Editor.Debuging;

namespace Unity.MemoryProfiler.Editor.Database.Operation
{
    // Create a hierarchy of contexts used when parsing identifier in any expression
    internal class ExpressionParsingContext
    {
        public ExpressionParsingContext parent;
        public View.SelectSet selectSet;
        public ExpressionParsingContext(ExpressionParsingContext parent, View.SelectSet selectSet)
        {
            this.parent = parent;
            this.selectSet = selectSet;
        }

        public long fixedRow = -1;

        public bool TryGetSelect(string name, out ExpressionParsingContext context, out View.Select select)
        {
            if (!selectSet.TryGetSelect(name, out select))
            {
                if (parent != null) return parent.TryGetSelect(name, out context, out select);
                context = null;
                return false;
            }
            context = this;
            return true;
        }
    }

    internal abstract class Expression
    {
        public Type type;
        public abstract string GetValueString(long row);
        public abstract IComparable GetComparableValue(long row);
        public abstract bool HasMultipleRow();
        public abstract long RowCount();

#if MEMPROFILER_DEBUG_INFO
        public abstract string GetDebugString(long row);
#endif

        // Meta Expression are unresolved expression that must be parsed using ParseIdentifier to create an Expression
        internal class MetaExpression
        {
            public MetaExpression() {}
            public MetaExpression(string value, bool valueIsLiteral)
            {
                this.valueIsLiteral = valueIsLiteral;
                this.value = value;
            }

            // Can be a const value or an identifier. Identifier has the format "selectName.columnName"
            public string value;

            // force a fixed row when evaluating the value. Will collapse a Many-to-Many select to a Many-to-One when used on where clauses
            public long row = -1;

            // Value used when the value cannot be evaluated without errors
            public bool hasDefaultValue;
            public string valueDefault;

            // when false: value is parsed for identifiers
            // when true : value is used without parsing for identifiers. Will still interpret numbers as int or float
            public bool valueIsLiteral;

            // force the expression to be a specific type. If null, the type will be inferred from the value.
            public Type type;

            // Data breakpoint value will trigger a breakpoint if a debugger is attached. valueDataBreakpoint can be an identifier
            public bool hasDataBreakpointValue;
            public bool dataBreakpointValueIsLiteral;
            public string valueDataBreakpoint;

            public static MetaExpression LoadFromXML(XmlElement root)
            {
                MetaExpression metaExpression = new MetaExpression();
                metaExpression.value = root.GetAttribute("value");
                var literal = root.GetAttribute("literal");
                if (!String.IsNullOrEmpty(literal))
                {
                    metaExpression.valueIsLiteral = bool.Parse(literal);
                }
                var defaultValue = root.GetAttribute("default");
                if (!String.IsNullOrEmpty(defaultValue))
                {
                    metaExpression.valueDefault = defaultValue;
                    metaExpression.hasDefaultValue = true;
                }
                var breakOn = root.GetAttribute("breakOn");
                if (!String.IsNullOrEmpty(breakOn))
                {
                    metaExpression.valueDataBreakpoint = breakOn;
                    metaExpression.hasDataBreakpointValue = true;
                    var breakOnLiteral = root.GetAttribute("breakOnLiteral");
                    if (!String.IsNullOrEmpty(literal))
                    {
                        metaExpression.dataBreakpointValueIsLiteral = bool.Parse(breakOnLiteral);
                    }
                }
                string strRow = root.GetAttribute("row");
                if (!long.TryParse(strRow, out metaExpression.row))
                {
                    metaExpression.row = -1;
                }
                string strType = root.GetAttribute("type");
                if (!string.IsNullOrEmpty(strType))
                {
                    switch (strType)
                    {
                        case "bool": metaExpression.type = typeof(bool); break;
                        case "double": metaExpression.type = typeof(double); break;
                        case "float": metaExpression.type = typeof(float); break;
                        case "int": metaExpression.type = typeof(int); break;
                        case "long": metaExpression.type = typeof(long); break;
                        case "short": metaExpression.type = typeof(short); break;
                        case "uint": metaExpression.type = typeof(uint); break;
                        case "ulong": metaExpression.type = typeof(ulong); break;
                        case "ushort": metaExpression.type = typeof(ushort); break;
                        case "string": metaExpression.type = typeof(string); break;
                        case "DiffResult": metaExpression.type = typeof(DiffTable.DiffResult); break;
                        default:
                            metaExpression.type = null;
                            break;
                    }
                }
                else
                {
                    metaExpression.type = null;
                }
                return metaExpression;
            }
        }

        internal class ParseIdentifierOption
        {
            public View.ViewSchema Schema;

            // the identifier can reference to this table either with the "tableName.columnName" format or the "columnName" format.
            public Table identifierColumn_table;

            // provide a way to add contextual information to parsing errors
            public Func<string, ParseIdentifierOption, string> formatError = (string s, ParseIdentifierOption opt) => { return s; };

            // will force the outputted expression to be of this type if the value in is a number
            public Type overrideValueType;

            //output a default expression even when an error happens
            public bool defaultOnError;

            // true: use the row parameter for the select source row. Useful for listing the first match for each source row. Only works if the select is many-to-many
            // false: use the row parameter for the matching result row. Useful for listing all match for a fixed source value
            public bool useFirstMatchSelect;

            //when set, it will not take into consideration this SelectSet's condition when referenced
            public View.SelectSet BypassSelectSetCondition;

            // context in which the identifier is parsed. will provide select sets in which to look for identifiers.
            public ExpressionParsingContext expressionParsingContext;

            public ParseIdentifierOption(Database.View.ViewSchema schema, Table identifierColumn_table, bool useFirstMatchSelect, bool defaultOnError, Type overrideValueType, ExpressionParsingContext expressionParsingContext)
            {
                this.Schema = schema;
                this.identifierColumn_table = identifierColumn_table;
                this.useFirstMatchSelect = useFirstMatchSelect;
                this.defaultOnError = defaultOnError;
                this.overrideValueType = overrideValueType;
                this.expressionParsingContext = expressionParsingContext;
            }

            public ParseIdentifierOption(ExpressionParsingContext expressionParsingContext)
            {
                this.expressionParsingContext = expressionParsingContext;
            }
        }
        private static Expression ProcessDataBreakpointValue(Expression expression, MetaExpression value, ParseIdentifierOption opt)
        {
            if (value.hasDataBreakpointValue)
            {
                MetaExpression breakpointValue = new MetaExpression();
                breakpointValue.value = value.valueDataBreakpoint;
                breakpointValue.valueIsLiteral = value.dataBreakpointValueIsLiteral;
                var dataBreakpointExpression = ParseIdentifier(breakpointValue, opt);
                return ColumnCreator.CreateTypedExpressionDataBreakPoint(expression, dataBreakpointExpression);
            }
            return expression;
        }

        //value can be interpreted as:
        // a number: "0", "-1", "6.7", "+76"
        // a table.column identifier: "tableName.columnName"
        // a column identifier: "columnName"
        //value cannot be interpreted as:
        // a string: it can however return a string expression if no valid identifier is found and opt.defaultOnError is set to true
        public static Database.Operation.Expression ParseIdentifier(MetaExpression value, ParseIdentifierOption opt)
        {
            if (value.valueIsLiteral)
            {
                return ColumnCreator.CreateTypedExpressionConst(value.type == null ? typeof(string) : value.type, value.value);
            }
            else if (String.IsNullOrEmpty(value.value))
            {
                if (!opt.defaultOnError) UnityEngine.Debug.LogWarning(opt.formatError("No value specified", opt));
            }
            else if (value.value == MetaTable.kRowIndexColumnName)
            {
                var expression = new ExpTableRowIndex(opt.identifierColumn_table);
                return ProcessDataBreakpointValue(expression, value, opt);
            }
            else
            {
                if (char.IsDigit(value.value[0]) || value.value[0] == '-' || value.value[0] == '+')
                {
                    Type valueType;
                    if (opt.overrideValueType != null) valueType = opt.overrideValueType;
                    else if (value.type != null) valueType = value.type;
                    else valueType = typeof(int);
                    var expression = ColumnCreator.CreateTypedExpressionConst(valueType, value.value);
                    return ProcessDataBreakpointValue(expression, value, opt);
                }
                else
                {
                    if (opt.overrideValueType == typeof(bool)
                        || (opt.overrideValueType == null && value.type == typeof(bool)))
                    {
                        //could be a bool const value: "false" or "true"
                        bool b;
                        if (Boolean.TryParse(value.value, out b))
                        {
                            var expression = new ExpConst<bool>(b);
                            return ProcessDataBreakpointValue(expression, value, opt);
                        }
                    }

                    string[] identifier = value.value.Split('.');
                    int colIdentifierIndex = 0;
                    Database.Table targetTable;
                    Database.View.Select sel = null;
                    ExpressionParsingContext expressionParsingContext = opt.expressionParsingContext;
                    if (identifier.Length == 2)
                    {
                        colIdentifierIndex = 1;
                        if (expressionParsingContext != null && expressionParsingContext.TryGetSelect(identifier[0], out expressionParsingContext, out sel))
                        {
                            targetTable = sel.sourceTable;
                        }
                        else if (opt.identifierColumn_table != null && identifier[0] == opt.identifierColumn_table.GetName())
                        {
                            targetTable = opt.identifierColumn_table;
                        }
                        else
                        {
                            targetTable = null;
                            if (!opt.defaultOnError) DebugUtility.LogError(opt.formatError("Unknown identifier '" + identifier[0] + "', must be a table or select name.", opt));
                        }
                    }
                    else if (identifier.Length == 1)
                    {
                        colIdentifierIndex = 0;
                        targetTable = opt.identifierColumn_table;
                    }
                    else
                    {
                        targetTable = null;
                    }

                    if (targetTable != null)
                    {
                        var col = targetTable.GetColumnByName(identifier[colIdentifierIndex]);
                        var metaCol = targetTable.GetMetaData().GetColumnByName(identifier[colIdentifierIndex]);

                        if (col != null && metaCol != null)
                        {
                            Type columnValueType = metaCol.Type;
                            Expression expression;
                            if (sel != null)
                            {
                                if (opt.useFirstMatchSelect && sel.isManyToMany)
                                {
                                    expression = ColumnCreator.CreateTypedExpressionSelectFirstMatch(columnValueType, sel, col);
                                }
                                else
                                {
                                    expression = ColumnCreator.CreateTypedExpressionSelect(columnValueType, sel, col);
                                }

                                if (expressionParsingContext.selectSet.Condition != null
                                    && opt.BypassSelectSetCondition != expressionParsingContext.selectSet)
                                {
                                    expression = ColumnCreator.CreateTypedExpressionSelectSetConditional(columnValueType, expressionParsingContext.selectSet, expression);
                                }
                            }
                            else
                            {
                                expression = ColumnCreator.CreateTypedExpressionColumn(columnValueType, col);
                            }

                            // Check if we need a fix row
                            if (value.row >= 0)
                            {
                                // options requires a fixed row
                                expression = ColumnCreator.CreateTypedExpressionFixedRow(expression, value.row);
                            }
                            else if (expressionParsingContext != null && expressionParsingContext.fixedRow >= 0)
                            {
                                // Parsing context requires a fixed row
                                expression = ColumnCreator.CreateTypedExpressionFixedRow(expression, expressionParsingContext.fixedRow);
                            }

                            // Check if we need a type change
                            Type desiredValueType;
                            if (opt.overrideValueType != null) desiredValueType = opt.overrideValueType;
                            else if (value.type != null) desiredValueType = value.type;
                            else desiredValueType = columnValueType;
                            if (desiredValueType != columnValueType)
                            {
                                //require type change
                                expression = ColumnCreator.CreateTypedExpressionTypeChange(desiredValueType, expression);
                            }

                            //Check for default value
                            if (value.hasDefaultValue)
                            {
                                expression = ColumnCreator.CreateTypedExpressionDefaultOnError(expression, value.valueDefault);
                            }
                            expression = ProcessDataBreakpointValue(expression, value, opt);
                            return expression;
                        }
                        else
                        {
                            if (!opt.defaultOnError) DebugUtility.LogError(opt.formatError("Unknown identifier '" + identifier[colIdentifierIndex] + "', must be a column name.", opt));
                        }
                    }
                }
            }


            if (opt.defaultOnError) return new Database.Operation.ExpConst<string>(value.value);
            else return null;
        }
    }

    internal abstract class TypedExpression<DataT> : Expression where DataT : IComparable
    {
        public abstract DataT GetValue(long row);
        // if GetValueString result may be different from GetValue().ToString()
        // When it is the same, we can avoid calling GetValue and GetValueString which may both be expensive
        public virtual bool StringValueDiffers { get { return true; } }
    }

    internal class ExpConst<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ExpConst<" + typeof(DataT).Name + ">[" + row + "](" + value + ")";
        }

#endif
        public ExpConst(DataT a)
        {
            value = a;
            type = typeof(DataT);
        }

        public ExpConst(string a)
        {
            value = (DataT)Convert.ChangeType(a, typeof(DataT));
            type = typeof(DataT);
        }

        public override DataT GetValue(long row)
        {
            return value;
        }
        public override bool StringValueDiffers { get { return false; } }

        public override string GetValueString(long row)
        {
            return value.ToString();
        }

        public override IComparable GetComparableValue(long row)
        {
            return value;
        }

        public override bool HasMultipleRow()
        {
            return false;
        }

        public override long RowCount()
        {
            return 1;
        }

        public DataT value;
    }

    internal class ExpColumn<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ExpColumn<" + typeof(DataT).Name + ">[" + row + "]{" + column.GetDebugString(row) + "}";
        }

#endif
        public Database.ColumnTyped<DataT> column;

        public ExpColumn(Database.Column c)
        {
            column = (Database.ColumnTyped<DataT>)c;
            type = typeof(DataT);
        }

        public override string GetValueString(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpColumnGetValueString).Auto())
            {
                return GetValue(row).ToString();
            }
        }

        public override DataT GetValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpColumnGetValue).Auto())
            {
                return column.GetRowValue(row);
            }
        }

        public override IComparable GetComparableValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpColumnGetComparableValue).Auto())
            {
                return column.GetRowValue(row);
            }
        }

        public override bool HasMultipleRow()
        {
            return true;
        }

        public override long RowCount()
        {
            return column.GetRowCount();
        }
    }

    // Output the row index for all entries of a given table
    internal class ExpTableRowIndex : TypedExpression<long>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ExpTableRowIndex<long>[" + row + "]";
        }

#endif
        public Table table;

        public ExpTableRowIndex(Table table)
        {
            this.table = table;
            type = typeof(long);
        }

        public override string GetValueString(long row)
        {
            return row.ToString();
        }

        public override long GetValue(long row)
        {
            return row;
        }

        public override IComparable GetComparableValue(long row)
        {
            return row;
        }

        public override bool HasMultipleRow()
        {
            return true;
        }

        public override long RowCount()
        {
            return table.GetRowCount();
        }
    }

    // Change the type of the input expression. will log an error if a value cannot change to the target type
    internal class ExpTypeChange<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ExpTypeChange<" + typeof(DataT).Name + ">[" + row + "]{" + sourceExpression.GetDebugString(row) + "}";
        }

#endif
        public Expression sourceExpression;

        public ExpTypeChange(Expression sourceExpression)
        {
            this.sourceExpression = sourceExpression;
            type = typeof(DataT);
        }

        public override string GetValueString(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpTypeChangeGetValueString).Auto())
            {
                IComparable value = sourceExpression.GetComparableValue(row);
                return (string)Convert.ChangeType(value, typeof(string));
            }
        }

        public override DataT GetValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpTypeChangeGetValue).Auto())
            {
                IComparable value = sourceExpression.GetComparableValue(row);
                try
                {
                    return (DataT)Convert.ChangeType(value, typeof(DataT));
                }
                catch (System.Exception)
                {
                    DebugUtility.LogError("ExpTypeChange: Cannot type change value \"" + value.ToString()
                        + "\" from type '" + value.GetType().Name
                        + "' to type '" + typeof(DataT).Name
                        + "'"
#if MEMPROFILER_DEBUG_INFO
                    + " Source expression: " + sourceExpression.GetDebugString(row)
#endif
                    );
                    return default(DataT);
                }
            }
        }

        public override IComparable GetComparableValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpTypeChangeGetComparableValue).Auto())
            {
                return GetValue(row);
            }
        }

        public override bool HasMultipleRow()
        {
            return sourceExpression.HasMultipleRow();
        }

        public override long RowCount()
        {
            return sourceExpression.RowCount();
        }
    }

    //Use the row parameter for the select result table
    internal class ExpSelect<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            Update();
            if (m_rowIndex != null)
            {
                string str = "ExpSelect<" + typeof(DataT).Name + ">[" + row + "]";
                if (Algorithm.IsInValidRange(m_rowIndex, row))
                {
                    str += "{" + column.GetDebugString(m_rowIndex[row]) + "}";
                }
                else
                {
                    str += "{ Out of range (" + row + " : [0," + m_rowIndex.Length + "[}";
                }
                return str;
            }
            else
            {
                return "ExpSelect<" + typeof(DataT).Name + ">[" + row + "]{" + column.GetDebugString(row) + "}";
            }
        }

#endif
        public View.Select select;
        public Database.ColumnTyped<DataT> column;

        protected long[] m_rowIndex;
        public ExpSelect(View.Select sel, Database.Column c)
        {
            select = sel;
            column = (Database.ColumnTyped<DataT>)c;
            type = typeof(DataT);
        }

        private string GetOutOfRangeError(long row)
        {
            return "Out Of Range (" + row + " : [0," + m_rowIndex.Length + "[)"
#if MEMPROFILER_DEBUG_INFO
                + " Source Select: " + select.GetDebugString(row, 0)
#endif
            ;
        }

        public void Update()
        {
            if (m_rowIndex != null) return;
            m_rowIndex = select.GetMatchingIndices();
        }

        public override string GetValueString(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpSelectGetValueString).Auto())
            {
                Update();
                if (m_rowIndex != null)
                {
                    if (!DebugUtility.IsInValidRange(m_rowIndex, row)) return GetOutOfRangeError(row);
                    return column.GetRowValueString(m_rowIndex[row]);
                }
                else
                {
                    //no indices means it uses the full table as is
                    return column.GetRowValueString(row);
                }
            }
        }

        public override DataT GetValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpSelectGetValue).Auto())
            {
                Update();
                if (m_rowIndex != null)
                {
                    return column.GetRowValue(m_rowIndex[row]);
                }
                else
                {
                    //no indices means it uses the full table as is
                    return column.GetRowValue(row);
                }
            }
        }

        public override IComparable GetComparableValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpSelectGetComparableValue).Auto())
            {
                Update();
                if (m_rowIndex != null)
                {
                    if (!DebugUtility.IsInValidRange(m_rowIndex, row)) return GetOutOfRangeError(row);
                    return column.GetRowValue(m_rowIndex[row]);
                }
                else
                {
                    //no indices means it uses the full table as is
                    return column.GetRowValue(row);
                }
            }
        }

        public override bool HasMultipleRow()
        {
            return true;
        }

        public override long RowCount()
        {
            Update();
            if (m_rowIndex != null)
            {
                return m_rowIndex.LongLength;
            }
            return column.GetRowCount();
        }
    }

    // Yield the result of a sub expression using the index of the main select from a SelectSet after passing the SelectSet's condition.
    internal class ExpSelectSetConditional<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            string str = "ExpSelectSetConditional<" + typeof(DataT).Name + ">[" + row + "]";
            if (IsInRange(row))
            {
                long effectiveRow = GetEffectiveRow(row);
                str += "(" + effectiveRow + "){ " + SubExpression.GetDebugString(effectiveRow) + "}";
            }
            else
            {
                str += "{ Out of range (" + row + " : [0," + GetEffectiveRowCount() + "[}";
            }
            return str;
        }

#endif
        public View.SelectSet SelectSet;
        public TypedExpression<DataT> SubExpression;
        public ExpSelectSetConditional(View.SelectSet selectSet, TypedExpression<DataT> subExpression)
        {
            SelectSet = selectSet;
            SubExpression = subExpression;
            type = typeof(DataT);
        }

        private long GetEffectiveRowCount()
        {
            long[] indices = SelectSet.GetConditionalRowIndices();
            if (indices == null) return SubExpression.RowCount();
            return indices.LongLength;
        }

        private long GetEffectiveRow(long rowIn)
        {
            long[] indices = SelectSet.GetConditionalRowIndices();
            if (indices == null) return rowIn;
            return indices[rowIn];
        }

        private string GetOutOfRangeError(long row)
        {
            return "Out Of Range (" + row + " : [0," + GetEffectiveRowCount() + "[)"
#if MEMPROFILER_DEBUG_INFO
                + " SubExpression: " + SubExpression.GetDebugString(row)
#endif
            ;
        }

        private bool IsInRange(long row)
        {
            return row >= 0 && row < GetEffectiveRowCount();
        }

        public override string GetValueString(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpSelectSetConditionalGetValueString).Auto())
            {
                if (!IsInRange(row)) return GetOutOfRangeError(row);
                long effectiveRow = GetEffectiveRow(row);
                return SubExpression.GetValueString(effectiveRow);
            }
        }

        public override DataT GetValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpSelectSetConditionalGetValue).Auto())
            {
                long effectiveRow = GetEffectiveRow(row);
                return SubExpression.GetValue(effectiveRow);
            }
        }

        public override IComparable GetComparableValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpSelectSetConditionalGetComparableValue).Auto())
            {
                long effectiveRow = GetEffectiveRow(row);
                return SubExpression.GetComparableValue(effectiveRow);
            }
        }

        public override bool HasMultipleRow()
        {
            return SubExpression.HasMultipleRow();
        }

        public override long RowCount()
        {
            return GetEffectiveRowCount();
        }
    }
    //Use the row parameter for the select where condition and only return the first match in each result
    internal class ExpFirstMatchSelect<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            long index = select.GetIndexFirstMatch(row);
            if (index >= 0)
            {
                return "ExpFirstMatchSelect<" + typeof(DataT).Name + ">[" + row + "]{" + column.GetDebugString(index) + "}";
            }
            else
            {
                return "ExpFirstMatchSelect<" + typeof(DataT).Name + ">[" + row + "]{ default value }";
            }
        }

#endif
        public View.Select select;
        public Database.ColumnTyped<DataT> column;
        public ExpFirstMatchSelect(View.Select sel, Database.Column c)
        {
            select = sel;
            column = (Database.ColumnTyped<DataT>)c;
            type = typeof(DataT);
        }

        public override string GetValueString(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpFirstMatchSelectGetValueString).Auto())
            {
                var index = select.GetIndexFirstMatch(row);
                if (index >= 0)
                {
                    return column.GetRowValueString(index);
                }
                return "N/A";
            }
        }

        public override DataT GetValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpFirstMatchSelectGetValue).Auto())
            {
                var index = select.GetIndexFirstMatch(row);
                if (index >= 0)
                {
                    return column.GetRowValue(index);
                }
                return default(DataT);
            }
        }

        public override IComparable GetComparableValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpFirstMatchSelectGetComparableValue).Auto())
            {
                return GetValue(row);
            }
        }

        public override bool HasMultipleRow()
        {
            return true;
        }

        public override long RowCount()
        {
            return select.GetIndexFirstMatchRowCount();
        }
    }


    internal class ExpFixedRow<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ExpFixedRow<" + typeof(DataT).Name + ">[" + row + "]{" + exp.GetDebugString(row) + "}";
        }

#endif
        public TypedExpression<DataT> exp;
        public long row;
        public ExpFixedRow(TypedExpression<DataT> exp, long row)
        {
            this.exp = exp;
            this.row = row;
            type = exp.type;
        }

        public override string GetValueString(long row)
        {
            return exp.GetValueString(this.row);
        }

        public override DataT GetValue(long row)
        {
            return exp.GetValue(this.row);
        }

        public override IComparable GetComparableValue(long row)
        {
            return exp.GetComparableValue(this.row);
        }

        public override bool HasMultipleRow()
        {
            return false;
        }

        public override long RowCount()
        {
            if (row < exp.RowCount()) return 1;
            return 0;
        }
    }

    // Yield a default value if the input row is out of range of sub expression.
    internal class ExpDefaultOnError<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ExpDefaultOnError<" + typeof(DataT).Name + ">[" + row + "]{" + SubExpression.GetDebugString(row) + "}";
        }

#endif
        public TypedExpression<DataT> SubExpression;
        public DataT DefaultValue;
        public ExpDefaultOnError(TypedExpression<DataT> exp, DataT defaultValue)
        {
            SubExpression = exp;
            DefaultValue = defaultValue;
            type = SubExpression.type;
        }

        private bool IsInRange(long row)
        {
            return row >= 0 && row < SubExpression.RowCount();
        }

        public override string GetValueString(long row)
        {
            if (IsInRange(row)) return SubExpression.GetValueString(row);
            return DefaultValue.ToString();
        }

        public override DataT GetValue(long row)
        {
            if (IsInRange(row)) return SubExpression.GetValue(row);
            return DefaultValue;
        }

        public override IComparable GetComparableValue(long row)
        {
            if (IsInRange(row)) return SubExpression.GetComparableValue(row);
            return DefaultValue;
        }

        public override bool HasMultipleRow()
        {
            return SubExpression.HasMultipleRow();
        }

        public override long RowCount()
        {
            return SubExpression.RowCount();
        }
    }

    // Yields the merged value of a sub table from a viewtable's group index for a specific column.
    internal class ExpColumnMerge<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            var subTable = m_ParentViewTable.CreateGroupTable(m_ParentGroupIndex);
            if (subTable != null)
            {
                var subColumn = subTable.GetColumnByIndex(m_MetaColumnToMerge.index);
                var rowCount = subColumn.GetRowCount();
                if (rowCount == 0)
                {
                    return "ExpColumnMerge<" + typeof(DataT).Name + ">(0, 0)[" + row + "]{}";
                }
                else
                {
                    return "ExpColumnMerge<" + typeof(DataT).Name + ">(0, " + rowCount + ")[" + row + "]{ [0] = " + subColumn.GetDebugString(0) + "}";
                }
            }
            return "ExpColumnMerge<" + typeof(DataT).Name + ">(0, 0){ no sub table }";
        }

#endif
        private View.ViewTable m_ParentViewTable;
        private long m_ParentGroupIndex;
        private MetaColumn m_MetaColumnToMerge;
        public ExpColumnMerge(View.ViewTable parentViewTable, long parentGroupIndex, Column parentColumn, MetaColumn metaColumnToMerge)
        {
            m_ParentViewTable = parentViewTable;
            m_ParentGroupIndex = parentGroupIndex;
            m_MetaColumnToMerge = metaColumnToMerge;
            type = typeof(DataT);
        }

        public override string GetValueString(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpColumnMergeGetValueString).Auto())
            {
                return GetValue(row).ToString();
            }
        }

        public override DataT GetValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpColumnMergeGetValue).Auto())
            {
                var subTable = m_ParentViewTable.CreateGroupTable(m_ParentGroupIndex);
                if (subTable != null)
                {
                    var subColumn = subTable.GetColumnByIndex(m_MetaColumnToMerge.Index);
                    while (subColumn is IColumnDecorator)
                    {
                        subColumn = (subColumn as IColumnDecorator).GetBaseColumn();
                    }
                    return (DataT)m_MetaColumnToMerge.DefaultMergeAlgorithm.Merge(subColumn, new ArrayRange(0, subColumn.GetRowCount()));
                }
                return default(DataT);
            }
        }

        public override bool StringValueDiffers { get { return false; } }

        public override IComparable GetComparableValue(long row)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ExpColumnMergeGetComparableValue).Auto())
            {
                var subTable = m_ParentViewTable.CreateGroupTable(m_ParentGroupIndex);
                if (subTable != null)
                {
                    var subColumn = subTable.GetColumnByIndex(m_MetaColumnToMerge.Index);
                    while (subColumn is IColumnDecorator)
                    {
                        subColumn = (subColumn as IColumnDecorator).GetBaseColumn();
                    }
                    return m_MetaColumnToMerge.DefaultMergeAlgorithm.Merge(subColumn, new ArrayRange(0, subColumn.GetRowCount()));
                }
                return default(DataT);
            }
        }

        public override bool HasMultipleRow()
        {
            return false;
        }

        public override long RowCount()
        {
            return 1;
        }
    }

    // Triggers a breakpoint when the source expression yields a value equal the the break value expression. Useful for debugging only
    internal class ExpDataBreakPoint<DataT> : TypedExpression<DataT> where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ExpDataBreakPoint<" + typeof(DataT).Name + ">(" + BreakOnValue + ")[" + row + "]{" + sourceExpression.GetDebugString(row) + "}";
        }

#endif
        public TypedExpression<DataT> sourceExpression;
        public TypedExpression<DataT> BreakOnValue;
        public ExpDataBreakPoint(TypedExpression<DataT> sourceExpression, TypedExpression<DataT> breakOnValue)
        {
            this.sourceExpression = sourceExpression;
            type = typeof(DataT);
            BreakOnValue = breakOnValue;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void CheckBreakPoint(long row)
        {
            var rhv = sourceExpression.GetValue(row);
            var lhv = BreakOnValue.GetValue(row);
            if (lhv.CompareTo(rhv) == 0)
            {
                //System.Diagnostics.Debugger.Break(); this will not work with Unity debugger.
                DebugUtility.LogError("A user breakpoint event has been triggered. Attach a debugger and add a breakpoint on this line for debugging.");
            }
        }

        public override string GetValueString(long row)
        {
            CheckBreakPoint(row);
            return sourceExpression.GetValueString(row);
        }

        public override DataT GetValue(long row)
        {
            CheckBreakPoint(row);
            return sourceExpression.GetValue(row);
        }

        public override IComparable GetComparableValue(long row)
        {
            CheckBreakPoint(row);
            return sourceExpression.GetComparableValue(row);
        }

        public override bool HasMultipleRow()
        {
            return sourceExpression.HasMultipleRow();
        }

        public override long RowCount()
        {
            return sourceExpression.RowCount();
        }
    }

    internal abstract class Matcher
    {
        public abstract bool Match(Expression exp, long row);
        public abstract long[] GetMatchIndex(Expression exp, ArrayRange indices);
        public abstract long[] GetMatchIndex(Expression exp, ArrayRange indices, Operator operation);

        public Type type;
    }
    internal abstract class TypedMatcher<DataT> : Matcher where DataT : IComparable
    {
        public abstract bool Match(DataT a);
    }

    internal class ConstMatcher<DataT> : TypedMatcher<DataT> where DataT : IComparable
    {
        public DataT value;
        public ConstMatcher(DataT c)
        {
            value = c;
            type = typeof(DataT);
        }

        public override bool Match(DataT a)
        {
            return value.CompareTo(a) == 0;
        }

        public override bool Match(Expression exp, long row)
        {
            var v = exp.GetComparableValue(row);
            return v.CompareTo(value) == 0;
        }

        public override long[] GetMatchIndex(Expression exp, ArrayRange indices)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ConstMatcherQuery).Auto())
            {
                long count = indices.indexCount;
                long[] o = new long[count];
                long lastO = 0;

                if (exp is TypedExpression<DataT>)
                {
                    var e = (TypedExpression<DataT>)exp;

                    for (int i = 0; i != count; ++i)
                    {
                        long ii = indices[i];
                        var v = e.GetValue(ii);
                        if (v.CompareTo(value) == 0)
                        {
                            o[lastO] = ii;
                            ++lastO;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i != count; ++i)
                    {
                        long ii = indices[i];
                        var v = exp.GetComparableValue(ii);
                        if (v.CompareTo(value) == 0)
                        {
                            o[lastO] = ii;
                            ++lastO;
                        }
                    }
                }
                if (lastO != count)
                {
                    long[] trimmed = new long[lastO];
                    System.Array.Copy(o, trimmed, lastO);
                    return trimmed;
                }
                return o;
            }
        }
        public override long[] GetMatchIndex(Expression exp, ArrayRange indices, Operator operation)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ConstMatcherQuery).Auto())
            {
                long count = indices.indexCount;
                long[] o = new long[count];
                long lastO = 0;

                if (exp is TypedExpression<DataT>)
                {
                    var e = (TypedExpression<DataT>)exp;

                    for (int i = 0; i != count; ++i)
                    {
                        long ii = indices[i];
                        var v = e.GetValue(ii);
                        if (Operation.Match(operation, v.CompareTo(value)))
                        {
                            o[lastO] = ii;
                            ++lastO;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i != count; ++i)
                    {
                        long ii = indices[i];
                        var v = exp.GetComparableValue(ii);
                        if (Operation.Match(operation, v.CompareTo(value)))
                        {
                            o[lastO] = ii;
                            ++lastO;
                        }
                    }
                }
                if (lastO != count)
                {
                    long[] trimmed = new long[lastO];
                    System.Array.Copy(o, trimmed, lastO);
                    return trimmed;
                }
                return o;
            }
        }
    }
    internal class SubStringMatcher : TypedMatcher<string>
    {
        public string value;

        public override bool Match(string a)
        {
            return a.Contains(value);
        }

        public override bool Match(Expression exp, long row)
        {
            var v = exp.GetValueString(row);
            return v.Contains(value);
        }

        public override long[] GetMatchIndex(Expression exp, ArrayRange indices)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.SubStringMatcherQuery).Auto())
            {
                var value2 = value.ToLower();
                long count = indices.indexCount;
                long[] o = new long[count];
                long lastO = 0;

                for (int i = 0; i != count; ++i)
                {
                    long ii = indices[i];
                    var v = exp.GetValueString(ii);
                    if (v.ToLower().Contains(value2))
                    {
                        o[lastO] = ii;
                        ++lastO;
                    }
                }

                if (lastO != count)
                {
                    long[] trimmed = new long[lastO];
                    System.Array.Copy(o, trimmed, lastO);
                    return trimmed;
                }
                return o;
            }
        }
        public override long[] GetMatchIndex(Expression exp, ArrayRange indices, Operator operation)
        {
            throw new InvalidOperationException("Do not use operators with string matcher");
        }
    }

    // Yields the boolean result of comparing 2 expressions using a specified operator
    internal class ExpComparison : TypedExpression<bool>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "("
                + valueLeft.GetValueString(row)
                + " " + Operation.OperatorToString(operation) + " "
                + valueRight.GetValueString(row)
                + "){"
                + valueLeft.GetDebugString(row)
                + " " + Operation.OperatorToString(operation) + " "
                + valueRight.GetDebugString(row)
                + "}";
        }

#endif
        public Expression valueLeft;
        public Operator operation;
        public Expression valueRight;
        public Operation.ComparableComparator comparator;
        public bool Evaluate(long rowLeft, long rowRight)
        {
            var valLeft = valueLeft.GetComparableValue(rowLeft);
            return Operation.Match(operation, comparator, valLeft, valueRight, rowRight);
        }

        public override bool GetValue(long row)
        {
            return Evaluate(row, row);
        }

        public override string GetValueString(long row)
        {
            return GetValue(row).ToString();
        }

        public override bool StringValueDiffers { get { return false; } }

        public override IComparable GetComparableValue(long row)
        {
            return GetValue(row);
        }

        public override bool HasMultipleRow()
        {
            return valueLeft.HasMultipleRow() || valueRight.HasMultipleRow();
        }

        public override long RowCount()
        {
            return Math.Min(valueLeft.RowCount(), valueRight.RowCount());
        }

        public bool IsManyToMany()
        {
            if (Operation.IsOperatorOneToMany(operation))
            {
                return false;
            }
            return valueLeft.HasMultipleRow() && valueRight.HasMultipleRow();
        }
    }

    // Compares a column value to an expression using a specified operator
    internal class ColumnComparison
    {
#if MEMPROFILER_DEBUG_INFO
        public string GetDebugString(long columnRow, long valueRow)
        {
            return "("
                + column.GetRowValueString(columnRow)
                + " " + Operation.OperatorToString(operation) + " "
                + value.GetValueString(columnRow)
                + "){"
                + "\"" + ColumnName + "\""
                + " " + column.GetDebugString(valueRow)
                + " " + Operation.OperatorToString(operation) + " "
                + value.GetDebugString(valueRow)
                + "}";
        }

        public string ColumnName;
#endif
        public Column column;
        public Operator operation;
        public Expression value;


        public long GetFirstMatchIndex(long row)
        {
            return column.GetFirstMatchIndex(operation, value, row);
        }

        public long[] GetMatchIndex(long row)
        {
            return column.GetMatchIndex(ArrayRange.FirstLast(0, column.GetRowCount()), operation, value, row, false);
        }

        public long[] GetMatchIndex(long row, ArrayRange range)
        {
            return column.GetMatchIndex(range, operation, value, row, false);
        }

        public bool IsManyToMany()
        {
            if (Operation.IsOperatorOneToMany(operation))
            {
                return false;
            }
            return value.HasMultipleRow();
        }
    }


    internal class MetaComparisonBase
    {
        private static System.Collections.Generic.SortedDictionary<string, Operator> _m_StringToOp;
        protected static System.Collections.Generic.SortedDictionary<string, Operator> m_StringToOp
        {
            get
            {
                if (_m_StringToOp == null)
                {
                    _m_StringToOp = new System.Collections.Generic.SortedDictionary<string, Operator>();
                    _m_StringToOp.Add("equal", Operator.Equal);
                    _m_StringToOp.Add("greater", Operator.Greater);
                    _m_StringToOp.Add("greaterEqual", Operator.GreaterEqual);
                    _m_StringToOp.Add("less", Operator.Less);
                    _m_StringToOp.Add("lessEqual", Operator.LessEqual);
                    _m_StringToOp.Add("notEqual", Operator.NotEqual);
                    _m_StringToOp.Add("isIn", Operator.IsIn);
                    _m_StringToOp.Add("notIn", Operator.NotIn);

                    _m_StringToOp.Add("=", Operator.Equal);
                    _m_StringToOp.Add("==", Operator.Equal);
                    _m_StringToOp.Add(">", Operator.Greater);
                    _m_StringToOp.Add(">=", Operator.GreaterEqual);
                    _m_StringToOp.Add("<", Operator.Less);
                    _m_StringToOp.Add("<=", Operator.LessEqual);
                    _m_StringToOp.Add("!=", Operator.NotEqual);
                }
                return _m_StringToOp;
            }
        }
    }

    // Represent an unresolved ColumnComparison. Can be resolved using a Expression.ParseIdentifierOption through the Build method
    internal class MetaColumnComparison : MetaComparisonBase
    {
        public string columnName;
        public Operator operation;
        public Expression.MetaExpression value;
        public MetaColumnComparison() {}
        public MetaColumnComparison(string column, Operator operation, Expression.MetaExpression value)
        {
            columnName = column;
            this.operation = operation;
            this.value = value;
        }

        public ColumnComparison Build(Expression.ParseIdentifierOption option)
        {
            ColumnComparison comparison = new ColumnComparison();
#if MEMPROFILER_DEBUG_INFO
            comparison.ColumnName = columnName;
#endif
            comparison.operation = operation;

            var metaColumn = option.identifierColumn_table.GetMetaData().GetColumnByName(columnName);
            if (metaColumn == null)
            {
                DebugUtility.LogError(option.formatError("No column named '" + columnName + "' in table '" + option.identifierColumn_table.GetName() + "'", option));
                return null;
            }

            comparison.column = option.identifierColumn_table.GetColumnByName(columnName);
            if (comparison.column == null)
            {
                DebugUtility.LogError(option.formatError("No column named '" + columnName + "' in table '" + option.identifierColumn_table.GetName() + "'", option));
                return null;
            }

            if (option.overrideValueType == null)
            {
                option.overrideValueType = metaColumn.Type;
            }

            if (Operation.IsOperatorOneToMany(operation))
            {
                option.useFirstMatchSelect = false;
            }
            comparison.value = Expression.ParseIdentifier(value, option);

            return comparison;
        }

        public static MetaColumnComparison LoadFromXML(System.Xml.XmlElement root)
        {
            MetaColumnComparison comparison = new MetaColumnComparison();
            comparison.columnName = root.GetAttribute("column");

            comparison.value = Expression.MetaExpression.LoadFromXML(root);
            string strOp = root.GetAttribute("op");
            if (!m_StringToOp.TryGetValue(strOp, out comparison.operation))
            {
                DebugUtility.LogError("Unknown operator '" + strOp + "'.");
            }
            DebugUtility.LogAnyXmlChildAsInvalid(root);
            return comparison;
        }
    }

    // Represent an unresolved ExpComparison. Can be resolved using a Expression.ParseIdentifierOption through the Build method
    internal class MetaExpComparison : MetaComparisonBase
    {
        public Expression.MetaExpression valueLeft;
        public Operator operation;
        public Expression.MetaExpression valueRight;
        public ExpComparison Build(Expression.ParseIdentifierOption option)
        {
            ExpComparison comparison = new ExpComparison();
            comparison.operation = operation;
            comparison.valueLeft = Expression.ParseIdentifier(valueLeft, option);
            comparison.valueRight = Expression.ParseIdentifier(valueRight, option);
            comparison.comparator = Operation.GetComparator(comparison.valueLeft.type, comparison.valueRight.type);
            return comparison;
        }

        public static MetaExpComparison LoadFromXML(System.Xml.XmlElement root)
        {
            MetaExpComparison exp = new MetaExpComparison();

            string strOp = root.GetAttribute("op");
            if (!m_StringToOp.TryGetValue(strOp, out exp.operation))
            {
                DebugUtility.LogError("Unknown operator '" + strOp + "'.");
            }

            foreach (XmlNode xNode in root.ChildNodes)
            {
                if (xNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement e = (XmlElement)xNode;
                    switch (e.Name)
                    {
                        case "Left":
                            exp.valueLeft = Expression.MetaExpression.LoadFromXML(e);
                            break;
                        case "Right":
                            exp.valueRight = Expression.MetaExpression.LoadFromXML(e);
                            break;
                        default:
                            DebugUtility.LogInvalidXmlChild(root, e);
                            break;
                    }
                }
            }
            return exp;
        }
    }
}
