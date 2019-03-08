namespace Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using NUnit.Framework;
    using UnityEngine.TestTools;

    using UnityEditor;
    using UnityEditor.XR.MagicLeap;
    public class MagicLeapRemoteImportSupportTests
    {
        const string kImportTestPath = "Tests/MLRemoteImport";
        const string kSampleShimData = @"
# some comments
STUB_PATH=$(MLSDK)/lib/$(HOST)

# in older builds, ML Remote lives inside MLSDK
MLREMOTE_BASE_0.16.0=$(MLSDK)/VirtualDevice
MLREMOTE_BASE_0.17.0=$(MLSDK)/VirtualDevice
MLREMOTE_BASE_0.18.0=$(MLSDK)/VirtualDevice

# ML Remote is installed in parallel, using the same version as MLSDK
MLREMOTE_BASE_0.19.0=$(MLSDK)/../../MLRemote/v$(MLSDK_VERSION)

# select the appropriate base for the running version
MLREMOTE_BASE=$(MLREMOTE_BASE_$(MLSDK_VERSION))

ZI_SHIM_PATH_win64=$(MLREMOTE_BASE)/lib/win64;$(MLREMOTE_BASE)/bin;$(STUB_PATH)
ZI_SHIM_PATH_osx=$(MLREMOTE_BASE)/lib/osx;$(STUB_PATH)
ZI_SHIM_PATH_linux64=$(MLREMOTE_BASE)/lib/linux64;$(STUB_PATH)
";

        [Test]
        public void FailsIfTheLuminSDKIsMissing()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            Assert.That(MagicLeapRemoteImportSupport.sdkPath, Does.Exist);
        }

        [Test]
        public void FailsIfLuminSDKVersionIsInvalid()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            Assert.That(() => new Version(MagicLeapRemoteImportSupport.version), Throws.Nothing);
        }

        [Test]
        public void ExposesHostProperty()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            Assert.That(MagicLeapRemoteImportSupport.host, Is.Not.Null);
        }

        [Test]
        public void ExposesSDKPathProperty()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            Assert.That(MagicLeapRemoteImportSupport.sdkPath, Is.Not.Null);

        }

        [Test]
        public void ExposesVersionProperty()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            Assert.That(MagicLeapRemoteImportSupport.version, Is.Not.Null);
        }

        [Test]
        public void ShimPathsDiscoveryHasASearchPathForHost()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            if (!MagicLeapRemoteImportSupport.hasDiscoveryFile)
                Assert.Ignore("Shim discovery file not found, ignoring test");
            var paths = MagicLeapRemoteImportSupport.ParseDiscoveryData(File.ReadAllLines(Path.Combine(MagicLeapRemoteImportSupport.sdkPath, MagicLeapRemoteImportSupport.kShimDiscoveryPath))).ToArray();
            Assert.That(paths.Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void FallbackToDefaultShimLogicIfDiscoveryFileIsMissing()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            var paths = MagicLeapRemoteImportSupport.ParseDiscoveryData(MagicLeapRemoteImportSupport.discoveryFileOrFallbackData).ToArray();
            Assert.That(paths.Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void CanImport()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            using (var tp = new TemporaryAssetPath(kImportTestPath))
                Assert.That(() => MagicLeapRemoteImportSupport.ImportSupportLibrares(tp.path), Throws.Nothing);
        }

        [Test]
        public void AttemptingToImportOverExistingFilesThrowsException()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            using (var tp = new TemporaryAssetPath(kImportTestPath))
            {
                MagicLeapRemoteImportSupport.ImportSupportLibrares(tp.path);
                Assert.That(() => MagicLeapRemoteImportSupport.ImportSupportLibrares(tp.path), Throws.InstanceOf<ImportFailureException>());
            }
        }
    }

    class TemporaryAssetPath : IDisposable
    {
        internal string m_DeleteRoot = null;
        public string path { get; }

        public TemporaryAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (Path.IsPathRooted(path))
                throw new ArgumentException("Cannot be a rooted path", "path");
            if (Directory.Exists(path))
                throw new InvalidOperationException("Requested temporary path already exists!");
            this.path = Path.Combine("Assets", path);
            FindFirstParentDirectoryThatActuallyExists();
            Directory.CreateDirectory(this.path);
        }

        private void FindFirstParentDirectoryThatActuallyExists()
        {
            var p = path;
            var parent = Path.GetDirectoryName(p);
            while (parent != "Assets" || !Directory.Exists(parent))
            {
                p = parent;
                parent = Path.GetDirectoryName(p);
            }
            m_DeleteRoot = p;
        }

        private string MetaFileFor(string path)
        {
            return string.Format("{0}.meta", path.TrimEnd(new[] { '\\', '/' }));
        }

        void IDisposable.Dispose()
        {
            Directory.Delete(m_DeleteRoot, true);
            File.Delete(MetaFileFor(m_DeleteRoot));
            AssetDatabase.Refresh();
        }
    }

    public class TemporaryAssetPathTests
    {
        [Test]
        public void ProperlyCleansUpIntermediatePaths()
        {
            string path = "Some/Test/Path";
            var root = Path.Combine("Assets", "Some");
            var root_meta = Path.Combine("Assets", "Some.meta");
            using (new TemporaryAssetPath(path)) { }
            Assert.That(root, Does.Not.Exist);
            Assert.That(root_meta, Does.Not.Exist);
        }

        [Test]
        public void FindsCorrectDeleteRoot()
        {
            string path = "Some/Test/Path";
            var root = Path.Combine("Assets", "Some");
            using (var tp = new TemporaryAssetPath(path))
                Assert.That(tp.m_DeleteRoot, Is.EqualTo(root), () => string.Format("Expected: {0}, Actual: {1}", root, tp.m_DeleteRoot));
        }
    }

