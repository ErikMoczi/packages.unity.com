namespace Unity.Tiny
{
    internal static class TinyRegistryExtensions
    {
        internal static TinyModule CreateModule(this IRegistry registry, string name)
        {
            return registry.CreateModule(TinyId.Generate(name), name);
        }
    }
}