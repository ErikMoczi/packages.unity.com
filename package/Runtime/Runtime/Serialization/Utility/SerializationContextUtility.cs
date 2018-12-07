using Unity.Properties;
using Unity.Tiny.Serialization;

#if UNITY_2018_3_OR_NEWER
#if (NET_4_6 || NET_STANDARD_2_0)

namespace Unity.Tiny.Attributes
{
    public static class SerializationContextUtility
    {
        public static bool NonSerializedInCurrentContext<TValue>(TValue target)
        {
            var context = SerializationContext.CurrentContext;
            
            switch (target)
            {
                case IProperty property when property.GetAttribute<NonSerializedInContext>()?.Context == context:
                case IAttributable attributable when attributable.GetAttribute<NonSerializedInContext>()?.Context == context:
                    return true;
            }
            
            return false;
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
#endif // UNITY_2018_3_OR_NEWER