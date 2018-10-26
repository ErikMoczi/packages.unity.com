namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class LicenseEvaluation : BaseValidation
    {
        public LicenseEvaluation()
        {
            TestName = "License Validation";
            TestDescription = "Verify there is a license, and that its valid.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.NotImplementedYet;
        }
    }
}