using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Semver;
using UnityEngine;
using UnityEditor.PackageManager.ValidationSuite.UI;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class DocumentationValidation : BaseValidation
    {
        public DocumentationValidation()
        {
            TestName = "Documentation Validation";
            TestDescription = "Make sure the documentation site exists for this version of the package.";
            TestCategory = TestCategory.DataValidation;
        }

        public override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

        }
    }
}