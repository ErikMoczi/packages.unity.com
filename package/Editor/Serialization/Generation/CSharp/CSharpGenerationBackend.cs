#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public class CSharpGenerationBackend
    {
        public string Code { get; internal set;  } = string.Empty;

        public void Generate(List<PropertyTypeNode> root, List<string> usings = null)
        {
            if (root.Count == 0)
            {
                return;
            }

            _cache.Clear();

            // @TODO Cleanup

            var propertyNodesByName = root.Select(
                    p => new KeyValuePair<string, PropertyTypeNode>(p.TypeName, p)
                ).ToDictionary(e => e.Key, e => e.Value);

            Func<string, CSharpGenerationCache.CodeInfo> dependancyLookupFunc = null;

            dependancyLookupFunc = (typeName) =>
            {
                // @TODO watch out for dependancy loops
                if (! propertyNodesByName.ContainsKey(typeName))
                {
                    throw new Exception($"Invalid request for property container type generation '{typeName}'");
                }

                if (_cache.Cache.ContainsKey(typeName))
                {
                    return _cache.Cache[typeName];
                }

                var g = new CSharpContainerGenerator();
                g.GeneratePropertyContainer(
                    propertyNodesByName[typeName],
                    dependancyLookupFunc);

                _cache.Cache[typeName] = new CSharpGenerationCache.CodeInfo()
                {
                    Code = g.Code.ToString(),
                    GeneratedPropertyFieldNames = g.PropertyBagItemNames
                };

                return _cache.Cache[typeName];
            };

            var code = new StringBuffer();

            WithUsing(code, usings);

            foreach (var container in root)
            {
                var g = new CSharpContainerGenerator();
                g.GeneratePropertyContainer(container, dependancyLookupFunc);

                code.Append(g.Code);
            }

            Code = code.ToString();
        }
        
        private readonly CSharpGenerationCache _cache = new CSharpGenerationCache();
        
        private static void WithUsing(StringBuffer sb, List<string> usings = null)
        {
            var defaultUsingAssemblies = new List<string>()
            {
                "System",
                "System.Collections.Generic",
                "Unity.Properties"
            };

            var usingDirectives = usings ?? defaultUsingAssemblies;

            usingDirectives.ForEach((currentUsing) =>
            {
                sb.Append($"using {currentUsing};");
                sb.Append(Environment.NewLine);
            });

            sb.Append(Environment.NewLine);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
