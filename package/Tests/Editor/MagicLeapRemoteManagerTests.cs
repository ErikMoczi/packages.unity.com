namespace Tests
{
    using System.Runtime.InteropServices;
    using NUnit.Framework;
    using UnityEngine.TestTools;

    using UnityEditor.XR.MagicLeap.Remote;

#if UNITY_EDITOR && PLATFORM_LUMIN
    public class  MagicLeapRemoteManagerTests
    {
        static class Native
        {
            [DllImport("ml_platform")]
            public static extern int MLPlatformGetAPILevel(ref uint platformLevel);
            [DllImport("ml_remote")]
            public static extern int MLRemoteIsServerConfigured([MarshalAs(UnmanagedType.U1)] ref bool isConfigured);
        }
        [Test]
        public void CanResolveMLGraphicsLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_graphics", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanResolveMLPerceptionLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_perception_client", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanResolveMLInputLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_input", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanResolveMLRemoteLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_remote", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanResolveMLPlatformLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_platform", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanResolveMLLifecycleLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_lifecycle", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanResolveMLAudioLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_audio", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanResolveMLGraphicsUtilsLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_graphics_utils", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanResolveMLIdentityLibrary()
        {
            string path;
            Assert.That(MagicLeapRemoteManager.TryResolveMLPluginPath("ml_identity", out path), Is.True);
            Assert.That(path, Is.Not.Null);
            Assert.That(path.EndsWith(MagicLeapRemoteManager.hostExtension), Is.True);
        }

        [Test]
        public void CanLoadAndCallIntoMLPlatformLibrary()
        {
            uint level = 0;
            var result = Native.MLPlatformGetAPILevel(ref level);
            Assert.That(result, Is.EqualTo(0));
            Assert.That(level, Is.Not.EqualTo(0));
        }

        [Test]
        public void CanLoadAndCallIntoMLRemoteLibrary()
        {
            bool configured = false;
            var result = Native.MLRemoteIsServerConfigured(ref configured);
            Assert.That(result, Is.EqualTo(0));
        }
    }
#endif // UNITY_EDITOR && PLATFORM_LUMIN
}