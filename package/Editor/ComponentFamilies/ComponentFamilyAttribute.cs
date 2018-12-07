
using System.Linq;

using TypeRef = Unity.Tiny.TinyType.Reference;

namespace Unity.Tiny
{
    internal class ComponentFamilyAttribute : TinyAttribute
    {
        private readonly TinyId[] m_Required;
        private readonly TinyId[] m_Optional;

        public ComponentFamilyAttribute(string[] requiredGuids, string[] optionalGuids = null)
            : base(0)
        {
            m_Required = null != requiredGuids ? requiredGuids.Select(Convert).ToArray() : new TinyId[0];
            m_Optional = null != optionalGuids ? optionalGuids.Select(Convert).ToArray() : new TinyId[0];
        }

        public FamilyDefinition CreateFamilyDefinition(IRegistry registry)
        {
            return new FamilyDefinition(
                m_Required.Select(id => GetType(registry, id)).ToArray(),
                m_Optional.Select(id => GetType(registry, id)).ToArray()
            );
        }

        private static TinyId Convert(string guid) => new TinyId(guid);

        private static TinyType.Reference GetType(IRegistry registry, TinyId id)
        {
            return registry.FindById<TinyType>(id) is var type ? type.Ref : new TypeRef(id, "Unresolved name");
        }
    }
}
