using System;

using UnityEngine;
using UnityEngine.XR.Management;

namespace ManagementTests.Runtime
{
    [XRConfigurationData("Dummy Runtime Tests", "com.unity.xr.management.tests.DummyRuntime")]
    public class DummyRuntimeSettings : ScriptableObject
    {
        [SerializeField]
        public bool m_IsGood = false;
    }
}
