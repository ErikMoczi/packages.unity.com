using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class TinyBuildReportProvider : SettingsProvider
    {
        public TinyBuildReportProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope)
        {
        }

        [SettingsProvider]
        public static SettingsProvider Provider()
        {
            return new TinyBuildReportProvider("Project/Tiny/Build Report") {label = "Build Report"};
        }

        [TinyInitializeOnLoad, UsedImplicitly]
        private static void RegisterCallbacks()
        {
            TinyEditorApplication.OnLoadProject += OnLoadProject;
            TinyEditorApplication.OnCloseProject += OnCloseProject;
        }

        private TinyBuildReportPanel m_BuildReportPanel;

        private static TinyContext s_Context;
        private int m_ModuleVersion;
        private int m_WorkspaceVersion;

        [SerializeField] private TinyBuildReportPanel.State m_BuildReportState = new TinyBuildReportPanel.State();

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            CreateBuildReport();
        }

        public override void OnDeactivate()
        {
            m_BuildReportPanel = null;
        }

        public override void OnGUI(string searchContext)
        {
            if (null == s_Context)
            {
                EditorGUILayout.LabelField("No Tiny context is currently opened.");
                return;
            }

            if (null == m_BuildReportPanel)
            {
                CreateBuildReport();
            }

            var oldEnabled = GUI.enabled;
            GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;
            try
            {
                m_BuildReportPanel?.DrawLayout();
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                TinyEditorAnalytics.SendExceptionOnce("Editor.OnGUI", e);
                throw;
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }
        }

        private static void OnLoadProject(TinyProject project, TinyContext context)
        {
            s_Context = context;
        }

        private static void OnCloseProject(TinyProject project, TinyContext context)
        {
            s_Context = null;
        }

        private void CreateBuildReport()
        {
            if (null == s_Context)
            {
                return;
            }

            var project = s_Context.Registry.FindAllByType<TinyProject>().FirstOrDefault();
            m_BuildReportPanel = new TinyBuildReportPanel(project.Registry, project.Module, m_BuildReportState);
        }
    }
}

