using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEditor.TestTools
{
    /// <summary>
    /// Ignore attributes dedicated to Asset Import Pipeline backend version handling.
    /// </summary>
    internal static class AssetPipelineIgnore
    {
        private enum AssetPipelineBackend
        {
            V1,
            V2
        }

        private static readonly AssetPipelineBackend k_ActiveBackend = Environment.GetCommandLineArgs()
            .Any(arg => arg == "-assetpipelinev2")
            ? AssetPipelineBackend.V2
            : AssetPipelineBackend.V1;

        /// <summary>
        /// Ignore the test when running with the legacy Asset Import Pipeline V1 backend.
        /// </summary>
        internal class IgnoreInV1 : Attribute, ITestAction
        {
            private readonly AssetPipelineIgnoreBase m_Handler;

            public IgnoreInV1(string ignoreReason)
            {
                m_Handler = new AssetPipelineIgnoreBase(AssetPipelineBackend.V1, ignoreReason);
            }

            public void BeforeTest(ITest test) { m_Handler.IgnoreTestIfNecessary(); }
            public void AfterTest(ITest test) {}
            public ActionTargets Targets { get; protected set; }
        }

        /// <summary>
        /// Ignore the test when running with the latest Asset Import Pipeline V2 backend.
        /// </summary>
        internal class IgnoreInV2 : Attribute, ITestAction
        {
            private readonly AssetPipelineIgnoreBase m_Handler;

            public IgnoreInV2(string ignoreReason)
            {
                m_Handler = new AssetPipelineIgnoreBase(AssetPipelineBackend.V2, ignoreReason);
            }

            public void BeforeTest(ITest test) { m_Handler.IgnoreTestIfNecessary(); }
            public void AfterTest(ITest test) {}
            public ActionTargets Targets { get; protected set; }
        }

        private class AssetPipelineIgnoreBase
        {
            private readonly AssetPipelineBackend m_IgnoredBackend;
            private readonly string m_IgnoreReason;

            public AssetPipelineIgnoreBase(AssetPipelineBackend backend, string ignoreReason)
            {
                m_IgnoredBackend = backend;
                m_IgnoreReason = ignoreReason;
            }

            public void IgnoreTestIfNecessary()
            {
                if (k_ActiveBackend == m_IgnoredBackend)
                    Assert.Ignore(m_IgnoreReason);
            }
        }
    }
}
