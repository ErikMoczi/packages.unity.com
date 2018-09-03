using System;

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

            if (v is UnityEngine.Object)
            {
                // handle fake nulls
                var uObj = (UnityEngine.Object)v;
                if (!uObj)
                {
                    return default(TValue);
                }
            }
            
            if (v is TValue)
            {
                return (TValue) v;
            }

            return (TValue) System.Convert.ChangeType(v, typeof(TValue));
        }
    }
}