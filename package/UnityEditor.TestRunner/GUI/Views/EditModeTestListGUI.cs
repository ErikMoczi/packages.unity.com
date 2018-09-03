using System;
using System.Linq;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.TestRunner.GUI;

namespace UnityEditor.TestTools.TestRunner.GUI
{
    [Serializable]
    internal class EditModeTestListGUI : TestListGUI
    {
        public override void RenderNoTestsInfo()
        {
            if (!TestListGUIHelper.SelectedFolderContainsTestAssembly())
            {
                var noTestText = "No tests to show";

                if (!PlayerSettings.playModeTestRunnerEnabled)
                {
                    const string testsArePulledFromCustomAssemblies =
                        "EditMode tests can be in Editor only Assemblies, either in the editor special folder or Editor only Assembly Definitions with added Unity References \"Test Assemblies\".";
                    noTestText += Environment.NewLine + testsArePulledFromCustomAssemblies;
                }

                EditorGUILayout.HelpBox(noTestText, MessageType.Info);
                if (GUILayout.Button("Create EditMode Test Assembly Folder"))
                {
                    TestListGUIHelper.AddFolderAndAsmDefForTesting(isEditorOnly: true);
                }
            }

            if (!TestListGUIHelper.CanAddEditModeTestScriptAndItWillCompile())
            {
                UnityEngine.GUI.enabled = false;
                EditorGUILayout.HelpBox("EditMode test scripts can only be created in editor test assemblies.", MessageType.Warning);
            }
            if (GUILayout.Button("Create Test Script in current folder"))
            {
                TestListGUIHelper.AddTest();
            }
            UnityEngine.GUI.enabled = true;
        }

        public override void PrintHeadPanel()
        {
            base.PrintHeadPanel();
            DrawFilters();
        }

        protected override void RunTests(TestRunnerFilter filter)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("Fix compilation issues before running tests");
                return;
            }

            filter.ClearResults(newResultList);

            var testExecutor = new EditModeLauncher(filter, TestPlatform.EditMode);
            testExecutor.AddEventHandler<WindowResultUpdater>();
            testExecutor.Run();
        }

        public override ITest GetTestListNUnit()
        {
            var testAssemblyProvider = new EditorLoadedTestAssemblyProvider(new EditorCompilationInterfaceProxy(), new EditorAssembliesProxy());
            var assemblies = testAssemblyProvider.GetAssembliesGroupedByType(TestPlatform.EditMode);
            return TestAssemblyHelper.BuildTests(TestPlatform.EditMode, assemblies.Select(x => x.Assembly).ToArray());
        }

        protected override bool IsBusy()
        {
            return EditModeLauncher.IsRunning || EditorApplication.isCompiling;
        }
    }
}
