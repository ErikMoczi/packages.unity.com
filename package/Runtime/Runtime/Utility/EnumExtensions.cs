using System;
using System.Collections.Generic;

namespace Unity.Tiny
{
    internal static class EnumExtensions
    {
        public static IEnumerable<TEnum> EnumerateFlags<TEnum>(this TEnum input)
            where TEnum : Enum
        {
            ulong flag = 0x1;
            
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                if (!input.HasFlag(value))
                {
                    continue;
                }
                
                var bits = Convert.ToUInt64(value);
                
                if (bits == 0L)
                {
                    continue; 
                }

                while (flag < bits)
                {
                    flag <<= 1;
                }

                if (flag == bits)
                {
                    yield return value;
                }
            }
        }
        
        public static IEnumerable<TEnum> EnumerateFlags<TEnum>()
            where TEnum : Enum
        {
            ulong flag = 0x1;
            
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                var bits = Convert.ToUInt64(value);
                
                if (bits == 0L)
                {
                    continue; 
                }

                while (flag < bits)
                {
                    flag <<= 1;
                }

                if (flag == bits)
                {
                    yield return value;
                }
            }
        }
    }
}