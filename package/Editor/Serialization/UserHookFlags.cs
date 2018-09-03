#if NET_4_6
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Properties.Serialization;

namespace Unity.Properties.Editor.Serialization
{
    [Flags]
    public enum UserHookFlags
    {
        None = 1,
        OnPropertyConstructed = 2
    }

    public static class UserHooks
    {
        public static UserHookFlags From(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return UserHookFlags.None;
            }
            return (UserHookFlags)Enum.Parse(typeof(UserHookFlags), s);
        }
    }
}
#endif // NET_4_6
