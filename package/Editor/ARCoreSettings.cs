using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.XR.ARCore
{

    /// <summary>
    /// Holds settings that are used to configure the Unity ARKit Plugin.
    /// </summary>
    [CreateAssetMenu(fileName = "ARCoreSettings", menuName = "ARCore/Settings", order = 1)]
    public class ARCoreSettings : ScriptableObject
    {
        public enum Requirement
        {
            Required,
            Optional
        }

        /// <summary>
        /// Toggles whether ARKit is required for this app: will make app only downloadable by devices with ARKit support if enabled.
        /// </summary>
        [Tooltip("Toggles whether ARCore is required for this app. Will make app only downloadable by devices with ARCore support if enabled.")]
        public Requirement requirment;
    }
}