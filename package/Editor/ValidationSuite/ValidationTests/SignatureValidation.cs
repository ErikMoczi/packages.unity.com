namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class SignatureValidation : BaseValidation
    {
        public SignatureValidation()
        {
            TestName = "Signature Validation";
            TestDescription = "Verify that the package has a valid signature associated with it.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.NotImplementedYet;

            // TODO: THIS CAN ONLY BE TURNED ON ONCE WE HAVE PACKAGE SIGNATURE WORKING END TO END.

        }
    }
}