using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class FolderStructureValidation : BaseValidation
    {
        public FolderStructureValidation()
        {
            TestName = "Folder Structure Validation";
            TestDescription = "Verify that the folder structure meets expectations.";
            TestCategory = TestCategory.ContentScan;
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing, ValidationType.VerifiedSet };
        }

        protected override void Run()
        {
            TestState = TestState.NotImplementedYet;

            // TODO: Write test around expected structure.
        }
    }
}
