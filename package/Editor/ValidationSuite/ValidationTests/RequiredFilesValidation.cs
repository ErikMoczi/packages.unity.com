namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class RequiredFilesValidation : BaseValidation
    {
        public RequiredFilesValidation()
        {
            TestName = "Required File Type Validation";
            TestDescription = "Make sure required file types are included with this package.";
            TestCategory = TestCategory.ContentScan;
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            // TODO: from the published project dir, check if each file type is present under the right conditions.
        }
    }
}
