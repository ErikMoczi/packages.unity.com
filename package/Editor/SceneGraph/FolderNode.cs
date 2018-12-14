using System;
using System.Linq;

namespace Unity.Tiny
{
    internal class FolderNode : SceneGraphNodeBase
    {
        public string Name { get; set; }

        public FolderNode(ISceneGraph graph, string name)
            : base(graph)
        {
            Name = name;
        }

        public static FolderNode GetOrCreateFolderHierarchy(ISceneGraph graph, string path)
        {
            // Validate arguments
            if (graph == null || string.IsNullOrEmpty(path))
            {
                throw new ArgumentException();
            }

            var names = path.Split('/');
            if (names.Count() == 0)
            {
                throw new ArgumentException(path);
            }

            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(path);
                }
            }

            // Since graph.Roots is not a node, we have to handle it node separately
            var parent = graph.Roots.OfType<FolderNode>().FirstOrDefault(n => n.Name == names[0]);
            if (parent == null)
            {
                parent = new FolderNode(graph, names[0]);
                graph.Roots.Add(parent);
            }

            // Create folder nodes
            foreach (var name in names.Skip(1))
            {
                var folder = parent.Children.OfType<FolderNode>().FirstOrDefault(n => n.Name == name) ?? null;
                if (folder == null)
                {
                    folder = new FolderNode(graph, name);
                }
                folder.SetParent(parent);
                parent = folder;
            }
            return parent;
        }
    }
}
