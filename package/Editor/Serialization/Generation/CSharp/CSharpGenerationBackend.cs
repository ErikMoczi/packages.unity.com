#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    public class CSharpGenerationBackend : IPropertyContainerGenerationBackend
    {
        public StringBuffer Code { get; internal set;  } = new StringBuffer();

        public void Generate(List<PropertyTypeNode> root)
        {
            if (root.Count == 0)
            {
                return;
            }

            _cache.Clear();

            // @TODO Cleanup

            var propertyNodesByName = root.Select(
                p => new KeyValuePair<string, PropertyTypeNode>(p.TypeName, p))
                .ToDictionary(e => e.Key, e => e.Value);

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

            using (new CSharpSourceFileDecorator(code))
            {
                foreach (var container in root)
                {
                    var g = new CSharpContainerGenerator();
                    g.GeneratePropertyContainer(container, dependancyLookupFunc);

                    code.Append(g.Code);
                }
            }

            Code = code;
        }

        public void GenerateProperty(PropertyTypeNode property,
            string containerName,
            PropertyTypeNode.TypeTag containerTypeTag)
        {
            _cache.Clear();

            var code = new CSharpContainerGenerator.Scope();

            var g = new CSharpContainerGenerator();
            g.GenerateProperty(containerName, containerTypeTag, property, code);

            Code = code.Code;
        }

        public void GeneratePropertyContainer(PropertyTypeNode property)
        {
            _cache.Clear();

            var code = new StringBuffer();

            var g = new CSharpContainerGenerator();
            g.GeneratePropertyContainer(property);
            code.Append(g.Code);

            Code = code;
        }

        private readonly CSharpGenerationCache _cache = new CSharpGenerationCache();
        
        private class CSharpSourceFileDecorator : IDisposable
        {
            private readonly List<string> _usings = new List<string>()
            {
                "System",
                "System.Collections.Generic",
                "Unity.Properties"
            };

            private StringBuffer _sb;

            public CSharpSourceFileDecorator(StringBuffer sb)
            {
                _sb = sb;

                GenerateHeader();
            }

            private void GenerateHeader()
            {
                _usings.ForEach((currentUsing) =>
                {
                    _sb.Append($"using {currentUsing};");
                    _sb.Append(Environment.NewLine);
                });
                _sb.Append(Environment.NewLine);
            }

            public void Dispose()
            {
            }
        }
    }
}
#endif // NET_4_6
