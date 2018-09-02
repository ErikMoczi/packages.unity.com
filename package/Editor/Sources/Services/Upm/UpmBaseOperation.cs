using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{    
    internal abstract class UpmBaseOperation : IBaseOperation
    {
        public static string GroupName(OriginType origin)
        {
            var group = PackageGroupOrigins.Packages.ToString();
            // MOCKED
            //if (origin == OriginType.Builtin)
            if (origin == OriginType.Path)
                group = PackageGroupOrigins.Modules.ToString();

            return group;
        }

        protected static IEnumerable<PackageInfo> FromUpmPackageInfo(UpmPackageInfo info, bool isCurrent=true)
        {
            var packages = new List<PackageInfo>();
            var displayName = info.displayName;
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = info.name.Replace("com.unity.modules.", "");
                displayName = displayName.Replace("com.unity.", "");
                displayName = new CultureInfo("en-US").TextInfo.ToTitleCase(displayName);
            }

            // MOCK
            // var state = info.version == info.latestVersion ?  PackageState.UpToDate : PackageState.Outdated,
            var state = PackageState.UpToDate;
            if (info.errors.Length > 0)
                state = PackageState.Error;

            packages.Add(new PackageInfo()
                {
                    Name = info.name,
                    DisplayName = displayName,
                    PackageId = info.packageId.ToLower(),
                    Tag = info.tag,
                    Version = info.version,
                    Description = info.description,
                    Category = info.category,
                    IsCurrent = isCurrent,
                    Errors = info.errors.ToList(),
                    
                    // MOCK
                    // Group = GroupName(info.originType),
                    Group = GroupName(info.name.StartsWith("com.unity.modules.") ? OriginType.Path : OriginType.Registry),

                    State = state,
                    
                    // MOCK TO REMOVE
                    // OriginType = info.originType                    
                    OriginType = info.name.StartsWith("com.unity.modules.") ? OriginType.Path : OriginType.Registry
                }
            );

            /* Mock remove this
            foreach(var version in info.versions)
            {
                if (version != info.version.ToString())
                {
                    packages.Add(new PackageInfo()
                        {
                            Name = info.name,
                            DisplayName = displayName,
                            PackageId = info.packageId.ToLower(),
                            Version = version,
                            Tag = info.tag,
                            Category = info.category,
                            IsCurrent = false,
                            State = info.version == info.latestVersion ?  PackageState.UpToDate : PackageState.Outdated,
                            Group = GroupName(info.originType),
                            OriginType = info.originType
                        }
                    );   
                }
            }
            */



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
