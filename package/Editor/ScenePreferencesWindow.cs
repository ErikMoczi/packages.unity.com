using UnityEngine;
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
			GUILayout.Label("Snap Settings", EditorStyles.boldLabel);

			float snap = editor.GetSnapIncrement();

			EditorGUI.BeginChangeCheck();

			snap = EditorGUILayout.FloatField("Snap Value", snap);

			if(EditorGUI.EndChangeCheck())
				editor.SetSnapIncrement(snap);

			EditorGUI.BeginChangeCheck();
			int majorLineIncrement = EditorPrefs.GetInt(PreferenceKeys.MajorLineIncrement, 10);
			majorLineIncrement = EditorGUILayout.IntField("Major Line Increment", majorLineIncrement);
			majorLineIncrement = majorLineIncrement < 2 ? 2 : majorLineIncrement > 128 ? 128 : majorLineIncrement;
			if(EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetInt(PreferenceKeys.MajorLineIncrement, majorLineIncrement);
				GridRenderer.majorLineIncrement = majorLineIncrement;
				ProGridsEditor.DoGridRepaint();
			}

			editor.ScaleSnapEnabled = EditorGUILayout.Toggle("Snap On Scale", editor.ScaleSnapEnabled);

			SnapUnit _gridUnits = (SnapUnit)(EditorPrefs.HasKey(PreferenceKeys.GridUnit) ? EditorPrefs.GetInt(PreferenceKeys.GridUnit) : 0);

			bool snapAsGroup = editor.snapAsGroup;
			snapAsGroup = EditorGUILayout.Toggle(m_SnapAsGroup, snapAsGroup);
			if(snapAsGroup != editor.snapAsGroup)
				editor.snapAsGroup = snapAsGroup;

			EditorGUI.BeginChangeCheck();

			_gridUnits = (SnapUnit)EditorGUILayout.EnumPopup("Grid Units", _gridUnits);

			EditorGUI.BeginChangeCheck();
			editor.angleValue = EditorGUILayout.Slider("Angle", editor.angleValue, 0f, 180f);
			if(EditorGUI.EndChangeCheck())
				SceneView.RepaintAll();

			if( EditorGUI.EndChangeCheck() )
			{
				EditorPrefs.SetInt(PreferenceKeys.GridUnit, (int) _gridUnits);
				editor.LoadPreferences();
			}

			bool tmp = editor.predictiveGrid;
			tmp = EditorGUILayout.Toggle(m_PredictiveGrid, tmp);
			if( tmp != editor.predictiveGrid )
			{
				editor.predictiveGrid = tmp;
				EditorPrefs.SetBool(PreferenceKeys.PredictiveGrid, tmp);
			}

			GUILayout.FlexibleSpace();

			if( GUILayout.Button("Done"))
				this.Close();
		}
	}

}
