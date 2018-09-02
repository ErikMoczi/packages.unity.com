using System;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using Object = UnityEngine.Object;

namespace ProGrids.Editor
{
	static class EditorUtility
	{
		const BindingFlags k_BindingFlagsAll = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
		static string s_ProGridsDirectory = "Packages/com.unity.progrids";
		const float k_Epsilon = .0001f;
		static Dictionary<Transform, SnapEnabledOverride> s_SnapOverrideCache = new Dictionary<Transform, SnapEnabledOverride>();
		static Dictionary<Type, bool> s_NoSnapAttributeTypeCache = new Dictionary<Type, bool>();
		static Dictionary<Type, MethodInfo> s_ConditionalSnapAttributeCache = new Dictionary<Type, MethodInfo>();

		// The order is important - always search for the package manager installed version first
		static string[] k_PossibleInstallDirectories = new string[]
		{
			"Packages/com.unity.progrids/",
			"UnityPackageManager/com.unity.progrids/",
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
			return File.Exists(dir + "Editor/ProGridsEditor.cs");
		}

		/// <summary>
		/// Return a relative path to the ProBuilder directory. Can be in the packages cache or Assets folder.
		/// If the project is in the packman cache it is immutable.
		/// </summary>
		/// <returns></returns>
		internal static string GetProGridsInstallDirectory()
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

