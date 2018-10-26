namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class ProjectTemplateValidation : BaseValidation
    {
        public ProjectTemplateValidation()
        {
            TestName = "Project Template Validation";
            TestDescription = "Verify that the content of a project template meets expectations.";
            TestCategory = TestCategory.ContentScan;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            if (!Context.ProjectPackageInfo.IsProjectTemplate)
            {
                TestOutput.Add(Context.ProjectPackageInfo.name + " is not a project template.");
                TestState = TestState.NotRun;
                return;
            }

            var publishPackagePath = Context.PublishPackageInfo.path;
            // Start by declaring victory
            TestState = TestState.Succeeded;

            var outputLog = string.Empty;
            var errorLog = string.Empty; 
            if (!ProjectTemplateUtils.ValidateTemplatePackage(publishPackagePath, ref outputLog, ref errorLog)) 
            {
                if (!string.IsNullOrEmpty(outputLog))
                    TestOutput.Add(outputLog);
                Error(errorLog);
            }
        }
    }
}