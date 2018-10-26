using System.Linq;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class DependenciesValidation : BaseValidation
    {
        public DependenciesValidation()
        {
            TestName = "Dependency Validation";
            TestDescription = "Verify that dependencies of this package make sense.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            // Package dependencies dont conflict with dependencies from verified packages (ERROR)
            foreach (var dependency in Context.ProjectPackageInfo.dependencies)
            {
                if (Context.PackageCoDependencies.ContainsKey(dependency.Key))
                {
                    var verifiedDependencies = Context.PackageCoDependencies[dependency.Key].Where(p => p.ParentIsVerified);
                    foreach (var verifiedDependency in verifiedDependencies)
                    {
                        var vExisting = Semver.SemVersion.Parse(verifiedDependency.DependencyVersion);
                        var vNew = Semver.SemVersion.Parse(dependency.Value);

                        if (vExisting.Major != vNew.Major)
                        {
                            Error("Package dependency {0}@{1} will create a major conflict with verified package {2}:{3}", dependency.Key, dependency.Value, verifiedDependency.ParentName, verifiedDependency.ParentVersion);
                        }
                    }
                }
            }

            // TODO: Package depenedencies dont conflict with dependencies from non-verified packages (WARNING)

            // TODO: Package itself doesn't introduce new conflicting versions of itself if it is a dependency for the verified set. (WARNING)

            // TODO: List what templates depend on this package.
        }
    }
}