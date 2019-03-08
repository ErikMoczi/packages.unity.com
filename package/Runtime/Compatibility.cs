using System;
#if !NETFX_CORE || !NET_4_6 || !NET_STANDARD_2_0
namespace UnityEngine.XR.MagicLeap.Compatibility
{
    internal class Lazy<T>
    {
        private T @value = default(T);
        private bool _Resolved = false;

        private Func<T> _Resolver = new Func<T>(() => (T)Activator.CreateInstance(typeof(T)));

        public Lazy() {}

        public Lazy(Func<T> func)
        {
            _Resolver = func;
        }

        public T Value
        {
            get
            {
                if (!_Resolved)
                {
                    lock (this)
                    {
                        // double test _Resolved because multiple threads might hit the lock before resolution.
                        if (!_Resolved)
                        {
                            @value = _Resolver();
                            _Resolved = true;
                        }
                    }
                }
            return @value;
            }

        }
    }
}
#endif
