using System;
using UnityEngine.TestTools.TestRunner.GUI;

namespace UnityEngine.TestTools.TestRunner
{
    [Serializable]
    internal class PlaymodeTestsControllerSettings
    {
        [SerializeField]
        public TestRunnerFilter filter;
        public bool sceneBased;
        public string originalScene;
        public string bootstrapScene;
        public bool displayTestResultUi;

        public static PlaymodeTestsControllerSettings CreateRunnerSettings(TestRunnerFilter filter)
        {
            var settings = new PlaymodeTestsControllerSettings
            {
                filter = filter,
                sceneBased = false,
                originalScene = null,
                bootstrapScene = null,
                displayTestResultUi = false,
            };
            return settings;
        }
    }
}
