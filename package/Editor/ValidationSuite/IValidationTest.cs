
namespace UnityEditor.PackageManager.ValidationSuite
{
    internal enum TestCategory
    {
        DataValidation,
        ApiValidation,
        ContentScan,
        TestValidation,
    }

    internal interface IValidationTest
    {
        string TestName { get; }

        string TestDescription { get; }

        // Category mostly used for sorting tests, or grouping in UI.
        TestCategory TestCategory { get; }
    }
}