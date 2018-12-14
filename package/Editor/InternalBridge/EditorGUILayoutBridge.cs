
using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
	internal static class EditorGUILayoutBridge
	{
		public static Gradient GradientField(string label, Gradient gradient, params GUILayoutOption[] options)
		{
			return EditorGUILayout.GradientField(label, gradient, options);
		}

		public static Gradient GradientField(GUIContent label, Gradient gradient, params GUILayoutOption[] options)
		{
			return EditorGUILayout.GradientField(label, gradient, options);
		}
	}
}
