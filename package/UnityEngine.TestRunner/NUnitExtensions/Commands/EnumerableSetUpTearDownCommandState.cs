using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace UnityEngine.TestTools
{
    internal class EnumerableSetUpTearDownCommandState : ScriptableObject
    {
        public int NextSetUpStepIndex;
        public int NextSetUpStepPc;
        public int NextTearDownStepIndex;
        public int NextTearDownStepPc;
        public bool TestHasRun;
        public TestStatus CurrentTestResultStatus;
        public string CurrentTestResultLabel;
        public FailureSite CurrentTestResultSite;
        public string CurrentTestMessage;
        public string CurrentTestStrackTrace;
        public bool TestTearDownStarted;

        public void Reset()
        {
            NextSetUpStepIndex = 0;
            NextSetUpStepPc = 0;
            NextTearDownStepIndex = 0;
            NextTearDownStepPc = 0;
            TestHasRun = false;
            CurrentTestResultStatus = TestStatus.Inconclusive;
            CurrentTestResultLabel = null;
            CurrentTestResultSite = default(FailureSite);
            CurrentTestMessage = null;
            CurrentTestStrackTrace = null;
            TestTearDownStarted = false;
        }
    }
}
