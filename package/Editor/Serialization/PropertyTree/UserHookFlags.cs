#if (NET_4_6 || NET_STANDARD_2_0)

using System;

namespace Unity.Properties.Editor.Serialization
{
    [Flags]
    public enum UserHookFlags
    {
        None = 1,
        OnPropertyBagConstructed = 2
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

#endif // (NET_4_6 || NET_STANDARD_2_0)
