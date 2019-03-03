namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class LicenseValidation : BaseValidation
    {
        public LicenseValidation()
        {
            TestName = "License Validation";
            TestDescription = "Verify there is a license, and that its valid.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing, ValidationType.VerifiedSet };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.NotImplementedYet;

            // TODO: Check that the 3rd party notice file is not empty, if so, delete it.

            // TODO: check that the code doesn't have any copyright headers if the 3rd party notice file is empty.

            // TODO: Check that license.md exists, and that it has the right signature.
        }
    }
}
