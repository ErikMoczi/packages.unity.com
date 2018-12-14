

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyTypeValidation
    {
        public static bool Validate(IEnumerable<TinyType> types)
        {
            var graph = new AcyclicGraph<TinyType.Reference>();

            foreach (var type in types)
            {
                if (type.IsRuntimeIncluded)
                {
                    continue;
                }

                if (type.IsPrimitive)
                {
                    continue;
                }
                
                var typeNode = graph.GetOrAdd(type.Ref);
                
                foreach (var field in type.Fields)
                {
                    var fieldNode = graph.GetOrAdd(field.FieldType);
                    graph.AddDirectedConnection(typeNode, fieldNode);
                }
            }

            if (DetectCycle(graph, out var error))
            {
                Debug.LogError(error);
                return false;
            }

            return true;
        }

        private static bool DetectCycle(AcyclicGraph<TinyType.Reference> graph, out string error)
        {
            var count = 0;
            StringBuilder sb = null;

            AcyclicGraphIterator.DetectCycle.Execute(graph,
                () =>
                {
                    sb = sb ?? new StringBuilder();
                    sb.Append($"[{TinyConstants.ApplicationName}] TinyType detected cyclic reference (");
                },
                () =>
                {
                    sb.Append(")");
                },
                type =>
                {
                    if (count != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(type.Name);

                    count++;
                });

            error = sb?.ToString();
            return !string.IsNullOrEmpty(error);
        }
    }
}
