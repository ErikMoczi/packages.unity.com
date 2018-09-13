using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;

namespace ManagementTests.Runtime {

    public class DummyLoader : XRLoader {
        private bool m_ShouldFail = false;

        public DummyLoader (bool shouldFail = false) {
            m_ShouldFail = shouldFail;
        }

        public override bool Initialize () {
            return m_ShouldFail;
        }

        public override T GetLoadedSubsystem<T>()
        {
            return default(T);
        }

    }

}
