
using System.Linq;

namespace Unity.Tiny
{
    internal class ComponentFamilyHiddenAttribute : TinyAttribute
    {
        private readonly TinyId[] m_Hidden;

        public TinyId[] Hidden => m_Hidden;

        public ComponentFamilyHiddenAttribute(string[] skip)
            : base(0)
        {
            m_Hidden = null != skip ? skip.Select(Convert).ToArray() : new TinyId[0];
        }

        private static TinyId Convert(string guid) => new TinyId(guid);
    }
}
