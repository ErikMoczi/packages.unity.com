using UnityEngine;
using System;

namespace ProGrids.Runtime
{
	/// <summary>
	/// Tells ProGrids to skip snapping on this object.
	/// </summary>
	/// <remarks>
	/// On Unity versions less than 5.2 this will not take effect until after a script reload.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class ProGridsNoSnapAttribute : Attribute
	{
	}

	/// <summary>
	/// Tells ProGrids to check `bool IsSnapEnabled()` function on this object.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class ProGridsConditionalSnapAttribute : Attribute
	{
	}
}
