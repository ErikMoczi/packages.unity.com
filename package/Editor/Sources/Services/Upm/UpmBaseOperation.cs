using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Semver;
using UnityEngine;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{    
    internal abstract class UpmBaseOperation : IBaseOperation
    {
        public static string GroupName(OriginType origin)
        {
            var group = PackageGroupOrigins.Packages.ToString();
            if (origin == OriginType.Builtin)
                group = PackageGroupOrigins.BuiltInPackages.ToString();

            return group;
        }

        public static bool GetIsRecommended(string version, string upmRecommended, bool isVersionCurrent)
        {
            SemVersion semVersion = version;
            var isPreRelease = !string.IsNullOrEmpty(semVersion.Prerelease);
                
            bool isRecommended = version == upmRecommended;
                
            // Your are the recommended if you also satisfy all these conditions:
            //     - If your are not alpha/beta
            //     - Your current major/minor version is the same as the recommended major/minor version from upm request
            //     - Your patch version is >= then the recommended from upm request
            if (!isRecommended && isVersionCurrent)
            {
                // In some cases, such as built-in packages, the recommended version will be empty,
                // so in these cases keep isRecommended to false.
                SemVersion semRecommended;
                if (SemVersion.TryParse(upmRecommended, out semRecommended))
                    if (!isPreRelease)
                        if (semVersion.Major == semRecommended.Major &&
                            semVersion.Minor == semRecommended.Minor &&
                            semVersion.Patch >= semRecommended.Patch)
                            isRecommended = true;
            }

            return isRecommended;
        }

        protected static IEnumerable<PackageInfo> FromUpmPackageInfo(PackageManager.PackageInfo info, bool isCurrent=true)
        {
            var packages = new List<PackageInfo>();
            var displayName = info.displayName;
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = info.name.Replace("com.unity.modules.", "");
                displayName = displayName.Replace("com.unity.", "");
                displayName = new CultureInfo("en-US").TextInfo.ToTitleCase(displayName);
            }

            var versions = new List<string>();
            versions.AddRange(info.versions.compatible);
            if (versions.All(version => version != info.version))
            {
                versions.Add(info.version);
            }
                        
            foreach(var version in versions)
            {
                bool isVersionCurrent = version == info.version && isCurrent;
                bool isRecommended = GetIsRecommended(version, info.versions.recommended, isVersionCurrent);

                var state = (info.originType == OriginType.Builtin || info.version == info.versions.latestCompatible) ? PackageState.UpToDate : PackageState.Outdated;
                
                // Happens mostly when using a package that hasn't been in production yet.
                if (info.versions.all.Length <= 0)
                    state = PackageState.UpToDate;
                
                if (info.errors.Length > 0)
                    state = PackageState.Error;

                var packageInfo = new PackageInfo
                {
                    Name = info.name,
                    DisplayName = displayName,
                    PackageId = version == info.version ? info.packageId : null,
                    Version = version,
                    Description = info.description,
                    Category = info.category,
                    IsCurrent = isVersionCurrent,
                    IsLatest = version == info.versions.latestCompatible,
                    IsRecommended = isRecommended,
                    Errors = info.errors.ToList(),
                    Group = GroupName(info.originType),
                    State = state,
                    OriginType = info.originType
                };
                
                packages.Add(packageInfo);
            }

            return packages;
        }
        
        public static event Action<UpmBaseOperation> OnOperationStart = delegate { };

        public event Action<Error> OnOperationError = delegate { };
        public event Action OnOperationFinalized = delegate { };
        
        public Error ForceError { get; set; }                // Allow external component to force an error on the requests (eg: testing)
        public Error Error { get; protected set; }        // Keep last error

        protected abstract Request CreateRequest();
        
        [SerializeField]
        protected Request CurrentRequest;
        public readonly ThreadedDelay Delay = new ThreadedDelay();

        protected abstract void ProcessData();

        protected void Start()
        {
            Error = null;
            OnOperationStart(this);
            Delay.Start();

            if (TryForcedError())
                return;

            EditorApplication.update += Progress;
        }

        // Common progress code for all classes
        private void Progress()
        {
            if (!Delay.IsDone)
                return;

            // Create the request after the delay
            if (CurrentRequest == null)
            {
                CurrentRequest = CreateRequest();
            }
            
            // Since CurrentRequest's error property is private, we need to simulate
            // an error instead of just setting it.
            if (TryForcedError())
                return;
            
            if (CurrentRequest.IsCompleted)
            {
                if (CurrentRequest.Status == StatusCode.Success)
                    OnDone();
                else if (CurrentRequest.Status >= StatusCode.Failure)
                    OnError(CurrentRequest.Error);
                else
                    Debug.LogError("Unsupported progress state " + CurrentRequest.Status);
            }
        }

        private void OnError(Error error)
        {
            Error = error;

            Debug.LogError("Cannot perform upm operation: " + Error.message + " [" + Error.errorCode + "]");

            OnOperationError(Error);
            FinalizeOperation();
        }

        private void OnDone()
        {
            ProcessData();
            FinalizeOperation();
        }

        private void FinalizeOperation()
        {
            EditorApplication.update -= Progress;
            OnOperationFinalized();
        }

        public void Cancel()
        {
            EditorApplication.update -= Progress;
            OnOperationError = delegate { };
            OnOperationFinalized = delegate { };
        }

        private bool TryForcedError()
        {
            if (ForceError != null)
            {
                OnError(ForceError);
                return true;
            }

            return false;
        }
    }
}
