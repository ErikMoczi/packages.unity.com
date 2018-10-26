using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Semver;
using UnityEngine;
using UnityEditor.PackageManager.ValidationSuite.UI;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class DiffEvaluation : BaseValidation
    {
        public DiffEvaluation()
        {
            TestName = "Package Diff Evaluation";
            TestDescription = "Produces a report of what's been changed in this version of the package.";
            TestCategory = TestCategory.DataValidation;
        }

        public override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            // Flag certain file types are requiring special attention.
            // Asmdef - can cause breaks on client's updates to packages.
            // package.json - Will change infomation in UI
            //      - Diff actual file, report what changed...
            // Meta files - if all meta files have changed, that's a red flag
            // if there are no common files, all files have changed,
        }
    }
}