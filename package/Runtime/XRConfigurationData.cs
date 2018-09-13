using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace UnityEngine.XR.Management
{
    /// <summary>
    /// This attribute is used to tag classes as providing build settings support for an XR SDK provider. The unified setting system
    /// will present the settings as an inspectable object in the Unified Settings window using the built-in inspector UI.
    ///
    /// The implementor of the settings is able to create their own custom UI and the Unified Settings system will use that UI in
    /// place of the build in inspector. See the <a href="https://docs.unity3d.com/Manual/ExtendingTheEditor.html">Extending the Editor</a>
    /// portion of the Unity documentation for information and instructions on doing this.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class XRConfigurationDataAttribute : Attribute
    {
        /// <summary>
        /// The display name to be presented to the user in the Unified Settings window.
        /// </summary>
        public string displayName { get; set; }

        /// <summary>
        /// The key that will be used to store the singleton instance of these settings within EditorBuildSettings.
        ///
        /// See <a href="https://docs.unity3d.com/ScriptReference/EditorBuildSettings.html">EditorBuildSettings</a> scripting
        /// API documentation on how this is beign done.
        /// </summary>
        public string buildSettingsKey { get; set; }

        private XRConfigurationDataAttribute() {}

        public XRConfigurationDataAttribute(string displayName, string buildSettingsKey)
        {
            this.displayName = displayName;
            this.buildSettingsKey = buildSettingsKey;
        }
    }
}
