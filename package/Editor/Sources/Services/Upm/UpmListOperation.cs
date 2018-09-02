using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{
    internal class UpmListOperation : UpmBaseOperation, IListOperation
    {
        [SerializeField]
        private Action<IEnumerable<PackageInfo>> _doneCallbackAction;

        public void GetPackageListAsync(Action<IEnumerable<PackageInfo>> doneCallbackAction, Action<Error> errorCallbackAction = null)
        {
            this._doneCallbackAction = doneCallbackAction;
            OnOperationError += errorCallbackAction;
            
            Start();
        }

        protected override Request CreateRequest()
        {
            return Client.List();            
        }

        protected override void ProcessData()
        {
            var request = CurrentRequest as ListRequest;
            var packages = new List<PackageInfo>();
            foreach (var upmPackage in request.Result)
            {
                var packageInfos = FromUpmPackageInfo(upmPackage);
                packages.AddRange(packageInfos);
            }

            _doneCallbackAction(packages);
        }
    }
}