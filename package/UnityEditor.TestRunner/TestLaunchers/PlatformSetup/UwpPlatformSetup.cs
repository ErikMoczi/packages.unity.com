using System;

namespace UnityEditor.TestTools.TestRunner
{
    internal class UwpPlatformSetup : IPlatformSetup
    {
        private const string k_SettingsBuildConfiguration = "BuildConfiguration";
        private bool m_InternetClientServer;
        private bool m_PrivateNetworkClientServer;

        public void Setup()
        {
            m_InternetClientServer = PlayerSettings.WSA.GetCapability(PlayerSettings.WSACapability.InternetClientServer);
            m_PrivateNetworkClientServer = PlayerSettings.WSA.GetCapability(PlayerSettings.WSACapability.PrivateNetworkClientServer);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClientServer, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.PrivateNetworkClientServer, true);

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNITY_THISISABUILDMACHINE")))
            {
                EditorUserBuildSettings.wsaSubtarget = WSASubtarget.PC;
                EditorUserBuildSettings.SetPlatformSettings(BuildPipeline.GetBuildTargetName(BuildTarget.WSAPlayer), k_SettingsBuildConfiguration, WSABuildType.Debug.ToString());
            }
        }

        public void PostBuildAction()
        {
        }

        public void PostSuccessfulBuildAction()
        {
        }

        public void CleanUp()
        {
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClientServer, m_InternetClientServer);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.PrivateNetworkClientServer, m_PrivateNetworkClientServer);
        }
    }
}
