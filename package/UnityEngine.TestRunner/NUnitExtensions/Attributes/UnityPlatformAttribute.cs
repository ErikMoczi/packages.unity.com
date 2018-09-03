using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class UnityPlatformAttribute : NUnitAttribute, IApplyToTest
    {
        public RuntimePlatform[] include { get; set; }
        public RuntimePlatform[] exclude { get; set; }

        private string m_skippedReason;

        public UnityPlatformAttribute()
        {
            include = new List<RuntimePlatform>().ToArray();
            exclude = new List<RuntimePlatform>().ToArray();
        }

        public UnityPlatformAttribute(params RuntimePlatform[] include)
            : this()
        {
            this.include = include;
        }

        public void ApplyToTest(Test test)
        {
            if (test.RunState == RunState.NotRunnable || test.RunState == RunState.Ignored || IsPlatformSupported())
            {
                return;
            }
            test.RunState = RunState.Skipped;
            test.Properties.Add("_SKIPREASON", m_skippedReason);
        }

        private bool IsPlatformSupported()
        {
            if (include.Any() && !include.Any(x => x == Application.platform))
            {
                m_skippedReason = string.Format("Only supported on {0}", string.Join(", ", include.Select(x => x.ToString()).ToArray()));
                return false;
            }

            if (exclude.Any(x => x == Application.platform))
            {
                m_skippedReason = string.Format("Not supported on  {0}", string.Join(", ", exclude.Select(x => x.ToString()).ToArray()));
                return false;
            }
            return true;
        }
    }
}
