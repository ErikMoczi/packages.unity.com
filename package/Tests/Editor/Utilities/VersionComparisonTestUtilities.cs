using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    public static class VersionComparisonTestUtilities
    {
        public static IEnumerable<TestCaseData> FailsInPatch()
        {
            yield return new TestCaseData(ReleaseType.Patch, true);//.SetName("FailsInPatch");
            yield return new TestCaseData(ReleaseType.Minor, false);//.SetName("SucceedsInMinor");
            yield return new TestCaseData(ReleaseType.Major, false);//.SetName("SucceedsInMajor");
        }

        public static IEnumerable<TestCaseData> FailsInMinor()
        {
            yield return new TestCaseData(ReleaseType.Patch, true);//.SetName("FailsInPatch");
            yield return new TestCaseData(ReleaseType.Minor, true);//.SetName("FailsInMinor");
            yield return new TestCaseData(ReleaseType.Major, false);//.SetName("SucceedsInMajor");
        }

        public static IEnumerable<TestCaseData> PassesAlways()
        {
            yield return new TestCaseData(ReleaseType.Patch, false);//.SetName("SucceedsInPatch");
            yield return new TestCaseData(ReleaseType.Minor, false);//.SetName("SucceedsInMinor");
            yield return new TestCaseData(ReleaseType.Major, false);//.SetName("SucceedsInMajor");
        }

        public static IEnumerable<TestCaseData> PassesNever()
        {
            yield return new TestCaseData(ReleaseType.Patch, true);//.SetName("FailsInPatch");
            yield return new TestCaseData(ReleaseType.Minor, true);//.SetName("SucceedsInMinor");
            yield return new TestCaseData(ReleaseType.Major, true);//.SetName("SucceedsInMajor");
        }

        public static string VersionForReleaseType(ReleaseType releaseType)
        {
            switch (releaseType)
            {
                case ReleaseType.Patch: return "0.0.2-preview";
                case ReleaseType.Minor: return "0.1.0-preview";
                case ReleaseType.Major: return "1.0.0";
                default: throw new NotSupportedException();
            }
        }
    }
}