			// wildcard search because while on windows file paths are case insensitve, osx and unix are not.
			foreach (var dir in Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories) )
			{
				if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(dir, "progrids", CompareOptions.IgnoreCase) > -1)
				{
					s_ProGridsDirectory = dir.Replace("\\", "/") + "/";

					if (ValidateProGridsRoot(s_ProGridsDirectory))
					{
						if (!s_ProGridsDirectory.EndsWith("/"))
							s_ProGridsDirectory += "/";
						break;
					}
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

		public static Color ColorWithString(string value)
		{
			string valid = "01234567890.,";
	        value = new string(value.Where(c => valid.Contains(c)).ToArray());
	        string[] rgba = value.Split(',');

	        // BRIGHT pink
	        if(rgba.Length < 4)
	        	return new Color(1f, 0f, 1f, 1f);

			return new Color(
				float.Parse(rgba[0]),
				float.Parse(rgba[1]),
				float.Parse(rgba[2]),
				float.Parse(rgba[3]));
		}

		static Vector3 VectorToMask(Vector3 vec)
		{
			return new Vector3( Mathf.Abs(vec.x) > Mathf.Epsilon ? 1f : 0f,
								Mathf.Abs(vec.y) > Mathf.Epsilon ? 1f : 0f,
								Mathf.Abs(vec.z) > Mathf.Epsilon ? 1f : 0f );
		}

		static Axis MaskToAxis(Vector3 vec)
		{
			Axis axis = Axis.None;
			if( Mathf.Abs(vec.x) > 0 ) axis |= Axis.X;
			if( Mathf.Abs(vec.y) > 0 ) axis |= Axis.Y;
			if( Mathf.Abs(vec.z) > 0 ) axis |= Axis.Z;
			return axis;
		}

		static Axis BestAxis(Vector3 vec)
		{
			float x = Mathf.Abs(vec.x);
			float y = Mathf.Abs(vec.y);
			float z = Mathf.Abs(vec.z);

			return (x > y && x > z) ? Axis.X : ((y > x && y > z) ? Axis.Y : Axis.Z);
		}

		public static Axis CalcDragAxis(Vector3 movement, Camera cam)
		{
			Vector3 mask = VectorToMask(movement);

			if(mask.x + mask.y + mask.z == 2)
			{
				return MaskToAxis(Vector3.one - mask);
			}
			else
			{
				switch( MaskToAxis(mask) )
				{
					case Axis.X:
						if( Mathf.Abs(Vector3.Dot(cam.transform.forward, Vector3.up)) < Mathf.Abs(Vector3.Dot(cam.transform.forward, Vector3.forward)))
							return Axis.Z;
						else
							return Axis.Y;

					case Axis.Y:
						if( Mathf.Abs(Vector3.Dot(cam.transform.forward, Vector3.right)) < Mathf.Abs(Vector3.Dot(cam.transform.forward, Vector3.forward)))
							return Axis.Z;
						else
							return Axis.X;

					case Axis.Z:
						if( Mathf.Abs(Vector3.Dot(cam.transform.forward, Vector3.right)) < Mathf.Abs(Vector3.Dot(cam.transform.forward, Vector3.up)))
							return Axis.Y;
						else
							return Axis.X;
					default:

						return Axis.None;
				}
			}
		}

		public static float ValueFromMask(Vector3 val, Vector3 mask)
		{
			if(Mathf.Abs(mask.x) > .0001f)
				return val.x;
			else if(Mathf.Abs(mask.y) > .0001f)
				return val.y;
			else
				return val.z;
		}

		public static Vector3 SnapValue(Vector3 val, float snapValue)
		{
			float _x = val.x, _y = val.y, _z = val.z;
			return new Vector3(
				Snap(_x, snapValue),
				Snap(_y, snapValue),
				Snap(_z, snapValue)
				);
		}

		/**
		 *	Fetch a type with name and optional assembly name.  `type` should include namespace.
		 */
		static Type GetType(string type, string assembly = null)
		{
			Type t = Type.GetType(type);

			if(t == null)
			{
				IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies();

				if(assembly != null)
					assemblies = assemblies.Where(x => x.FullName.Contains(assembly));

				foreach(Assembly ass in assemblies)
				{
					t = ass.GetType(type);

					if(t != null)
						return t;
				}
			}

			return t;
		}

		public static void SetUnityGridEnabled(bool isEnabled)
		{
			try
			{
				Type annotationUtility = GetType("UnityEditor.AnnotationUtility");
				PropertyInfo pi = annotationUtility.GetProperty("showGrid", BindingFlags.NonPublic | BindingFlags.Static);
				pi.SetValue(null, isEnabled, BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
			}
			catch
			{}
		}

		public static bool GetUnityGridEnabled()
		{
			try
			{
				Type annotationUtility = GetType("UnityEditor.AnnotationUtility");
				PropertyInfo pi = annotationUtility.GetProperty("showGrid", BindingFlags.NonPublic | BindingFlags.Static);
				return (bool) pi.GetValue(null, null);
			}
			catch
			{}

			return false;
		}

		public static Vector3 SnapValue(Vector3 val, Vector3 mask, float snapValue)
		{

			float _x = val.x, _y = val.y, _z = val.z;
			return new Vector3(
				( Mathf.Abs(mask.x) < k_Epsilon ? _x : Snap(_x, snapValue) ),
				( Mathf.Abs(mask.y) < k_Epsilon ? _y : Snap(_y, snapValue) ),
				( Mathf.Abs(mask.z) < k_Epsilon ? _z : Snap(_z, snapValue) )
				);
		}

		public static Vector3 SnapToCeil(Vector3 val, Vector3 mask, float snapValue)
		{
			float _x = val.x, _y = val.y, _z = val.z;
			return new Vector3(
				( Mathf.Abs(mask.x) < k_Epsilon ? _x : SnapToCeil(_x, snapValue) ),
				( Mathf.Abs(mask.y) < k_Epsilon ? _y : SnapToCeil(_y, snapValue) ),
				( Mathf.Abs(mask.z) < k_Epsilon ? _z : SnapToCeil(_z, snapValue) )
				);
		}

		public static Vector3 SnapToFloor(Vector3 val, float snapValue)
		{
			float _x = val.x, _y = val.y, _z = val.z;
			return new Vector3(
				SnapToFloor(_x, snapValue),
				SnapToFloor(_y, snapValue),
				SnapToFloor(_z, snapValue)
				);
		}

		public static Vector3 SnapToFloor(Vector3 val, Vector3 mask, float snapValue)
		{
			float _x = val.x, _y = val.y, _z = val.z;
			return new Vector3(
				( Mathf.Abs(mask.x) < k_Epsilon ? _x : SnapToFloor(_x, snapValue) ),
				( Mathf.Abs(mask.y) < k_Epsilon ? _y : SnapToFloor(_y, snapValue) ),
				( Mathf.Abs(mask.z) < k_Epsilon ? _z : SnapToFloor(_z, snapValue) )
				);
		}

		public static float Snap(float val, float round)
		{
			return round * Mathf.Round(val / round);
		}

		public static float SnapToFloor(float val, float snapValue)
		{
			return snapValue * Mathf.Floor(val / snapValue);
		}

		public static float SnapToCeil(float val, float snapValue)
		{
			return snapValue * Mathf.Ceil(val / snapValue);
		}

		public static Vector3 CeilFloor(Vector3 v)
		{
			v.x = v.x < 0 ? -1 : 1;
			v.y = v.y < 0 ? -1 : 1;
			v.z = v.z < 0 ? -1 : 1;

			return v;
		}

		abstract class SnapEnabledOverride
		{
			public abstract bool IsEnabled();
		}

		class SnapIsEnabledOverride : SnapEnabledOverride
		{
			bool m_SnapIsEnabled;

			public SnapIsEnabledOverride(bool snapIsEnabled)
			{
				m_SnapIsEnabled = snapIsEnabled;
			}

			public override bool IsEnabled() { return m_SnapIsEnabled; }
		}

		class ConditionalSnapOverride : SnapEnabledOverride
		{
			public System.Func<bool> m_IsEnabledDelegate;

			public ConditionalSnapOverride(System.Func<bool> d)
			{
				m_IsEnabledDelegate = d;
			}

			public override bool IsEnabled() { return m_IsEnabledDelegate(); }
		}

		public static void ClearSnapEnabledCache()
		{
			s_SnapOverrideCache.Clear();
		}

		public static bool SnapIsEnabled(Transform t)
		{
			SnapEnabledOverride so;

			if(s_SnapOverrideCache.TryGetValue(t, out so))
				return so.IsEnabled();

			object[] attribs = null;

			foreach(Component c in t.GetComponents<MonoBehaviour>())
			{
				if(c == null)
					continue;

				Type type = c.GetType();

				bool hasNoSnapAttrib;

				if(s_NoSnapAttributeTypeCache.TryGetValue(type, out hasNoSnapAttrib))
				{
					if(hasNoSnapAttrib)
					{
						s_SnapOverrideCache.Add(t, new SnapIsEnabledOverride(!hasNoSnapAttrib));
						return true;
					}
				}
				else
				{
					attribs = type.GetCustomAttributes(true);
					hasNoSnapAttrib = attribs.Any(x => x != null && x.ToString().Contains("ProGridsNoSnap"));
					s_NoSnapAttributeTypeCache.Add(type, hasNoSnapAttrib);

					if(hasNoSnapAttrib)
					{
						s_SnapOverrideCache.Add(t, new SnapIsEnabledOverride(!hasNoSnapAttrib));
						return true;
					}
				}

				MethodInfo mi;

				if(s_ConditionalSnapAttributeCache.TryGetValue(type, out mi))
				{
					if(mi != null)
					{
						s_SnapOverrideCache.Add(t, new ConditionalSnapOverride(() => { return (bool) mi.Invoke(c, null); }));
						return (bool) mi.Invoke(c, null);
					}
				}
				else
				{
					if( attribs.Any(x => x != null && x.ToString().Contains("ProGridsConditionalSnap")) )
					{
						mi = type.GetMethod("IsSnapEnabled", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);

						s_ConditionalSnapAttributeCache.Add(type, mi);

						if(mi != null)
						{
							s_SnapOverrideCache.Add(t, new ConditionalSnapOverride(() => { return (bool) mi.Invoke(c, null); }));
							return (bool) mi.Invoke(c, null);
						}
					}
					else
					{
						s_ConditionalSnapAttributeCache.Add(type, null);
					}
				}
			}

			s_SnapOverrideCache.Add(t, new SnapIsEnabledOverride(true));

			return true;
		}

		public static bool Contains(this Transform[] arr, Transform trs)
		{
			for(int i = 0; i < arr.Length; i++)
				if(arr[i] == trs)
					return true;
			return false;
		}

		public static float Sum(this Vector3 v)
		{
			return v[0] + v[1] + v[2];
		}

		public static bool InFrustum(this Camera cam, Vector3 point)
		{
			Vector3 p = cam.WorldToViewportPoint(point);
			return  (p.x >= 0f && p.x <= 1f) &&
			        (p.y >= 0f && p.y <= 1f) &&
			        p.z >= 0f;
		}
	}
}
