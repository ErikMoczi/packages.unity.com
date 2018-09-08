using System;

namespace Unity.MemoryProfiler.Editor.Database.Operation
{
    public class Comparer
    {
        public static System.Collections.Generic.IComparer<DataT> Ascending<DataT>() where DataT : System.IComparable
        {
            if (typeof(DataT).IsValueType)
            {
                return new AscendingComparerValueType<DataT>();
            }
            else
            {
                return new AscendingComparerReferenceType<DataT>();
            }
        }

        public static System.Collections.Generic.IComparer<DataT> Descending<DataT>() where DataT : System.IComparable
        {
            if (typeof(DataT).IsValueType)
            {
                return new DescendingComparerValueType<DataT>();
            }
            else
            {
                return new DescendingComparerReferenceType<DataT>();
            }
        }
    }
    public enum Operator
    {
        equal,
        notEqual,
        greater,
        greaterEqual,
        less,
        lessEqual,
        isIn,
        notIn
    }
    public class Operation
    {
        public static bool IsOperatorOneToOne(Operator op)
        {
            return !IsOperatorOneToMany(op);
        }

        public static bool IsOperatorOneToMany(Operator op)
        {
            switch (op)
            {
                case Operator.isIn:
                case Operator.notIn:
                    return true;
                default: return false;
            }
        }

        public static string OperatorToString(Operator op)
        {
            switch (op)
            {
                case Operator.equal: return "==";
                case Operator.notEqual: return "!=";
                case Operator.greater: return ">";
                case Operator.greaterEqual: return ">=";
                case Operator.less: return "<";
                case Operator.lessEqual: return "<=";
                case Operator.isIn: return "isIn";
                case Operator.notIn: return "notIn";
                default: return "<unknown operator>";
            }
        }

        public static int Compare(ComparableComparator comparator, IComparable leftValue, IComparable rightValue)
        {
            try
            {
                return comparator(leftValue, rightValue);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to compare values '" + (leftValue == null ? "null" : leftValue.ToString())
                    + "' and '" + (rightValue == null ? "null" : rightValue.ToString()) + "'. "
                    + e.Message);
            }
        }

        public static bool Match(Operator op, ComparableComparator comparator, IComparable leftValue, IComparable rightValue)
        {
            return Match(op, Compare(comparator, leftValue, rightValue));
        }

        public static bool Match(Operator op, ComparableComparator comparator, IComparable leftValue, Expression rightExpression, long rightExpressionRow)
        {
            switch (op)
            {
                case Operator.isIn:
                case Operator.notIn:
                {
                    long rightRowCount = rightExpression.RowCount();
                    for (long expRow = 0; expRow != rightRowCount; ++expRow)
                    {
                        var rightValue = rightExpression.GetComparableValue(expRow);
                        if (Compare(comparator, leftValue, rightValue) == 0)
                        {
                            return op == Operator.isIn;
                        }
                    }
                    return op == Operator.notIn;
                }
                default:
                {
                    var rightValue = rightExpression.GetComparableValue(rightExpressionRow);
                    return Match(op, comparator(leftValue, rightValue));
                }
            }
        }

        public static bool Match(Operator op, int CompareResult)
        {
            switch (op)
            {
                case Operator.equal: return CompareResult == 0;
                case Operator.notEqual: return CompareResult != 0;
                case Operator.greater: return CompareResult > 0;
                case Operator.greaterEqual: return CompareResult >= 0;
                case Operator.less: return CompareResult < 0;
                case Operator.lessEqual: return CompareResult <= 0;
            }
            throw new System.Exception("bad operator");
        }

        public static Operator Inverse(Operator op)
        {
            switch (op)
            {
                case Operator.equal: return Operator.notEqual;
                case Operator.notEqual: return Operator.equal;
                case Operator.greater: return Operator.lessEqual;
                case Operator.greaterEqual: return Operator.less;
                case Operator.less: return Operator.greaterEqual;
                case Operator.lessEqual: return Operator.greater;
                case Operator.isIn: return Operator.notIn;
                case Operator.notIn: return Operator.isIn;
            }
            throw new System.Exception("bad operator");
        }

        public static bool Test<ExpLhs, ExpRhs>(ExpLhs lhs, Operator op, ExpRhs rhs) where ExpLhs : IComparable where ExpRhs : IComparable
        {
            switch (op)
            {
                case Operator.equal: return lhs.CompareTo(rhs) == 0;
                case Operator.notEqual: return lhs.CompareTo(rhs) != 0;
                case Operator.greater: return lhs.CompareTo(rhs) > 0;
                case Operator.greaterEqual: return lhs.CompareTo(rhs) >= 0;
                case Operator.less: return lhs.CompareTo(rhs) < 0;
                case Operator.lessEqual: return lhs.CompareTo(rhs) <= 0;
            }
            throw new System.Exception("bad operator");
        }

