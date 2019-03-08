using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARCore.Tests
{
    [TestFixture]
    public class ARCoreTestFixture
    {
        [Test]
        public void DepthSubsystemRegistered()
        {
            Assert.That(SubsystemDescriptorRegistered<XRDepthSubsystemDescriptor>("ARCore-Depth"));
        }

        [Test]
        public void SessionSubsystemRegistered()
        {
            Assert.That(SubsystemDescriptorRegistered<XRSessionSubsystemDescriptor>("ARCore-Session"));
        }

        [Test]
        public void PlaneSubsystemRegistered()
        {
            Assert.That(SubsystemDescriptorRegistered<XRPlaneSubsystemDescriptor>("ARCore-Plane"));
        }

        [Test]
        public void RaycastSubsystemRegistered()
        {
            Assert.That(SubsystemDescriptorRegistered<XRRaycastSubsystemDescriptor>("ARCore-Raycast"));
        }
        [Test]
        public void ReferencePointSubsystemRegistered()
        {
            Assert.That(SubsystemDescriptorRegistered<XRReferencePointSubsystemDescriptor>("ARCore-ReferencePoint"));
        }

        [Test]
        public void CameraSubsystemRegistered()
        {
            Assert.That(SubsystemDescriptorRegistered<XRCameraSubsystemDescriptor>("ARCore-Camera"));
        }
        bool SubsystemDescriptorRegistered<T>(string id) where T : SubsystemDescriptor
        {
            List<T> descriptors = new List<T>();

            SubsystemManager.GetSubsystemDescriptors<T>(descriptors);

            foreach(T descriptor in descriptors)
            {
                if (descriptor.id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}