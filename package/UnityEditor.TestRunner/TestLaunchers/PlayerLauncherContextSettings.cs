using System;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityEditor.TestTools.TestRunner
{
    internal class PlayerLauncherContextSettings : IDisposable
    {
        private ITestRunSettings m_OverloadSettings;

        private EditorBuildSettingsScene[] m_EditorBuildSettings;
        private ResolutionDialogSetting m_DisplayResolutionDialog;
        private bool m_RunInBackground;
        private FullScreenMode m_FullScreenMode;
        private bool m_ResizableWindow;
        private bool m_ShowUnitySplashScreen;
        private string m_OldproductName;
        private string m_OldAotOptions;
        private Lightmapping.GIWorkflowMode m_OldLightmapping;
        private bool m_explicitNullChecks;

        private bool m_Disposed;

        public PlayerLauncherContextSettings(ITestRunSettings overloadSettings)
        {
            m_OverloadSettings = overloadSettings;
            SetupProjectParameters();

            if (overloadSettings != null)
            {
                overloadSettings.Apply();
            }
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                CleanupProjectParameters();
                if (m_OverloadSettings != null)
                {
                    m_OverloadSettings.Dispose();
                }

                m_Disposed = true;
            }
        }

        private void SetupProjectParameters()
        {
            EditorApplication.LockReloadAssemblies();

            m_EditorBuildSettings = EditorBuildSettings.scenes;

            m_DisplayResolutionDialog = PlayerSettings.displayResolutionDialog;
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;

            m_RunInBackground = PlayerSettings.runInBackground;
            PlayerSettings.runInBackground = true;

            m_FullScreenMode = PlayerSettings.fullScreenMode;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

            m_OldAotOptions = PlayerSettings.aotOptions;
            PlayerSettings.aotOptions = "nimt-trampolines=1024";

            m_ResizableWindow = PlayerSettings.resizableWindow;
            PlayerSettings.resizableWindow = true;

            m_ShowUnitySplashScreen = PlayerSettings.SplashScreen.show;
            PlayerSettings.SplashScreen.show = false;

            m_OldproductName = PlayerSettings.productName;
            PlayerSettings.productName = "UnityTestFramework";

            m_OldLightmapping = Lightmapping.giWorkflowMode;
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;

            m_explicitNullChecks = EditorUserBuildSettings.explicitNullChecks;
            EditorUserBuildSettings.explicitNullChecks = true;
        }

        private void CleanupProjectParameters()
        {
            EditorBuildSettings.scenes = m_EditorBuildSettings;

            PlayerSettings.fullScreenMode = m_FullScreenMode;
            PlayerSettings.runInBackground = m_RunInBackground;
            PlayerSettings.displayResolutionDialog = m_DisplayResolutionDialog;
            PlayerSettings.resizableWindow = m_ResizableWindow;
            PlayerSettings.SplashScreen.show = m_ShowUnitySplashScreen;
            PlayerSettings.productName = m_OldproductName;
            PlayerSettings.aotOptions = m_OldAotOptions;
            Lightmapping.giWorkflowMode = m_OldLightmapping;
            EditorUserBuildSettings.explicitNullChecks = m_explicitNullChecks;

            EditorApplication.UnlockReloadAssemblies();
        }
    }
}