        public delegate int ComparableComparator(IComparable a, IComparable b);
        public class TypePair : IComparable
        {
            int IComparable.CompareTo(object obj)
            {
                TypePair o = (TypePair)obj;
                if (o == null) return 1;
                int r1 = a.Name.CompareTo(o.a.Name);
                if (r1 == 0)
                {
                    return b.Name.CompareTo(o.b.Name);
                }
                return r1;
            }

            public TypePair(Type aa, Type ab)
            {
                a = aa;
                b = ab;
            }

            public Type a;
            public Type b;
        }
        private static ComparableComparator _comparableComparatorIdentity;
        public static ComparableComparator comparableComparatorIdentity
        {
            get
            {
                if (_comparableComparatorIdentity == null)
                {
                    _comparableComparatorIdentity = (IComparable a, IComparable b) => {
                            try
                            {
                                return a.CompareTo(b);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Failed to compare values: '" + (a == null ? "null" : a.ToString())
                                    + "' and '" + (b == null ? "null" : b.ToString())
                                    + "' Exception: " + e.Message);
                            }
                        };
                }
                return _comparableComparatorIdentity;
            }
        }
        private static System.Collections.Generic.SortedDictionary<TypePair, ComparableComparator> comparableComparators;
        private static int PromoteToInt(string sb)
        {
            int ib;
            if (int.TryParse(sb, out ib))
            {
                return ib;
            }
            return 0;
        }

        private static long PromoteToLong(string sb)
        {
            long ib;
            if (long.TryParse(sb, out ib))
            {
                return ib;
            }
            return 0;
        }

        private static float PromoteToFloat(string sb)
        {
            float ib;
            if (float.TryParse(sb, out ib))
            {
                return ib;
            }
            return 0;
        }

        private static bool PromoteToBool(string sb)
        {
            bool ib;
            if (bool.TryParse(sb, out ib))
            {
                return ib;
            }
            return false;
        }

        public static ComparableComparator GetComparator(Type ta, Type tb)
        {
            if (ta == tb) { return comparableComparatorIdentity; }
            if (comparableComparators == null)
            {
                comparableComparators = new System.Collections.Generic.SortedDictionary<TypePair, ComparableComparator>();
                var identity = comparableComparatorIdentity;
                comparableComparators.Add(new TypePair(typeof(int), typeof(string)), (IComparable a, IComparable b) => a.CompareTo(PromoteToInt((string)b)));
                comparableComparators.Add(new TypePair(typeof(float), typeof(string)), (IComparable a, IComparable b) => a.CompareTo(PromoteToFloat((string)b)));
                comparableComparators.Add(new TypePair(typeof(long), typeof(string)), (IComparable a, IComparable b) => a.CompareTo(PromoteToLong((string)b)));
                comparableComparators.Add(new TypePair(typeof(bool), typeof(string)), (IComparable a, IComparable b) => a.CompareTo(PromoteToBool((string)b)));

                comparableComparators.Add(new TypePair(typeof(string), typeof(int)), (IComparable a, IComparable b) => PromoteToInt((string)b).CompareTo(a));
                comparableComparators.Add(new TypePair(typeof(string), typeof(float)), (IComparable a, IComparable b) => PromoteToFloat((string)b).CompareTo(a));
                comparableComparators.Add(new TypePair(typeof(string), typeof(long)), (IComparable a, IComparable b) => PromoteToLong((string)b).CompareTo(a));
                comparableComparators.Add(new TypePair(typeof(string), typeof(bool)), (IComparable a, IComparable b) => PromoteToBool((string)b).CompareTo(a));

                comparableComparators.Add(new TypePair(typeof(int), typeof(float)), (IComparable a, IComparable b) => ((float)(int)a).CompareTo(b));
                comparableComparators.Add(new TypePair(typeof(long), typeof(float)), (IComparable a, IComparable b) => ((float)(long)a).CompareTo(b));
                comparableComparators.Add(new TypePair(typeof(bool), typeof(float)), (IComparable a, IComparable b) => ((bool)a).CompareTo(((float)b) != 0.0f));

                comparableComparators.Add(new TypePair(typeof(int), typeof(long)), (IComparable a, IComparable b) => ((long)(int)a).CompareTo(b));
                comparableComparators.Add(new TypePair(typeof(int), typeof(bool)), (IComparable a, IComparable b) => (((int)a) != 0).CompareTo(b));

                comparableComparators.Add(new TypePair(typeof(long), typeof(int)), (IComparable a, IComparable b) => a.CompareTo((long)(int)b));
                comparableComparators.Add(new TypePair(typeof(long), typeof(bool)), (IComparable a, IComparable b) => (((long)a) != 0).CompareTo(b));

                comparableComparators.Add(new TypePair(typeof(bool), typeof(int)), (IComparable a, IComparable b) => a.CompareTo((int)b != 0));
                comparableComparators.Add(new TypePair(typeof(bool), typeof(long)), (IComparable a, IComparable b) => a.CompareTo((long)b != 0));
            }
            ComparableComparator o;
            if (comparableComparators.TryGetValue(new TypePair(ta, tb), out o))
            {
                return o;
            }
            return comparableComparatorIdentity;
        }
    }
}
