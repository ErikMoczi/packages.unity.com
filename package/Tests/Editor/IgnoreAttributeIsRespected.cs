using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using ProGrids.Runtime;
using UObject = UnityEngine.Object;

namespace ProGrids.Editor.Tests
{
	public class IgnoreAttributeIsRespected
	{
		[Test]
		public void GameObjectWithIgnoreAttribIsNotSnapped()
		{
			var go = new GameObject();
			Assert.IsTrue(EditorUtility.SnapIsEnabled(go.transform));
			EditorUtility.ClearSnapEnabledCache();
			go.AddComponent<IgnoreSnap>();
			Assert.IsFalse(EditorUtility.SnapIsEnabled(go.transform));
			UObject.DestroyImmediate(go);
		}
	}
}
