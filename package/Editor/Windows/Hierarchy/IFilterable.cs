using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Tiny
{
    [Flags]
    internal enum StringCompare
    {
        Contains     = 0,
        StartsWith   = 1 << 0,
        EndsWith     = 1 << 1,
        Exact        = StartsWith | EndsWith,
    }

    internal interface IFilterable
    {
        bool Keep(TinyEntity entity);
    }

    internal static class IFilterableExtensions
    {
        internal static bool Compare(this IFilterable filterable, string lhs, string rhs, StringCompare compare)
        {
            if (string.IsNullOrEmpty(lhs) || string.IsNullOrEmpty(rhs))
            {
                return true;
            }

            switch (compare)
            {
                case StringCompare.Contains:
                    return lhs.IndexOf(rhs, StringComparison.OrdinalIgnoreCase) >= 0;
                case StringCompare.StartsWith:
                    return lhs.StartsWith(rhs, StringComparison.OrdinalIgnoreCase);
                case StringCompare.EndsWith:
                    return lhs.EndsWith(rhs, StringComparison.OrdinalIgnoreCase);
                case StringCompare.Exact:
                    return string.Equals(lhs, rhs, StringComparison.OrdinalIgnoreCase);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal struct NameFilter : IFilterable
    {
        public string Name;
        public StringCompare Compare;

        public bool Inverted { get; set; }

        public bool Keep(TinyEntity entity)
        {
            return Keep(entity.Name);
        }

        public bool Keep(string name)
        {
            if (string.IsNullOrEmpty(Name))
            {
                return true;
            }

            return this.Compare(name, Name, Compare) ^ Inverted;
        }
    }

    internal struct ComponentFilter : IFilterable
    {
        public NameFilter Name;

        public bool Keep(TinyEntity entity)
        {
            var name = Name;
            return !name.Inverted ? entity.Components.Any(c => name.Keep(c.Name)) : entity.Components.All(c => name.Keep(c.Name));
        }
    }

    internal struct HierarchyFilter
    {
        private readonly IFilterable[] m_Filters;

        public HierarchyFilter(IEnumerable<IFilterable> filters)
        {
            m_Filters = filters.ToArray();
        }

        public bool Keep(TinyEntity entity)
        {
            return m_Filters.All(f => f.Keep(entity));
        }
    }

    internal static class FilterUtility
    {
        internal static HierarchyFilter CreateFilter(string input)
        {
            return new HierarchyFilter(GenerateFilterTokens(input));
        }

        private static IEnumerable<IFilterable> GenerateFilterTokens(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                yield break;
            }

            filter = filter.Trim();

            var start = 0;
            var component = false;
            for (var i = 0; i < filter.Length - 1; ++i)
            {
                if (filter[i] == ' ')
                {
                    if (component)
                    {
                        component = false;
                        yield return CreateComponentFilter(filter.Substring(start, i - start));
                    }
                    else
                    {
                        yield return CreateNameFilter(filter.Substring(start, i - start));
                    }

                    start = i + 1;
                    continue;
                }

                if (char.ToLower(filter[i]) == 'c' && char.ToLower(filter[i + 1]) == ':')
                {
                    component = true;
                    start = i + 2;
                    ++i;
                }
            }

            if (start < filter.Length)
            {
                if (component)
                {
                    yield return CreateComponentFilter(filter.Substring(start));
                }
                else
                {
                    yield return CreateNameFilter(filter.Substring(start));
                }
            }
        }

        private static NameFilter CreateNameFilter(string input)
        {
            input = input.Trim();
            var filter = new NameFilter();

            var length = input.Length;
            var current = 0;
            var end = input.Length;

            if (length > current && input[current] == '!')
            {
                filter.Inverted = true;
                ++current;
            }

            if (length > current && input[current] == '^')
            {
                filter.Compare |= StringCompare.StartsWith;
                ++current;
            }

            if (length > 0 && input[length - 1] == '^')
            {
                filter.Compare |= StringCompare.EndsWith;
                --end;
            }

            var size = end - current;
            if (current < length && current + size <= length)
            {
                filter.Name = input.Substring(current, size);
            }

            return filter;
        }

        private static ComponentFilter CreateComponentFilter(string input)
        {
            return new ComponentFilter {Name = CreateNameFilter(input)};
        }
    }
}