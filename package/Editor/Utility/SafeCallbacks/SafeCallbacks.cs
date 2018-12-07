
using System;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class SafeCallbacks
    {
        public static void Invoke(Delegate del, params object[] args) => SafeInvoke(del, args);
        public static void Invoke<T1>(Action<T1> del, T1 t1) => SafeInvoke(del, t1);
        public static void Invoke<T1, T2>(Action<T1, T2> del, T1 t1, T2 t2) => SafeInvoke(del, t1, t2);
        public static void Invoke<T1, T2, T3>(Action<T1, T2> del, T1 t1, T2 t2, T3 t3) => SafeInvoke(del, t1, t2, t3);
        public static void Invoke<T1, T2, T3, T4>(Action<T1, T2> del, T1 t1, T2 t2, T3 t3, T4 t4) => SafeInvoke(del, t1, t2, t3, t4);

        /// <summary>
        /// By default, a delegate will call all of its invocation list in order, and will stop on the first exception
        /// encountered. When dealing with callbacks, we should try to avoid the situation where user code could kill our
        /// own processes. This method allows to do that without having to add a try/catch every time we invoke a callback.
        /// We can force the normal behaviour by throwing a <see cref="TinyForceCancellationException"/>.
        /// </summary>
        /// <param name="del">The delegate to call.</param>
        /// <param name="args">The arguments</param>
        private static void SafeInvoke(Delegate del, params object[] args)
        {
            if (null == del)
            {
                return;
            }

            foreach (var d in del.GetInvocationList())
            {
                try
                {
                    d.Method.Invoke(d.Target, args);
                }
                catch (TinyForceCancellationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
