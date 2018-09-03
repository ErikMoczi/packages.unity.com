using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestRunner.TestLaunchers
{
    [Serializable]
    internal class RemoteTestData
    {
        public string id;
        public string name;
        public string fullName;
        public int testCaseCount;
        public bool hasChildren;
        public bool isSuite;
        public string[] childrenIds;
        public int testCaseTimeout;

        private RemoteTestData(ITest test)
        {
            id = test.Id;
            name = test.Name;
            fullName = test.FullName;
            testCaseCount = test.TestCaseCount;
            hasChildren = test.HasChildren;
            isSuite = test.IsSuite;
            childrenIds = test.Tests.Select(t => t.Id).ToArray();
        }

        internal static RemoteTestData[] GetTestDataList(ITest test)
        {
            var list = new List<RemoteTestData>();
            list.Add(new RemoteTestData(test));
            list.AddRange(test.Tests.SelectMany(GetTestDataList));
            return list.ToArray();
        }
    }
}
