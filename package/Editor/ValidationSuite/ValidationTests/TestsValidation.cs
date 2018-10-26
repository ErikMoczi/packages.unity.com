namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class TestsValidation : BaseValidation
    {
        public TestsValidation()
        {
            TestName = "Tests Validation";
            TestDescription = "Verify that the package has tests, and that test coverage is good.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.NotImplementedYet;

            // if the package has c# files, it should have tests.
            // Can we evaluate coverage imperically for now, until we have code coverage numbers?

        }
    }
}