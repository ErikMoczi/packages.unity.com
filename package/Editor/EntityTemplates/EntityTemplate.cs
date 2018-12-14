


using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal sealed class EntityTemplate
    {
        public readonly TinyId[] Ids;

        public EntityTemplate(params TinyId[] ids)
        {
            Assert.IsNotNull(ids);
            Ids = ids;
        }
    }
}


