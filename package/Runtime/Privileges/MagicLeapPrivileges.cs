using System.Collections;
using UnityEngine;

#if PLATFORM_LUMIN
using System.Runtime.InteropServices;
#endif // PLATFORM_LUMIN

namespace UnityEngine.XR.MagicLeap
{
    public static class MagicLeapPrivileges
    {
        enum ResultCode : uint
        {
            Ok = 0,
            UnspecifiedFailure = 4,
            InvalidParameter = 5,
            NotImplemented = 8,
            Granted = 0xcbcd,
            Denied = Granted + 1
        }
        static class Native
        {
#if PLATFORM_LUMIN
            const string Library = "ml_privileges";
            [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MLPrivilegesStartup")]
            public static extern ResultCode Startup();

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MLPrivilegesShutdown")]
            public static extern ResultCode Shutdown();

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MLPrivilegesCheckPrivilege")]
            public static extern ResultCode CheckPrivilege(uint privilegeId);

            [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MLPrivilegesRequestPrivilege")]
            public static extern ResultCode RequestPrivilege(uint privilegeId);
#else
            public static ResultCode Startup() { return ResultCode.Ok; }
            public static ResultCode Shutdown() { return ResultCode.Ok; }
            public static ResultCode CheckPrivilege(uint privilegeId) { return (privilegeId != 0) ? ResultCode.Granted : ResultCode.Denied; }
            public static ResultCode RequestPrivilege(uint privilegeId) { return (privilegeId != 0) ? ResultCode.Granted : ResultCode.Denied; }
#endif // PLATFORM_LUMIN
        }

        private static uint s_RefCount = 0;

        private static bool isInitialized() => s_RefCount >= 1;

        internal static void Initialize()
        {
            // TODO :: Need to enfore access on main thread only..
            s_RefCount++;
            if (s_RefCount != 1)
                return;
            Native.Startup();
            // on app shutdown, we're tearing the world down anyways, so futzing with the ref count is okay.
            Application.quitting += () => { s_RefCount = 1; Shutdown(); };
        }

        internal static void Shutdown()
        {
            // TODO :: Need to enfore access on main thread only..
            s_RefCount--;
            if (s_RefCount != 0)
                return;
            Application.quitting -= Shutdown;
            Native.Shutdown();
        }

        public static bool IsPrivilegeApproved(uint privilegeId)
        {
            if (!isInitialized()) return false;
            return Native.CheckPrivilege(privilegeId) == ResultCode.Granted;
        }

        public static bool RequestPrivilege(uint privilegeId)
        {
            if (!isInitialized()) return false;
            return Native.RequestPrivilege(privilegeId) == ResultCode.Granted;
        }
    }

    // Helper class for pausing script execution until
    // a specific permission is available.
    internal sealed class WaitForPrivilege : IEnumerator
    {
        private readonly uint m_PrivilegeId;
        private readonly System.Func<IEnumerator> m_YieldFunc;
        public WaitForPrivilege(uint privilegeId) : this(privilegeId, null)
        {
        }

        public WaitForPrivilege(uint privilegeId, System.Func<IEnumerator> yieldFunc)
        {
            m_PrivilegeId = privilegeId;
            m_YieldFunc = yieldFunc;
        }
        object IEnumerator.Current => (m_YieldFunc != null) ? m_YieldFunc() : null;

        bool IEnumerator.MoveNext() => !MagicLeapPrivileges.IsPrivilegeApproved(m_PrivilegeId);

        void IEnumerator.Reset() {}
    }
}