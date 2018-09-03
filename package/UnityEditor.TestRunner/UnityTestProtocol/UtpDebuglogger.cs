using UnityEngine;

namespace UnityEditor.TestTools.TestRunner.UnityTestProtocol
{
    class UtpDebugLogger : IUtpLogger
    {
        public void Log(Message msg)
        {
            var msgJson = JsonUtility.ToJson(msg);
            Debug.Log("\n##utp:" + msgJson);
        }
    }
}
