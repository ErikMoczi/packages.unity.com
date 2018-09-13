using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;


namespace UnityEditor.XR.Management
{
	public abstract class XRBuildData : ScriptableObject {
        public abstract string Name { get; }
	}

}
