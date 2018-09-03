using System;
using System.IO;
using System.Text;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.TestRunner.GUI;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.TestRunner.TestLaunchers;
using UnityEngine.TestTools.TestRunner;
using UnityEngine.TestTools.TestRunner.GUI;

namespace UnityEditor.TestTools.TestRunner
{
    [Serializable]
    internal class RemoteTestResultReciever
    {
        [SerializeField]
        private bool m_AllTestsRanSuccessfull;

        public RemoteTestResultReciever()
        {
            PlayerResultWindowUpdater.instance.ResetTestState();
        }

        public void RunStarted(MessageEventArgs messageEventArgs)
        {
            m_AllTestsRanSuccessfull = true;
        }

        public void RunFinished(MessageEventArgs messageEventArgs)
        {
            EditorConnection.instance.Send(PlayerConnectionMessageIds.runFinishedMessageId, null, messageEventArgs.playerId);
            EditorConnection.instance.DisconnectAll();
        }

        public void ReceivedTestsData(MessageEventArgs messageEventArgs)
        {
            var testResult = Deserialize<TestRunnerResult>(messageEventArgs.data);
            PlayerResultWindowUpdater.instance.TestDone(testResult);
            m_AllTestsRanSuccessfull &= testResult.resultStatus != TestRunnerResult.ResultStatus.Failed;
        }

        private T Deserialize<T>(byte[] data)
        {
            return JsonUtility.FromJson<T>(Encoding.UTF8.GetString(data));
        }
    }
}
