using UnityEngine;
using UnityEngine.XR.Management;

namespace ManagementTests.Runtime {

    internal class DummyLoader : XRLoader {
        public bool m_ShouldFail = false;
        public int m_Id;

        public DummyLoader ()
        {
        }

        public override bool Initialize () {
            return !m_ShouldFail;
        }

        public override T GetLoadedSubsystem<T>()
        {
            return default(T);
        }

        protected bool Equals(DummyLoader other)
        {
            return base.Equals(other) && m_ShouldFail == other.m_ShouldFail && m_Id == other.m_Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DummyLoader) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ m_ShouldFail.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Id;
                return hashCode;
            }
        }
    }

}
