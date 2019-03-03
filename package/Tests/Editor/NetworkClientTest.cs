using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

[TestFixture]
public class NetworkClientTest
{
    private NetworkClient m_Client;
    private static string s_LatestLogMessage;
    private int m_ReceivedMessages;

    static void HandleLog(string logString, string stackTrace, LogType type)
    {
        s_LatestLogMessage = type + ": " + logString + "\n" + stackTrace;
    }

    [SetUp]
    public void Setup()
    {
        Application.logMessageReceived += HandleLog;
        m_ReceivedMessages = 0;
    }

    [TearDown]
    public void Teardown()
    {
        Application.logMessageReceived -= HandleLog;
    }

    [Test]
    public void DisconnectWithoutConnectedConnection()
    {
        m_Client = new NetworkClient(new NetworkConnection());
        m_Client.Disconnect();
        Assert.AreEqual(null, s_LatestLogMessage);
    }
}
