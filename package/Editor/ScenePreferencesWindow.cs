﻿using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ProGrids.Editor
{
	class ScenePreferencesWindow : EditorWindow
	{
		internal ProGridsEditor editor;

		GUIContent m_PredictiveGrid = new GUIContent("Predictive Grid", "If enabled, the grid will automatically render at the optimal axis based on movement.");
		GUIContent m_SnapAsGroup = new GUIContent("Snap as Group", "If enabled, selected objects will keep their relative offsets when moving.  If disabled, every object in the selection is snapped to grid independently.");

		void OnGUI()
		{
			if (editor == null)
			{
				editor = ProGridsEditor.Instance;

				if (editor == null)
				{
					Close();
					return;
				}
			}

			GUILayout.Label("Snap Settings", EditorStyles.boldLabel);

			float snap = editor.SnapValueInGridUnits * editor.SnapModifier;

			EditorGUI.BeginChangeCheck();

			snap = EditorGUILayout.FloatField("Snap Value", snap);

			if(EditorGUI.EndChangeCheck())
				editor.SnapValueInGridUnits = snap / editor.SnapModifier;

			editor.ScaleSnapEnabled = EditorGUILayout.Toggle("Snap On Scale", editor.ScaleSnapEnabled);


			bool snapAsGroup = editor.SnapAsGroupEnabled;
			snapAsGroup = EditorGUILayout.Toggle(m_SnapAsGroup, snapAsGroup);
			if(snapAsGroup != editor.SnapAsGroupEnabled)
				editor.SnapAsGroupEnabled = snapAsGroup;

			EditorGUI.BeginChangeCheck();

			EditorGUI.BeginChangeCheck();
			editor.AngleValue = EditorGUILayout.Slider("Angle", editor.AngleValue, 0f, 180f);
			if(EditorGUI.EndChangeCheck())
				SceneView.RepaintAll();

			bool tmp = editor.PredictiveGrid;
			tmp = EditorGUILayout.Toggle(m_PredictiveGrid, tmp);
			if( tmp != editor.PredictiveGrid )
			{
				editor.PredictiveGrid = tmp;
				EditorPrefs.SetBool(PreferenceKeys.PredictiveGrid, tmp);
			}

			GUILayout.FlexibleSpace();

			if( GUILayout.Button("Done"))
				Close();
		}
	}

}
