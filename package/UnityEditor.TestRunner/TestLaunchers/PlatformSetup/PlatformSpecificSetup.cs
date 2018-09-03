using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.TestTools.TestRunner
{
    [Serializable]
    internal class PlatformSpecificSetup
    {
        [SerializeField]
        private ApplePlatformSetup m_AppleiOSPlatformSetup = new ApplePlatformSetup(BuildTarget.iOS);
        [SerializeField]
        private ApplePlatformSetup m_AppleTvOSPlatformSetup = new ApplePlatformSetup(BuildTarget.tvOS);
        [SerializeField]
        private XboxOnePlatformSetup m_XboxOnePlatformSetup = new XboxOnePlatformSetup();
        [SerializeField]
        private AndroidPlatformSetup m_AndroidPlatformSetup = new AndroidPlatformSetup();

        [SerializeField]
        private UwpPlatformSetup m_UwpPlatformSetup = new UwpPlatformSetup();


        private IDictionary<BuildTarget, IPlatformSetup> m_SetupTypes;

        [SerializeField]
        private BuildTarget m_Target;

        public PlatformSpecificSetup()
        {
        }

        public PlatformSpecificSetup(BuildTarget target)
        {
            m_Target = target;
        }

        public void Setup()
        {
            var dictionary = GetSetup();

            if (!dictionary.ContainsKey(m_Target))
            {
                return;
            }

            dictionary[m_Target].Setup();
        }

        public void PostBuildAction()
        {
            var dictionary = GetSetup();

            if (!dictionary.ContainsKey(m_Target))
            {
                return;
            }

            dictionary[m_Target].PostBuildAction();
        }

        public void PostSuccessfulBuildAction()
        {
            var dictionary = GetSetup();

            if (!dictionary.ContainsKey(m_Target))
            {
                return;
            }

            dictionary[m_Target].PostSuccessfulBuildAction();
        }

        public void CleanUp()
        {
            var dictionary = GetSetup();

            if (!dictionary.ContainsKey(m_Target))
            {
                return;
            }

            dictionary[m_Target].CleanUp();
        }

        private IDictionary<BuildTarget, IPlatformSetup> GetSetup()
        {
            m_SetupTypes = new Dictionary<BuildTarget, IPlatformSetup>()
            {
                {BuildTarget.iOS, m_AppleiOSPlatformSetup},
                {BuildTarget.tvOS, m_AppleTvOSPlatformSetup},
                {BuildTarget.XboxOne, m_XboxOnePlatformSetup},
                {BuildTarget.Android, m_AndroidPlatformSetup},
                {BuildTarget.WSAPlayer, m_UwpPlatformSetup}
            };
            return m_SetupTypes;
        }
    }
}
