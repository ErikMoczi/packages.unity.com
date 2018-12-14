using Unity.Properties;
using Unity.Tiny.Serialization;

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