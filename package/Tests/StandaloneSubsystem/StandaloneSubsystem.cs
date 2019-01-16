using System;

using UnityEngine;
using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;

namespace Unity.XR.Management.Tests.Standalone
{
    public class StandaloneSubsystem : Subsystem
    {
        public event Action startCalled;
        public event Action stopCalled;
        public event Action destroyCalled;

        public override void Start()
        {
            if (startCalled != null)
                startCalled.Invoke();
        }

        public override void Stop()
        {
            if (stopCalled != null)
                stopCalled.Invoke();
        }

        public override void Destroy()
        {
            if (destroyCalled != null)
                destroyCalled.Invoke();
        }
    }
}