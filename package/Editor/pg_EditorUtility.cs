using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ProGrids.Editor
{
	static class pg_EditorUtility
	{
		const BindingFlags k_BindingFlagsAll = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
		static string s_ProGridsDirectory = "Packages/com.unity.progrids";

		// The order is important - always search for the package manager installed version first
		static string[] k_PossibleInstallDirectories = new string[]
		{
			"Packages/com.unity.progrids/",
			"UnityPackageManager/com.unity.progrids/",
			"Assets/ProCore/ProGrids/",
			"Assets/Plugins/ProGrids/",
			"Assets/Extensions/ProGrids/",
			"Assets/AssetStore/ProGrids/",
		};

		static SceneView.OnSceneFunc onPreSceneGuiDelegate
		{
			get
			{
				var fi = typeof(SceneView).GetField("onPreSceneGUIDelegate", k_BindingFlagsAll);
				return fi != null ? fi.GetValue(null) as SceneView.OnSceneFunc : null;
			}

			set
			{
				var fi = typeof(SceneView).GetField("onPreSceneGUIDelegate", k_BindingFlagsAll);

				if (fi != null)
					fi.SetValue(null, value);
			}
		}

		public static void RegisterOnPreSceneGUIDelegate(SceneView.OnSceneFunc func)
		{
			var del = onPreSceneGuiDelegate;

			if (del == null)
				onPreSceneGuiDelegate = func;
			else
				del += func;
		}

		public static void UnregisterOnPreSceneGUIDelegate(SceneView.OnSceneFunc func)
		{
			var del = onPreSceneGuiDelegate;

			if (del != null)
				del -= func;
		}

		public static T LoadInternalAsset<T>(string path) where T : Object
		{
			if(path.StartsWith("/"))
				path = path.Substring(1, path.Length - 1);

			return AssetDatabase.LoadAssetAtPath<T>(GetProGridsInstallDirectory() + path);
		}

		static bool ValidateProGridsRoot(string dir)
		{
			return File.Exists(dir + "Editor/pg_Editor.cs");
		}

		/// <summary>
		/// Return a relative path to the ProBuilder directory. Can be in the packages cache or Assets folder.
		/// If the project is in the packman cache it is immutable.
		/// </summary>
		/// <returns></returns>
		static string GetProGridsInstallDirectory()
		{
			if (ValidateProGridsRoot(s_ProGridsDirectory))
				return s_ProGridsDirectory;

			foreach (var install in k_PossibleInstallDirectories)
			{
				s_ProGridsDirectory = install;

				if (ValidateProGridsRoot(s_ProGridsDirectory))
					return s_ProGridsDirectory;
			}

			// It's not in any of the usual haunts, start digging through Assets until we find it (likely an A$ install)
			s_ProGridsDirectory = FindProGridsInProject();

			if (Directory.Exists(s_ProGridsDirectory))
				return s_ProGridsDirectory;

			Debug.LogWarning("Could not find ProGrids directory... was the ProGrids folder renamed?\nIcons & preferences may not work in this state.");

			return s_ProGridsDirectory;
		}

		/// <summary>
		/// Scan the Assets directory for an install of ProGrids
		/// </summary>
		/// <returns></returns>
		static string FindProGridsInProject()
		{
			if (Directory.Exists(s_ProGridsDirectory) && ValidateProGridsRoot(s_ProGridsDirectory))
				return s_ProGridsDirectory;

			string[] matches = Directory.GetDirectories("Assets", "ProGrids", SearchOption.AllDirectories);

			foreach (var match in matches)
			{
				s_ProGridsDirectory = match.Replace("\\", "/") +  "/";

				if (ValidateProGridsRoot(s_ProGridsDirectory))
				{
					if (!s_ProGridsDirectory.EndsWith("/"))
						s_ProGridsDirectory += "/";
					break;
				}
			}

			return s_ProGridsDirectory;
		}

		internal static string FindFolder(string folder)
		{
#if !UNITY_WEBPLAYER
			string single = folder.Replace("\\", "/").Substring(folder.LastIndexOf('/') + 1);

			string[] matches = Directory.GetDirectories("Assets/", single, SearchOption.AllDirectories);

			foreach(string str in matches)
			{
				string path = str.Replace("\\", "/");

				if(path.Contains(folder))
				{
					if(!path.EndsWith("/"))
						path += "/";

					return path;
				}
			}
#endif
			Debug.LogError("Could not locate ProGrids/GUI/ProGridsToggles folder.  The ProGrids folder may be moved, but the contents of ProGrids must remain unmodified.");

			return "";
		}

	}
}
