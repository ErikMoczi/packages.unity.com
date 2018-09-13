using UnityEngine;
using UnityEditor.XR.Management;

namespace UnityEngine.XR.Management.Sample
{
    [XRConfigurationData("Sample Settings", SampleConstants.kSettingsKey)]
    [System.Serializable]
    public class SampleSettings : ScriptableObject
    {
         public enum Requirement
         {
             Required,
             Optional,
             None
         }

         [SerializeField, Tooltip("Changes item requirement.")]
         Requirement m_RequiresItem;

         public Requirement RequiresItem
         {
             get { return m_RequiresItem; }
             set { m_RequiresItem = value; }
         }

         [SerializeField, Tooltip("Some toggle for runtime.")]
         bool m_RuntimeToggle = true;

         public bool RuntimeToggle
         {
            get { return m_RuntimeToggle; }
            set { m_RuntimeToggle = value; }
         }
    }
}
