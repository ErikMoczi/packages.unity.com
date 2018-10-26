namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class LicenseValidation : BaseValidation
    {
        public LicenseValidation()
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

            // Check that the 3rd party notice file is not empty, if so, delete it.

            // check that the code doesn't have any copyright headers if the 3rd party notice file is empty.

            // Check that license.md exists, and that it has the right signature.
        }
    }
}