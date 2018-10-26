namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class UpdateValidation : BaseValidation
    {
        public UpdateValidation()
        {
            TestName = "Package Update Validation";
            TestDescription = "If this is an update, validate that the package's metadata is correct.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.NotImplementedYet;

            // Version bump was done.
            // Does the version bump make sense, go from beta to alpha on a path update seems wrong?
            // Is this the first version of a package?  if so, it should be a pre-release
        }
    }
}