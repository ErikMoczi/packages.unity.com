

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal static class EnumUtility
    {
        public static IEnumerable<TEnum> EnumValues<TEnum>()
            where TEnum : struct, IConvertible
        {
            var type = typeof(TEnum);
            Assert.IsTrue(type.IsEnum);
            return Enum.GetValues(type).Cast<TEnum>();
        }
    }
}

