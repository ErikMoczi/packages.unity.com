namespace Unity.Tiny
{
    internal static class TinyEntityGroupExtensions
    {
        public static TinyEntity CreateEntity(this TinyEntityGroup group, string name)
        {
            var registry = group.Registry;
            var entity = registry.CreateEntity(TinyId.New(), name);
            group.AddEntityReference(entity.Ref);
            return entity;
        }
    }
}