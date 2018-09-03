#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using Object = UnityEngine.Object;

namespace Unity.Properties
{
    public static class TypeConversion
    {
        public static TValue Convert<TValue>(object v)
        {
            if (ReferenceEquals(v, null))
            {
                return default(TValue);
            }
            
            if (v is TValue)
            {
                return (TValue) v;
            }

            var uObj = v as Object;
            if (uObj != null)
            {
                // handle fake nulls
                if (!uObj)
                {
                    return default(TValue);
                }
            }
            
            if (typeof(TValue).IsEnum)
            {
                var s = v as string;
                if (s != null)
                {
                    return (TValue) Enum.Parse(typeof(TValue), s);
                }
                
                // Try to convert to the underlying type
                var enumValue = System.Convert.ChangeType(v, Enum.GetUnderlyingType(typeof(TValue)));
                return (TValue) enumValue;
            }

            return (TValue) System.Convert.ChangeType(v, typeof(TValue));
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)

