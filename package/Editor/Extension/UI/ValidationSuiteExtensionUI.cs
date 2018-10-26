#if UNITY_2018_2_OR_NEWER

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.ValidationSuite.UI
{
    internal class ValidationSuiteExtensionUI : VisualElement
    {
        private const string PackagePath = "Packages/com.unity.package-validation-suite/";
        private const string ResourcesPath = PackagePath + "Editor/Resources/";
        private const string TemplatePath = ResourcesPath + "Templates/ValidationSuiteTools.uxml";
        private const string DarkStylePath = ResourcesPath + "Styles/Dark.uss";
        private const string LightStylePath = ResourcesPath + "Styles/Light.uss";

        private VisualElement root;

        private PackageInfo CurrentPackageinfo { get; set; }
        private string PackageId { get; set; }

        public ValidationSuiteExtensionUI()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath);
            if (asset == null)
            {
                Debug.LogError("Could not find asset \"" + TemplatePath + "\"");
                return;
            }

            root = asset.CloneTree(null);

            root.AddStyleSheetPath(EditorGUIUtility.isProSkin ? DarkStylePath : LightStylePath);
            Add(root);

            ValidateButton.clickable.clicked += Validate;
            ViewResultsButton.clickable.clicked += ViewResults;
            ViewDiffButton.clickable.clicked += ViewDiffs;
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (root == null)
                return;

            if (packageInfo == null)
            {
                AddToClassList("display-none");
                visible = false;
                return;
            }

            CurrentPackageinfo = packageInfo;
            PackageId = CurrentPackageinfo.name + "@" + CurrentPackageinfo.version;
            ValidationResults.text = string.Empty;
            ViewResultsButton.visible = ValidationSuiteReport.ReportExists(PackageId);
            ViewDiffButton.visible = ValidationSuiteReport.DiffsReportExists(PackageId);
            root.style.backgroundColor = Color.gray;
        }

        private void Validate()
        {
            if (root == null)
                return;

            if (CurrentPackageinfo.status != PackageStatus.Available || (CurrentPackageinfo.source != PackageSource.Embedded && CurrentPackageinfo.source != PackageSource.Registry))
            {
                EditorUtility.DisplayDialog("Validation Suite", "Install the package in your project to Validate", "Got it")  ;  
            }

            var results = ValidationSuite.RunValidationSuite(PackageId, CurrentPackageinfo.source == PackageSource.Embedded);
            ValidationResults.text = results ? "Success" : "Failed";
            ViewResultsButton.visible = ValidationSuiteReport.ReportExists(PackageId);
            ViewDiffButton.visible = ValidationSuiteReport.DiffsReportExists(PackageId);
            root.style.backgroundColor = results ? Color.green : Color.red;
        }

        private void ViewResults()
        {
            var filePath = ValidationSuiteReport.TextReportPath(PackageId);
            try
            {
                var data = File.ReadAllText(filePath);
                EditorUtility.DisplayDialog("Validation Results", data, "Ok");
            }
            catch (Exception)
            {
                EditorUtility.DisplayDialog("Validation Results", "Results are missing", "Ok");
            }

        }

        private void ViewDiffs()
        {
            if (ValidationSuiteReport.DiffsReportExists(PackageId))
            {
                Application.OpenURL("file://" + Path.GetFullPath(ValidationSuiteReport.DiffsReportPath(PackageId)));
            }
        }

        [MenuItem("internal:Project/Packages/Validate Packman UI")]
        internal static void ShowPackageManagerWindow()
        {
            ValidationSuite.RunValidationSuite(string.Format("{0}@{1}", "com.unity.package-manager-ui", "1.8.1"));
        }

        internal Label ValidationResults { get { return root.Q<Label>("validationResults");} }

        internal Button ValidateButton { get { return root.Q<Button>("validateButton"); } }

        internal Button ViewResultsButton { get { return root.Q<Button>("viewResults"); } }

        internal Button ViewDiffButton { get { return root.Q<Button>("viewdiff"); } }
    }
}

#else

namespace UnityEditor.PackageManager.ValidationSuite.UI
{
    internal class ValidationSuiteUI
    {
        [MenuItem("internal:Packages/Test Packman Validation")]
        internal static void TestPackmanValidation()
        {
            ValidationSuite.RunValidationSuite(string.Format("{0}@{1}", "com.unity.package-manager-ui", "1.8.1"));
        }

        [MenuItem("internal:Packages/Test AssetStore Validation")]
        internal static void TestAssetStoreValidation()
        {
            ValidationSuite.RunAssetStoreValidationSuite("Graph & Charts", "5.3", "data/pkg1", "data/pkg2");
        }
    }
}

#endif
