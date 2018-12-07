

using System;
using System.Collections.Generic;

namespace Unity.Tiny
{
    internal static class AcyclicGraphIterator
    {
        /// <summary>
        /// https://en.wikipedia.org/wiki/Topological_sorting
        /// </summary>
        internal static class DepthFirst
        {
            private enum MarkType
            {
                Unmarked,
                Temporary,
                Permanent
            }

            public static void Execute<T>(AcyclicGraph<T> graph, Action<T> action)
            {
                var nodes = graph.Nodes;
                var marks = new MarkType[nodes.Count];

                foreach (var node in nodes)
                {
                    Visit(node, marks, action);
                }
            }

            private static void Visit<T>(AcyclicGraph<T>.Node node, IList<MarkType> marks, Action<T> action)
            {
                var index = node.Index;

                switch (marks[index])
                {
                    case MarkType.Permanent:
                        return;
                    case MarkType.Temporary:
                        return;
                }

                marks[index] = MarkType.Temporary;

                foreach (var connection in node.Connections)
                {
                    Visit(connection, marks, action);
                }

                marks[index] = MarkType.Permanent;

                var data = node.Data;

                if (data != null)
                {
                    action(data);
                }
            }
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm
        /// </summary>
        internal static class DetectCycle
        {
            private struct Context
            {
                public int UnusedIndex;
                public int[] vIndex;
                public int[] vLowLink;
                public bool[] vOnStack;
                public int[] Stack;
                public int SP;
            }

            public static void Execute<T>(AcyclicGraph<T> graph, Action startOfCycle, Action endOfCycle, Action<T> action)
            {
                var nodes = graph.Nodes;
                var count = nodes.Count;

                var vIndex = new int[count];
                var vLowLink = new int[count];
                var vOnStack = new bool[count];
                var stack = new int[count];

                for (var i = 0; i < count; ++i)
                {
                    vIndex[i] = -1;
                    vLowLink[i] = -1;
                }

                var context = new Context
                {
                    UnusedIndex = 0,
                    vIndex = vIndex,
                    vLowLink = vLowLink,
                    vOnStack = vOnStack,
                    Stack = stack,
                };

                for (var i = 0; i < nodes.Count; i++)
                {
                    if (context.vIndex[i] == -1)
                    {
                        StrongConnect(graph, nodes[i], ref context, startOfCycle, endOfCycle, action);
                    }
                }
            }

            private static void StrongConnect<T>(AcyclicGraph<T> graph, AcyclicGraph<T>.Node node, ref Context context, Action startOfCycle, Action endOfCycle, Action<T> action)
            {
                var report = false;
                
                // Index of the current node being processed
                var index = node.Index;

                // Set the depth index for this node to the smallest unused index
                context.vIndex[index] = context.UnusedIndex;
                context.vLowLink[index] = context.UnusedIndex;

                context.UnusedIndex++;

                context.Stack[context.SP++] = index;
                context.vOnStack[index] = true;

                foreach (var connection in node.Connections)
                {
                    if (context.vIndex[connection.Index] == -1)
                    {
                        // Successor has not yet been visited; recurse on it
                        StrongConnect(graph, connection, ref context, startOfCycle, endOfCycle, action);
                        context.vLowLink[index] = UnityEngine.Mathf.Min(context.vLowLink[index], context.vLowLink[connection.Index]);
                    }
                    else if (index == connection.Index)
                    {
                        report = true;
                    }
                    else if (context.vOnStack[connection.Index])
                    {
                        // Successor is on the stack and hence in the current SCC
                        // Note: The next line may look odd - but is correct.
                        // It says connection.index not connection.lowlink; that is deliberate and from the original paper
                        context.vLowLink[index] = UnityEngine.Mathf.Min(context.vLowLink[index], context.vIndex[connection.Index]);
                    }
                }

                if (context.vLowLink[index] != context.vIndex[index])
                {
                    return;
                }

                report |= context.Stack[context.SP - 1] != index;

                if (report)
                {
                    // If is a root node, pop the stack and generate an SCC
                    startOfCycle();
                }

                int top;

                do
                {
                    top = context.Stack[--context.SP];
                    context.vOnStack[top] = false;
                    if (report)
                    {
                        action(graph.Nodes[top].Data);
                    }
                } while (top != index);

                if (report)
                {
                    endOfCycle();
                }
            }
        }
    }
}

