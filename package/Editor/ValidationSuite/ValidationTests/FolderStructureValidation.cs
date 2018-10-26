using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class FolderStructureValidation : BaseValidation
    {
        // Move code that validates that development is happening on the right version based on the package.json
        public FolderStructureValidation()
        {
            TestName = "Folder Structure Validation";
            TestDescription = "Verify that the folder structure meets expectations.";
            TestCategory = TestCategory.ContentScan;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            TestState = TestState.NotImplementedYet;
        }
    }
}
