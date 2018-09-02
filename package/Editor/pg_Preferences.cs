using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ProGrids.Editor
{
	static class pg_Preferences
	{
		static Color s_GridColorX;
		static Color s_GridColorY;
		static Color s_GridColorZ;
		static float s_AlphaBump;
		static bool s_ScaleSnapEnabled;
		static SnapMethod s_SnapMethod;
		static float s_BracketIncreaseValue;
		static SnapUnit s_GridUnits;
		static bool s_SyncUnitySnap;

		static KeyCode s_IncreaseGridSize = KeyCode.Equals;
		static KeyCode s_DecreaseGridSize = KeyCode.Minus;
		static KeyCode s_NudgePerspectiveBackward = KeyCode.LeftBracket;
		static KeyCode s_NudgePerspectiveForward = KeyCode.RightBracket;
		static KeyCode s_NudgePerspectiveReset = KeyCode.Alpha0;
		static KeyCode s_CyclePerspective = KeyCode.Backslash;

		static bool s_PrefsLoaded = false;

		[PreferenceItem("ProGrids")]
		public static void PreferencesGUI()
		{
			if (!s_PrefsLoaded)
				s_PrefsLoaded = LoadPreferences();

			EditorGUI.BeginChangeCheck();

			GUILayout.Label("Snap Behavior", EditorStyles.boldLabel);
			s_AlphaBump = EditorGUILayout.Slider(new GUIContent("Tenth Line Alpha", "Every 10th line will have it's alpha value bumped by this amount."), s_AlphaBump, 0f, 1f);
			s_GridUnits = (SnapUnit)EditorGUILayout.EnumPopup("Grid Units", s_GridUnits);
			s_ScaleSnapEnabled = EditorGUILayout.Toggle("Snap On Scale", s_ScaleSnapEnabled);
			s_SnapMethod = (SnapMethod) EditorGUILayout.EnumPopup("Snap Method", s_SnapMethod);
			s_SyncUnitySnap = EditorGUILayout.Toggle("Sync w/ Unity Snap", s_SyncUnitySnap);

			GUILayout.Label("Grid Colors", EditorStyles.boldLabel);
			s_GridColorX = EditorGUILayout.ColorField("X Axis", s_GridColorX);
			s_GridColorY = EditorGUILayout.ColorField("Y Axis", s_GridColorY);
			s_GridColorZ = EditorGUILayout.ColorField("Z Axis", s_GridColorZ);

			GUILayout.Label("Shortcuts", EditorStyles.boldLabel);
			s_IncreaseGridSize = (KeyCode)EditorGUILayout.EnumPopup("Increase Grid Size", s_IncreaseGridSize);
			s_DecreaseGridSize = (KeyCode)EditorGUILayout.EnumPopup("Decrease Grid Size", s_DecreaseGridSize);
			s_NudgePerspectiveBackward = (KeyCode)EditorGUILayout.EnumPopup("Nudge Perspective Backward", s_NudgePerspectiveBackward);
			s_NudgePerspectiveForward = (KeyCode)EditorGUILayout.EnumPopup("Nudge Perspective Forward", s_NudgePerspectiveForward);
			s_NudgePerspectiveReset = (KeyCode)EditorGUILayout.EnumPopup("Nudge Perspective Reset", s_NudgePerspectiveReset);
			s_CyclePerspective = (KeyCode)EditorGUILayout.EnumPopup("Cycle Perspective", s_CyclePerspective);

			if (GUILayout.Button("Reset"))
			{
				if (EditorUtility.DisplayDialog("Delete ProGrids editor preferences?", "Are you sure you want to delete these? This action cannot be undone.", "Yes", "No"))
					ResetPrefs();
			}

			if(EditorGUI.EndChangeCheck())
				SetPreferences();
		}

		public static bool LoadPreferences()
		{
			s_ScaleSnapEnabled = EditorPrefs.GetBool(pg_PreferenceKeys.SnapScale, pg_Defaults.SnapOnScale);
			s_GridColorX = (EditorPrefs.HasKey(pg_PreferenceKeys.GridColorX)) ? pg_Util.ColorWithString(EditorPrefs.GetString(pg_PreferenceKeys.GridColorX)) : pg_Defaults.GridColorX;
			s_GridColorY = (EditorPrefs.HasKey(pg_PreferenceKeys.GridColorY)) ? pg_Util.ColorWithString(EditorPrefs.GetString(pg_PreferenceKeys.GridColorY)) : pg_Defaults.GridColorY;
			s_GridColorZ = (EditorPrefs.HasKey(pg_PreferenceKeys.GridColorZ)) ? pg_Util.ColorWithString(EditorPrefs.GetString(pg_PreferenceKeys.GridColorZ)) : pg_Defaults.GridColorZ;
			s_AlphaBump = EditorPrefs.GetFloat(pg_PreferenceKeys.AlphaBump, pg_Defaults.AlphaBump);
			s_BracketIncreaseValue = EditorPrefs.HasKey(pg_PreferenceKeys.BracketIncreaseValue) ? EditorPrefs.GetFloat(pg_PreferenceKeys.BracketIncreaseValue) : .25f;
			s_GridUnits = (SnapUnit) EditorPrefs.GetInt(pg_PreferenceKeys.GridUnit, 0);
			s_SyncUnitySnap = EditorPrefs.GetBool(pg_PreferenceKeys.SyncUnitySnap, true);
			s_SnapMethod = (SnapMethod) EditorPrefs.GetInt(pg_PreferenceKeys.SnapMethod, (int) pg_Defaults.SnapMethod);

			s_IncreaseGridSize = EditorPrefs.HasKey(pg_PreferenceKeys.IncreaseGridSize)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.IncreaseGridSize)
				: KeyCode.Equals;
			s_DecreaseGridSize = EditorPrefs.HasKey(pg_PreferenceKeys.DecreaseGridSize)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.DecreaseGridSize)
				: KeyCode.Minus;
			s_NudgePerspectiveBackward = EditorPrefs.HasKey(pg_PreferenceKeys.NudgePerspectiveBackward)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.NudgePerspectiveBackward)
				: KeyCode.LeftBracket;
			s_NudgePerspectiveForward = EditorPrefs.HasKey(pg_PreferenceKeys.NudgePerspectiveForward)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.NudgePerspectiveForward)
				: KeyCode.RightBracket;
			s_NudgePerspectiveReset = EditorPrefs.HasKey(pg_PreferenceKeys.NudgePerspectiveReset)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.NudgePerspectiveReset)
				: KeyCode.Alpha0;
			s_CyclePerspective = EditorPrefs.HasKey(pg_PreferenceKeys.CyclePerspective)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.CyclePerspective)
				: KeyCode.Backslash;

			return true;
		}

		public static void SetPreferences()
		{
			EditorPrefs.SetBool(pg_PreferenceKeys.SnapScale, s_ScaleSnapEnabled);
			EditorPrefs.SetString(pg_PreferenceKeys.GridColorX, s_GridColorX.ToString("f3"));
			EditorPrefs.SetString(pg_PreferenceKeys.GridColorY, s_GridColorY.ToString("f3"));
			EditorPrefs.SetString(pg_PreferenceKeys.GridColorZ, s_GridColorZ.ToString("f3"));
			EditorPrefs.SetFloat(pg_PreferenceKeys.AlphaBump, s_AlphaBump);
			EditorPrefs.SetFloat(pg_PreferenceKeys.BracketIncreaseValue, s_BracketIncreaseValue);
			EditorPrefs.SetInt(pg_PreferenceKeys.GridUnit, (int)s_GridUnits);
			EditorPrefs.SetBool(pg_PreferenceKeys.SyncUnitySnap, s_SyncUnitySnap);
			EditorPrefs.SetInt(pg_PreferenceKeys.IncreaseGridSize, (int)s_IncreaseGridSize);
			EditorPrefs.SetInt(pg_PreferenceKeys.DecreaseGridSize, (int)s_DecreaseGridSize);
			EditorPrefs.SetInt(pg_PreferenceKeys.NudgePerspectiveBackward, (int)s_NudgePerspectiveBackward);
			EditorPrefs.SetInt(pg_PreferenceKeys.NudgePerspectiveForward, (int)s_NudgePerspectiveForward);
			EditorPrefs.SetInt(pg_PreferenceKeys.NudgePerspectiveReset, (int)s_NudgePerspectiveReset);
			EditorPrefs.SetInt(pg_PreferenceKeys.CyclePerspective, (int)s_CyclePerspective);
			EditorPrefs.SetInt(pg_PreferenceKeys.SnapMethod, (int) s_SnapMethod);

			if (pg_Editor.instance != null)
				pg_Editor.instance.LoadPreferences();
		}

		public static void ResetPrefs()
		{
			EditorPrefs.DeleteKey(pg_PreferenceKeys.SnapValue);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.SnapMultiplier);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.SnapEnabled);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.LastOrthoToggledRotation);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.BracketIncreaseValue);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.GridUnit);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.LockGrid);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.LockedGridPivot);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.GridAxis);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.PerspGrid);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.SnapScale);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.PredictiveGrid);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.SnapAsGroup);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.MajorLineIncrement);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.SyncUnitySnap);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.SnapMethod);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.GridColorX);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.GridColorY);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.GridColorZ);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.AlphaBump);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.ShowGrid);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.IncreaseGridSize);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.DecreaseGridSize);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.NudgePerspectiveBackward);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.NudgePerspectiveForward);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.NudgePerspectiveReset);
			EditorPrefs.DeleteKey(pg_PreferenceKeys.CyclePerspective);

			LoadPreferences();
		}
	}
}