#if UNITY_EDITOR_OSX
    public class MacOSDependencyCheckerTests
    {
        const string kGraphicsLibPath = "{0}/VirtualDevice/lib/libml_graphics.dylib";
        string graphicsLib { get { return string.Format(kGraphicsLibPath, MagicLeapRemoteImportSupport.sdkPath); } }

        [Test]
        public void CanLocateTestLibrary()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            var lines = MacOSDependencyChecker.LaunchOtool(graphicsLib);
            Assert.That(lines, Is.Not.Null);
        }

        [Test]
        public void DependencyMapOfTestLibraryIsNotNull()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            var dm = MacOSDependencyChecker.GetDependencies(graphicsLib);
            Assert.That(dm, Is.Not.Null);
        }

        [Test]
        public void DependencyMapOfTestLibraryHasCorrectFilePath()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            var dm = MacOSDependencyChecker.GetDependencies(graphicsLib);
            Assert.That(dm.file, Is.EqualTo(graphicsLib));
        }

        [Test]
        public void DependencyMapOfTestLibraryHasDependencies()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            var dm = MacOSDependencyChecker.GetDependencies(graphicsLib);
            Func<string> errorFunc = () => {
                var sb = new StringBuilder();
                sb.AppendFormat("wrong number of deps for {0}", dm.file);
                sb.AppendLine();
                foreach (var item in dm.dependencies)
                {
                    sb.AppendFormat("item: {0}", item);
                    sb.AppendLine();
                }
                return sb.ToString();
            };
            Assert.That(dm.dependencies.Count, Is.GreaterThanOrEqualTo(4), errorFunc);
        }

        [Test]
        public void DependencyMapOfTestLibraryHasAllDependenciesExist()
        {
            if (string.IsNullOrEmpty(MagicLeapRemoteImportSupport.sdkPath))
                Assert.Ignore("Ignoring test because the environment is not properly configured");
            var dm = MacOSDependencyChecker.GetDependencies(graphicsLib);
            using (new WorkingDirectoryShift(Path.GetDirectoryName(graphicsLib)))
                Assert.That(dm.dependencies, Has.All.Exist);
        }
    }
#endif
}