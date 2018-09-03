using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor.IMGUI.Controls;
using UnityEngine.TestRunner.NUnitExtensions;
using UnityEngine.TestTools.TestRunner.GUI;
using UnityEngine.TestRunner.NUnitExtensions.Filters;

namespace UnityEditor.TestTools.TestRunner.GUI
{
    internal class TestTreeViewBuilder
    {
        public List<TestRunnerResult> results = new List<TestRunnerResult>();
        private readonly List<TestRunnerResult> m_OldTestResultList;
        private readonly TestRunnerUIFilter m_UIFilter;
        private readonly ITest m_TestListRoot;

        private readonly List<string> m_AvailableCategories = new List<string>();

        public string[] AvailableCategories
        {
            get { return m_AvailableCategories.Distinct().OrderBy(a => a).ToArray(); }
        }

        public TestTreeViewBuilder(ITest tests, List<TestRunnerResult> oldTestResultResults, TestRunnerUIFilter uiFilter)
        {
            m_AvailableCategories.Add(CategoryFilterExtended.k_DefaultCategory);
            m_OldTestResultList = oldTestResultResults;
            m_TestListRoot = tests;
            m_UIFilter = uiFilter;
        }

        public TreeViewItem BuildTreeView(TestFilterSettings settings, bool sceneBased, string sceneName)
        {
            var rootItem = new TreeViewItem(int.MaxValue, 0, null, "Invisible Root Item");
            m_TestListRoot.ParseForNameDuplicates();
            ParseTestTree(0, rootItem, m_TestListRoot);
            return rootItem;
        }

        private bool IsFilteredOutByUIFilter(ITest test, TestRunnerResult result)
        {
            if (m_UIFilter.PassedHidden && result.resultStatus == TestRunnerResult.ResultStatus.Passed)
                return true;
            if (m_UIFilter.FailedHidden && (result.resultStatus == TestRunnerResult.ResultStatus.Failed || result.resultStatus == TestRunnerResult.ResultStatus.Inconclusive))
                return true;
            if (m_UIFilter.NotRunHidden && (result.resultStatus == TestRunnerResult.ResultStatus.NotRun || result.resultStatus == TestRunnerResult.ResultStatus.Skipped))
                return true;
            if (m_UIFilter.CategoryFilter.Length > 0)
                return !test.HasCategory(m_UIFilter.CategoryFilter);
            return false;
        }

        private void ParseTestTree(int depth, TreeViewItem rootItem, ITest testElement)
        {
            m_AvailableCategories.AddRange(testElement.GetAllCategoriesFromTest());

            var testElementId = TestRunnerResult.GetId(testElement);
            if (testElement is TestMethod)
            {
                var result = m_OldTestResultList.FirstOrDefault(a => a.id == testElementId);

                if (result != null &&
                    (result.ignoredOrSkipped
                     || result.notRunnable
                     || testElement.RunState == RunState.NotRunnable
                     || testElement.RunState == RunState.Ignored
                     || testElement.RunState == RunState.Skipped))
                {
                    //if the test was or becomes ignored or not runnable, we recreate the result in case it has changed
                    result = null;
                }
                if (result == null)
                {
                    result = new TestRunnerResult(testElement);
                }
                results.Add(result);

                var test = new TestTreeViewItem((Test)testElement, depth, rootItem);
                if (!IsFilteredOutByUIFilter(testElement, result))
                    rootItem.AddChild(test);
                test.SetResult(result);
                return;
            }

            var groupResult = m_OldTestResultList.FirstOrDefault(a => a.id == testElementId);
            if (groupResult == null)
            {
                groupResult = new TestRunnerResult(testElement);
            }

            results.Add(groupResult);
            var group = new TestTreeViewItem((Test)testElement, depth, rootItem);
            group.SetResult(groupResult);

            depth++;
            foreach (var child in testElement.Tests)
            {
                ParseTestTree(depth, group, child);
            }

            if (testElement is TestAssembly && !testElement.HasChildren)
                return;

            if (group.hasChildren)
                rootItem.AddChild(group);
        }
    }
}
