
using System.Linq;
using TypeRef = Unity.Tiny.TinyType.Reference;

namespace Unity.Tiny
{
    internal class FamilyDefinition
    {
        public readonly TypeRef[] Required;
        public readonly TypeRef[] Optional;
        public readonly TypeRef[] All;

        public FamilyDefinition(TypeRef[] required, TypeRef[] optional = null)
        {
            Required = required;
            Optional = optional ?? new TypeRef[0];
            All = Required.Concat(Optional).ToArray();
        }

    }
}
