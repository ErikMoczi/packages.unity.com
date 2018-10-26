#if UNITY_2018_2_OR_NEWER

using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Debug = UnityEngine.Debug;

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

        public static ValidationSuiteExtensionUI CreateUI()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath);
            return asset == null ? null : new ValidationSuiteExtensionUI(asset);
        }

        private ValidationSuiteExtensionUI(VisualTreeAsset asset)
        {

            root = asset.CloneTree(null);

            root.AddStyleSheetPath(EditorGUIUtility.isProSkin ? DarkStylePath : LightStylePath);
            Add(root);

            ValidateButton.clickable.clicked += Validate;
            ViewResultsButton.clickable.clicked += ViewResults;
            ViewDiffButton.clickable.clicked += ViewDiffs;
        }

        public static bool SourceSupported(PackageSource source)
        {
            return source == PackageSource.Embedded || source == PackageSource.Local || source == PackageSource.Registry;
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (root == null)
                return;

            var showValidationUI = packageInfo != null && packageInfo.status == PackageStatus.Available && SourceSupported(packageInfo.source);
            UIUtils.SetElementDisplay(this, showValidationUI);
            if (!showValidationUI)
                return;

            CurrentPackageinfo = packageInfo;
            PackageId = CurrentPackageinfo.name + "@" + CurrentPackageinfo.version;
            ValidationResults.text = string.Empty;

            UIUtils.SetElementDisplay(ViewResultsButton, ValidationSuiteReport.ReportExists(PackageId));
            UIUtils.SetElementDisplay(ViewDiffButton, ValidationSuiteReport.DiffsReportExists(PackageId));

            root.style.backgroundColor = Color.gray;
        }

        private void Validate()
        {
            if (root == null)
                return;

            if (Utilities.NetworkNotReachable)
            {
                EditorUtility.DisplayDialog("", "Validation suite requires network access and cannot be used offline.", "Ok");
                return;
            }

            var results = ValidationSuite.RunValidationSuite(PackageId, CurrentPackageinfo.source);
            ValidationResults.text = results ? "Success" : "Failed";
            UIUtils.SetElementDisplay(ViewResultsButton, ValidationSuiteReport.ReportExists(PackageId));
            UIUtils.SetElementDisplay(ViewDiffButton, ValidationSuiteReport.DiffsReportExists(PackageId));
            root.style.backgroundColor = results ? Color.green : Color.red;
        }

        private void ViewResults()
        {
            var filePath = ValidationSuiteReport.TextReportPath(PackageId);
            try
            {
                try
                {
                    var targetFile = Directory.GetCurrentDirectory() + "/" + filePath;
                    if (!File.Exists(targetFile))
                        throw new Exception("Validation Result not found!");
                    
                    Process.Start(targetFile);
                }
                catch (Exception)
                {
                    var data = File.ReadAllText(filePath);
                    EditorUtility.DisplayDialog("Validation Results", data, "Ok");
                }
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
#if UNITY_2018_2_OR_NEWER
        [MenuItem("internal:Packages/Test Packman Validation")]
        internal static void TestPackmanValidation()
        {
            ValidationSuite.RunValidationSuite(string.Format("{0}@{1}", "com.unity.package-manager-ui", "1.8.1"));
        }
#endif

        [MenuItem("internal:Packages/Test AssetStore Validation")]
        internal static void TestAssetStoreValidation()
        {
            ValidationSuite.RunAssetStoreValidationSuite("Graph - Charts", "5.3", "data/pkg1", "data/pkg2");
        }

        [MenuItem("internal:Packages/Test AssetStore Validation no Previous")]
        internal static void TestAssetStoreValidationNoPrevious()
        {
            ValidationSuite.RunAssetStoreValidationSuite("Graph - Charts", "5.3", "data/pkg1");
        }
    }
}

#endif
