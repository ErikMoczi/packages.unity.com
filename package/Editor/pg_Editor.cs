#define PRO

using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ProGrids.Editor
{
	[InitializeOnLoad]
	static class pg_Initializer
	{
		/// <summary>
		/// When opening Unity, remember whether or not ProGrids was open when Unity was shut down last.
		/// </summary>
		static pg_Initializer()
		{
			pg_Editor.InitIfEnabled();
			pg_PlayModeStateListener.onEnterEditMode += pg_Editor.InitIfEnabled;
			pg_PlayModeStateListener.onEnterPlayMode += pg_Editor.DestroyIfEnabled;
		}
	}

	class pg_Editor
	{
#region Properties

		static pg_Editor s_Instance;

		// the actual snap value, taking into account unit size
		float m_SnapValue = 1f;
		// what the user sees
		float m_UiSnapValue = 1f;
		bool m_SnapEnabled = true;
		SnapUnit m_SnapUnit = SnapUnit.Meter;
		bool m_GridIsLocked = false;
		bool m_DrawGrid = true;
		bool m_DrawAngles = false;
		bool m_DoGridRepaint = true;
		bool m_ScaleSnapEnabled = false;
		bool m_SnapAsGroup = true;
		Axis m_RenderPlane = Axis.Y;
		public float angleValue = 45f;
		public bool predictiveGrid = true;

		KeyCode m_IncreaseGridSizeShortcut = KeyCode.Equals;
		KeyCode m_DecreaseGridSizeShortcut = KeyCode.Minus;
		KeyCode m_NudgePerspectiveBackwardShortcut = KeyCode.LeftBracket;
		KeyCode m_NudgePerspectiveForwardShortcut = KeyCode.RightBracket;
		KeyCode m_NudgePerspectiveResetShortcut = KeyCode.Alpha0;
		KeyCode m_CyclePerspectiveShortcut = KeyCode.Backslash;

		Texture2D m_ExtendoClosed;
		Texture2D m_ExtendoOpen;
		internal Color gridColorX;
		internal Color gridColorY;
		internal Color gridColorZ;
		internal Color gridColorXPrimary;
		internal Color gridColorYPrimary;
		internal Color gridColorZPrimary;

		GUIStyle m_GridButtonStyle = new GUIStyle();
		GUIStyle m_ExtendoStyle = new GUIStyle();
		GUIStyle m_GridButtonStyleBlank = new GUIStyle();
		GUIStyle m_BackgroundStyle = new GUIStyle();
		bool m_GuiInitialized = false;

		const int k_CurrentPreferencesVersion = 22;

		// the maximum amount of lines to display on screen in either direction
		const int k_MaxLines = 150;

		/// <summary>
		/// Every tenth line gets an alpha bump by this amount
		/// </summary>
		public static float alphaBump;

		Transform m_LastActiveTransform;
		const string k_AxisConstraintKey = "s";
		const string k_TempDisableKey = "d";
		bool m_ToggleAxisConstraint = false;
		bool m_ToggleTempSnap = false;
		Vector3 m_LastPosition = Vector3.zero;
		// Vector3 lastRotation = Vector3.zero;
		Vector3 m_LastScale = Vector3.one;
		Vector3 pivot = Vector3.zero;
		Vector3 lastPivot = Vector3.zero;
		Vector3 camDir = Vector3.zero, prevCamDir = Vector3.zero;
		// Distance from camera to pivot at the last time the grid mesh was updated.
		float lastDistance = 0f;
		public float offset = 0f;
		public bool ortho { get; private set; }
		bool prevOrtho = false;
		bool firstMove = true;
		float planeGridDrawDistance = 0f;
		bool m_IsEnabled;

		public static pg_Editor instance
		{
			get { return s_Instance; }
		}

		public bool snapAsGroup
		{
			get { return EditorPrefs.GetBool(pg_PreferenceKeys.SnapAsGroup, pg_Defaults.SnapAsGroup); }

			set
			{
				m_SnapAsGroup = value;
				EditorPrefs.SetBool(pg_PreferenceKeys.SnapAsGroup, m_SnapAsGroup);
			}
		}

		bool useAxisConstraints
		{
			get { return (SnapMethod) EditorPrefs.GetInt(pg_PreferenceKeys.SnapMethod, (int) pg_Defaults.SnapMethod) == SnapMethod.SnapOnSelectedAxis; }
			set { EditorPrefs.SetInt(pg_PreferenceKeys.SnapMethod, (int) (value ? SnapMethod.SnapOnSelectedAxis : SnapMethod.SnapOnAllAxes)); }
		}

		public bool fullGrid { get; private set; }

		public bool ScaleSnapEnabled
		{
			get { return EditorPrefs.GetBool(pg_PreferenceKeys.SnapScale, true); }

			set
			{
				m_ScaleSnapEnabled = value;
				EditorPrefs.SetBool(pg_PreferenceKeys.SnapScale, m_ScaleSnapEnabled);
			}
		}

		public bool gridIsLocked
		{
			get { return m_GridIsLocked; }
		}

		public float GetSnapIncrement()
		{
			return m_UiSnapValue;
		}

		public void SetSnapIncrement(float inc)
		{
			SetSnapValue(m_SnapUnit, Mathf.Max(inc, .001f), pg_Defaults.DefaultSnapMultiplier);
		}

		int isMenuHidden { get { return menuIsOrtho ? -192 : -173; } }

		pg_ToggleContent m_SnapToGridContent = new pg_ToggleContent("Snap", "", "Snaps all selected objects to grid.");
		pg_ToggleContent m_GridEnabledContent = new pg_ToggleContent("Hide", "Show", "Toggles drawing of guide lines on or off.  Note that object snapping is not affected by this setting.");
		pg_ToggleContent m_SnapEnabledContent = new pg_ToggleContent("On", "Off", "Toggles snapping on or off.");
		pg_ToggleContent m_LockGridContent = new pg_ToggleContent("Lock", "Unlck", "Lock the perspective grid center in place.");
		pg_ToggleContent m_AngleEnabledContent = new pg_ToggleContent("> On", "> Off", "If on, ProGrids will draw angled line guides.  Angle is settable in degrees.");
		pg_ToggleContent m_RenderPlaneXContent = new pg_ToggleContent("X", "X", "Renders a grid on the X plane.");
		pg_ToggleContent m_RenderPlaneYContent = new pg_ToggleContent("Y", "Y", "Renders a grid on the Y plane.");
		pg_ToggleContent m_RenderPlaneZContent = new pg_ToggleContent("Z", "Z", "Renders a grid on the Z plane.");
		pg_ToggleContent m_RenderPerspectiveGridContent = new pg_ToggleContent("Full", "Plane", "Renders a 3d grid in perspective mode.");
		GUIContent m_ExtendMenuContent = new GUIContent("", "Show or hide the scene view menu.");
		GUIContent m_SnapIncrementContent = new GUIContent("", "Set the snap increment.");
#endregion

#region Menu Actions

		internal static void IncreaseGridSize()
		{
			if (!IsEnabled())
				return;

			int multiplier = EditorPrefs.HasKey(pg_PreferenceKeys.SnapMultiplier) ? EditorPrefs.GetInt(pg_PreferenceKeys.SnapMultiplier) : pg_Defaults.DefaultSnapMultiplier;

			float val = EditorPrefs.HasKey(pg_PreferenceKeys.SnapValue) ? EditorPrefs.GetFloat(pg_PreferenceKeys.SnapValue) : 1f;

			if (multiplier < int.MaxValue / 2)
				multiplier *= 2;

			s_Instance.SetSnapValue(pg_Editor.instance.m_SnapUnit, val, multiplier);

			SceneView.RepaintAll();
		}

		internal static void DecreaseGridSize()
		{
			if (!IsEnabled())
				return;

			int multiplier = EditorPrefs.HasKey(pg_PreferenceKeys.SnapMultiplier) ? EditorPrefs.GetInt(pg_PreferenceKeys.SnapMultiplier) : pg_Defaults.DefaultSnapMultiplier;
			float val = EditorPrefs.HasKey(pg_PreferenceKeys.SnapValue) ? EditorPrefs.GetFloat(pg_PreferenceKeys.SnapValue) : 1f;

			if (multiplier > 1)
				multiplier /= 2;

			s_Instance.SetSnapValue(pg_Editor.instance.m_SnapUnit, val, multiplier);
			SceneView.RepaintAll();

		}

		internal static void MenuNudgePerspectiveBackward()
		{
			if (!IsEnabled() || !instance.m_GridIsLocked)
				return;

			instance.offset -= instance.m_SnapValue;
			DoGridRepaint();

		}

		internal static void MenuNudgePerspectiveForward()
		{
			if (!IsEnabled() || !instance.m_GridIsLocked)
				return;
			instance.offset += instance.m_SnapValue;
			DoGridRepaint();
		}

		internal static void MenuNudgePerspectiveReset()
		{
			if (!IsEnabled() || !instance.m_GridIsLocked)
				return;

			instance.offset = 0;
			DoGridRepaint();
		}

		internal static void CyclePerspective()
		{
			if (!IsEnabled())
				return;

			SceneView scnvw = SceneView.lastActiveSceneView;

			if (scnvw == null)
				return;

			int nextOrtho = EditorPrefs.GetInt(pg_PreferenceKeys.LastOrthoToggledRotation);

			switch (nextOrtho)
			{
				case 0:
					scnvw.orthographic = true;
					scnvw.LookAt(scnvw.pivot, Quaternion.Euler(Vector3.zero));
					nextOrtho++;
					break;

				case 1:
					scnvw.orthographic = true;
					scnvw.LookAt(scnvw.pivot, Quaternion.Euler(Vector3.up * -90f));
					nextOrtho++;
					break;

				case 2:
					scnvw.orthographic = true;
					scnvw.LookAt(scnvw.pivot, Quaternion.Euler(Vector3.right * 90f));
					nextOrtho++;
					break;

				case 3:
					scnvw.orthographic = false;
					scnvw.LookAt(scnvw.pivot, new Quaternion(-0.1f, 0.9f, -0.2f, -0.4f));
					nextOrtho = 0;
					break;
			}

			EditorPrefs.SetInt(pg_PreferenceKeys.LastOrthoToggledRotation, nextOrtho);
		}

#endregion

#region INITIALIZATION / SERIALIZATION

		internal static void InitIfEnabled()
		{
			if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorPrefs.GetBool(pg_PreferenceKeys.ProGridsIsEnabled))
				Init();
		}

		internal static void DestroyIfEnabled()
		{
			if(s_Instance != null)
				s_Instance.Destroy();
		}

		public static void Init()
		{
			EditorPrefs.SetBool(pg_PreferenceKeys.ProGridsIsEnabled, true);

			if (s_Instance == null)
				new pg_Editor().Initialize();
			else
				s_Instance.Initialize();
		}

		pg_Editor()
		{
		}

		~pg_Editor()
		{
			Destroy();
		}

		void Initialize()
		{
			s_Instance = this;
			RegisterDelegates();
			LoadGUIResources();
			LoadPreferences();
			pg_GridRenderer.Init();
			SetMenuIsExtended(menuOpen);
			lastTime = Time.realtimeSinceStartup;

			// reset colors without changing anything
			menuOpen = !menuOpen;
			ToggleMenuVisibility();

			if (m_DrawGrid)
				pg_Util.SetUnityGridEnabled(false);

			DoGridRepaint();
		}

		public static void Close()
		{
			EditorPrefs.SetBool(pg_PreferenceKeys.ProGridsIsEnabled, false);
			if(s_Instance != null)
				s_Instance.Destroy();
		}

		public void Destroy()
		{
			pg_GridRenderer.Destroy();
			UnregisterDelegates();
			foreach (Action<bool> listener in toolbarEventSubscribers)
				listener(false);
			pg_Util.SetUnityGridEnabled(true);
			SceneView.RepaintAll();
		}

		public static bool IsEnabled()
		{
			return s_Instance != null && s_Instance.m_IsEnabled;
		}

		void RegisterDelegates()
		{
			UnregisterDelegates();

			SceneView.onSceneGUIDelegate += OnSceneGUI;
			EditorApplication.update += Update;
			Selection.selectionChanged += OnSelectionChange;
			pg_EditorUtility.RegisterOnPreSceneGUIDelegate(pg_GridRenderer.DoGUI);

#if UNITY_2018_1_OR_NEWER
			EditorApplication.hierarchyChanged += HierarchyWindowChanged;
#else
			EditorApplication.hierarchyWindowChanged += HierarchyWindowChanged;
#endif

			m_IsEnabled = true;
		}

		void UnregisterDelegates()
		{
			m_IsEnabled = false;

			SceneView.onSceneGUIDelegate -= pg_GridRenderer.DoGUI;
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			EditorApplication.update -= Update;
			Selection.selectionChanged -= OnSelectionChange;
			pg_EditorUtility.UnregisterOnPreSceneGUIDelegate(pg_GridRenderer.DoGUI);

#if UNITY_2018_1_OR_NEWER
			EditorApplication.hierarchyChanged -= HierarchyWindowChanged;
#else
			EditorApplication.hierarchyWindowChanged -= HierarchyWindowChanged;
#endif
		}

		public void LoadPreferences()
		{
			if (EditorPrefs.GetInt(pg_PreferenceKeys.StoredPreferenceVersion, k_CurrentPreferencesVersion) != k_CurrentPreferencesVersion)
			{
				EditorPrefs.SetInt(pg_PreferenceKeys.StoredPreferenceVersion, k_CurrentPreferencesVersion);
				pg_Preferences.ResetPrefs();
			}

			if (EditorPrefs.HasKey(pg_PreferenceKeys.SnapEnabled))
			{
				m_SnapEnabled = EditorPrefs.GetBool(pg_PreferenceKeys.SnapEnabled);
			}

			menuOpen = EditorPrefs.GetBool(pg_PreferenceKeys.ProGridsIsExtended, true);

			SetSnapValue(
				(SnapUnit) EditorPrefs.GetInt(pg_PreferenceKeys.GridUnit, (int) pg_Defaults.SnapUnit),
				EditorPrefs.GetFloat(pg_PreferenceKeys.SnapValue, pg_Defaults.SnapValue),
				EditorPrefs.GetInt(pg_PreferenceKeys.SnapMultiplier, pg_Defaults.DefaultSnapMultiplier)
				);

			m_IncreaseGridSizeShortcut = EditorPrefs.HasKey(pg_PreferenceKeys.IncreaseGridSize)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.IncreaseGridSize)
				: KeyCode.Equals;
			m_DecreaseGridSizeShortcut = EditorPrefs.HasKey(pg_PreferenceKeys.DecreaseGridSize)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.DecreaseGridSize)
				: KeyCode.Minus;
			m_NudgePerspectiveBackwardShortcut = EditorPrefs.HasKey(pg_PreferenceKeys.NudgePerspectiveBackward)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.NudgePerspectiveBackward)
				: KeyCode.LeftBracket;
			m_NudgePerspectiveForwardShortcut = EditorPrefs.HasKey(pg_PreferenceKeys.NudgePerspectiveForward)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.NudgePerspectiveForward)
				: KeyCode.RightBracket;
			m_NudgePerspectiveResetShortcut = EditorPrefs.HasKey(pg_PreferenceKeys.NudgePerspectiveReset)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.NudgePerspectiveReset)
				: KeyCode.Alpha0;
			m_CyclePerspectiveShortcut = EditorPrefs.HasKey(pg_PreferenceKeys.CyclePerspective)
				? (KeyCode)EditorPrefs.GetInt(pg_PreferenceKeys.CyclePerspective)
				: KeyCode.Backslash;

			m_GridIsLocked = EditorPrefs.GetBool(pg_PreferenceKeys.LockGrid);

			if (m_GridIsLocked)
			{
				if (EditorPrefs.HasKey(pg_PreferenceKeys.LockedGridPivot))
				{
					string piv = EditorPrefs.GetString(pg_PreferenceKeys.LockedGridPivot);
					string[] pivsplit = piv.Replace("(", "").Replace(")", "").Split(',');

					float x, y, z;
					if (!float.TryParse(pivsplit[0], out x)) goto NoParseForYou;
					if (!float.TryParse(pivsplit[1], out y)) goto NoParseForYou;
					if (!float.TryParse(pivsplit[2], out z)) goto NoParseForYou;

					pivot.x = x;
					pivot.y = y;
					pivot.z = z;

				NoParseForYou:
					;   // appease the compiler
				}

			}

			fullGrid = EditorPrefs.GetBool(pg_PreferenceKeys.PerspGrid);

			m_RenderPlane = EditorPrefs.HasKey(pg_PreferenceKeys.GridAxis) ? (Axis)EditorPrefs.GetInt(pg_PreferenceKeys.GridAxis) : Axis.Y;

			alphaBump = EditorPrefs.GetFloat(pg_PreferenceKeys.AlphaBump, pg_Defaults.AlphaBump);
			gridColorX = (EditorPrefs.HasKey(pg_PreferenceKeys.GridColorX)) ? pg_Util.ColorWithString(EditorPrefs.GetString(pg_PreferenceKeys.GridColorX)) : pg_Defaults.GridColorX;
			gridColorXPrimary = new Color(gridColorX.r, gridColorX.g, gridColorX.b, gridColorX.a + alphaBump);
			gridColorY = (EditorPrefs.HasKey(pg_PreferenceKeys.GridColorY)) ? pg_Util.ColorWithString(EditorPrefs.GetString(pg_PreferenceKeys.GridColorY)) : pg_Defaults.GridColorY;
			gridColorYPrimary = new Color(gridColorY.r, gridColorY.g, gridColorY.b, gridColorY.a + alphaBump);
			gridColorZ = (EditorPrefs.HasKey(pg_PreferenceKeys.GridColorZ)) ? pg_Util.ColorWithString(EditorPrefs.GetString(pg_PreferenceKeys.GridColorZ)) : pg_Defaults.GridColorZ;
			gridColorZPrimary = new Color(gridColorZ.r, gridColorZ.g, gridColorZ.b, gridColorZ.a + alphaBump);

			m_DrawGrid = EditorPrefs.GetBool(pg_PreferenceKeys.ShowGrid, pg_Defaults.ShowGrid);

			predictiveGrid = EditorPrefs.GetBool(pg_PreferenceKeys.PredictiveGrid, pg_Defaults.PredictiveGrid);

			m_SnapAsGroup = snapAsGroup;
			m_ScaleSnapEnabled = ScaleSnapEnabled;
		}

		void LoadGUIResources()
		{
			if (m_GridEnabledContent.image_on == null)
				m_GridEnabledContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_Vis_On.png");

			if (m_GridEnabledContent.image_off == null)
				m_GridEnabledContent.image_off = pg_IconUtility.LoadIcon("ProGrids2_GUI_Vis_Off.png");

			if (m_SnapEnabledContent.image_on == null)
				m_SnapEnabledContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_Snap_On.png");

			if (m_SnapEnabledContent.image_off == null)
				m_SnapEnabledContent.image_off = pg_IconUtility.LoadIcon("ProGrids2_GUI_Snap_Off.png");

			if (m_SnapToGridContent.image_on == null)
				m_SnapToGridContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_PushToGrid_Normal.png");

			if (m_LockGridContent.image_on == null)
				m_LockGridContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_Lock_On.png");

			if (m_LockGridContent.image_off == null)
				m_LockGridContent.image_off = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_Lock_Off.png");

			if (m_AngleEnabledContent.image_on == null)
				m_AngleEnabledContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_AngleVis_On.png");

			if (m_AngleEnabledContent.image_off == null)
				m_AngleEnabledContent.image_off = pg_IconUtility.LoadIcon("ProGrids2_GUI_AngleVis_Off.png");

			if (m_RenderPlaneXContent.image_on == null)
				m_RenderPlaneXContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_X_On.png");

			if (m_RenderPlaneXContent.image_off == null)
				m_RenderPlaneXContent.image_off = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_X_Off.png");

			if (m_RenderPlaneYContent.image_on == null)
				m_RenderPlaneYContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_Y_On.png");

			if (m_RenderPlaneYContent.image_off == null)
				m_RenderPlaneYContent.image_off = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_Y_Off.png");

			if (m_RenderPlaneZContent.image_on == null)
				m_RenderPlaneZContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_Z_On.png");

			if (m_RenderPlaneZContent.image_off == null)
				m_RenderPlaneZContent.image_off = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_Z_Off.png");

			if (m_RenderPerspectiveGridContent.image_on == null)
				m_RenderPerspectiveGridContent.image_on = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_3D_On.png");

			if (m_RenderPerspectiveGridContent.image_off == null)
				m_RenderPerspectiveGridContent.image_off = pg_IconUtility.LoadIcon("ProGrids2_GUI_PGrid_3D_Off.png");

			if (m_ExtendoOpen == null)
				m_ExtendoOpen = pg_IconUtility.LoadIcon("ProGrids2_MenuExtendo_Open.png");

			if (m_ExtendoClosed == null)
				m_ExtendoClosed = pg_IconUtility.LoadIcon("ProGrids2_MenuExtendo_Close.png");
		}

#endregion

#region INTERFACE

		public static void DoGridRepaint()
		{
			if (instance != null)
			{
				instance.m_DoGridRepaint = true;
				SceneView.RepaintAll();
			}
		}

		const int MENU_EXTENDED = 8;
		const int PAD = 3;
		Rect r = new Rect(8, MENU_EXTENDED, 42, 16);
		Rect backgroundRect = new Rect(00, 0, 0, 0);
		Rect extendoButtonRect = new Rect(0, 0, 0, 0);
		bool menuOpen = true;
		float menuStart = MENU_EXTENDED;
		const float MENU_SPEED = 500f;
		float deltaTime = 0f;
		float lastTime = 0f;
		const float FADE_SPEED = 2.5f;
		float backgroundFade = 1f;
		bool mouseOverMenu = false;
		Color menuBackgroundColor = new Color(0f, 0f, 0f, .5f);
		Color extendoNormalColor = new Color(.9f, .9f, .9f, .7f);
		Color extendoHoverColor = new Color(0f, 1f, .4f, 1f);
		bool extendoButtonHovering = false;
		bool menuIsOrtho = false;

		void Update()
		{
			deltaTime = Time.realtimeSinceStartup - lastTime;
			lastTime = Time.realtimeSinceStartup;

			if ((menuOpen && menuStart < MENU_EXTENDED) || (!menuOpen && menuStart > isMenuHidden))
			{
				menuStart += deltaTime * MENU_SPEED * (menuOpen ? 1f : -1f);
				menuStart = Mathf.Clamp(menuStart, isMenuHidden, MENU_EXTENDED);
				DoGridRepaint();
			}

			float a = menuBackgroundColor.a;
			backgroundFade = (mouseOverMenu || !menuOpen) ? FADE_SPEED : -FADE_SPEED;

			menuBackgroundColor.a = Mathf.Clamp(menuBackgroundColor.a + backgroundFade * deltaTime, 0f, .5f);
			extendoNormalColor.a = menuBackgroundColor.a;
			extendoHoverColor.a = (menuBackgroundColor.a / .5f);

			if (!Mathf.Approximately(menuBackgroundColor.a, a))
				DoGridRepaint();
		}

		void DrawSceneGUI()
		{
			GUI.backgroundColor = menuBackgroundColor;
			backgroundRect.x = r.x - 4;
			backgroundRect.y = 0;
			backgroundRect.width = r.width + 8;
			backgroundRect.height = r.y + r.height + PAD;
			GUI.Box(backgroundRect, "", m_BackgroundStyle);

			// when hit testing mouse for showing the background, add some leeway
			backgroundRect.width += 32f;
			backgroundRect.height += 32f;
			GUI.backgroundColor = Color.white;

			if (!m_GuiInitialized)
			{
				m_ExtendoStyle.normal.background = menuOpen ? m_ExtendoClosed : m_ExtendoOpen;
				m_ExtendoStyle.hover.background = menuOpen ? m_ExtendoClosed : m_ExtendoOpen;

				m_GuiInitialized = true;
				m_BackgroundStyle.normal.background = EditorGUIUtility.whiteTexture;

				Texture2D icon_button_normal = pg_IconUtility.LoadIcon("ProGrids2_Button_Normal.png");
				Texture2D icon_button_hover = pg_IconUtility.LoadIcon("ProGrids2_Button_Hover.png");

				if (icon_button_normal == null)
				{
					m_GridButtonStyleBlank = new GUIStyle("button");
				}
				else
				{
					m_GridButtonStyleBlank.normal.background = icon_button_normal;
					m_GridButtonStyleBlank.hover.background = icon_button_hover;
					m_GridButtonStyleBlank.normal.textColor = icon_button_normal != null ? Color.white : Color.black;
					m_GridButtonStyleBlank.hover.textColor = new Color(.7f, .7f, .7f, 1f);
				}

				m_GridButtonStyleBlank.padding = new RectOffset(1, 2, 1, 2);
				m_GridButtonStyleBlank.alignment = TextAnchor.MiddleCenter;
			}

			r.y = menuStart;

			m_SnapIncrementContent.text = m_UiSnapValue.ToString("#.####");

			if (GUI.Button(r, m_SnapIncrementContent, m_GridButtonStyleBlank))
			{
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		// On Mac ShowAsDropdown and ShowAuxWindow both throw stack pop exceptions when initialized.
		pg_ParameterWindow options = EditorWindow.GetWindow<pg_ParameterWindow>(true, "ProGrids Settings", true);
		Rect screenRect = SceneView.lastActiveSceneView.position;
		options.editor = this;
		options.position = new Rect(screenRect.x + r.x + r.width + PAD,
										screenRect.y + r.y + 24,
										256,
										174);
#else
				pg_ParameterWindow options = ScriptableObject.CreateInstance<pg_ParameterWindow>();
				Rect screenRect = SceneView.lastActiveSceneView.position;
				options.editor = this;
				options.ShowAsDropDown(new Rect(screenRect.x + r.x + r.width + PAD,
												screenRect.y + r.y + 24,
												0,
												0),
												new Vector2(256, 174));
#endif
			}

			r.y += r.height + PAD;

			// Draw grid
			if (pg_ToggleContent.ToggleButton(r, m_GridEnabledContent, m_DrawGrid, m_GridButtonStyle, EditorStyles.miniButton))
				SetGridEnabled(!m_DrawGrid);

			r.y += r.height + PAD;

			// Snap enabled
			if (pg_ToggleContent.ToggleButton(r, m_SnapEnabledContent, m_SnapEnabled, m_GridButtonStyle, EditorStyles.miniButton))
				SetSnapEnabled(!m_SnapEnabled);

			r.y += r.height + PAD;

			// Push to grid
			if (pg_ToggleContent.ToggleButton(r, m_SnapToGridContent, true, m_GridButtonStyle, EditorStyles.miniButton))
				SnapToGrid(Selection.transforms);

			r.y += r.height + PAD;

			// Lock grid
			if (pg_ToggleContent.ToggleButton(r, m_LockGridContent, m_GridIsLocked, m_GridButtonStyle, EditorStyles.miniButton))
			{
				m_GridIsLocked = !m_GridIsLocked;
				EditorPrefs.SetBool(pg_PreferenceKeys.LockGrid, m_GridIsLocked);
				EditorPrefs.SetString(pg_PreferenceKeys.LockedGridPivot, pivot.ToString());

				// if we've modified the nudge value, reset the pivot here
				if (!m_GridIsLocked)
					offset = 0f;

				DoGridRepaint();
			}

			if (menuIsOrtho)
			{
				r.y += r.height + PAD;

				if (pg_ToggleContent.ToggleButton(r, m_AngleEnabledContent, m_DrawAngles, m_GridButtonStyle, EditorStyles.miniButton))
					SetDrawAngles(!m_DrawAngles);
			}

			/**
			 * Perspective Toggles
			 */
			r.y += r.height + PAD + 4;

			if (pg_ToggleContent.ToggleButton(r, m_RenderPlaneXContent, (m_RenderPlane & Axis.X) == Axis.X && !fullGrid, m_GridButtonStyle, EditorStyles.miniButton))
				SetRenderPlane(Axis.X);

			r.y += r.height + PAD;

			if (pg_ToggleContent.ToggleButton(r, m_RenderPlaneYContent, (m_RenderPlane & Axis.Y) == Axis.Y && !fullGrid, m_GridButtonStyle, EditorStyles.miniButton))
				SetRenderPlane(Axis.Y);

			r.y += r.height + PAD;

			if (pg_ToggleContent.ToggleButton(r, m_RenderPlaneZContent, (m_RenderPlane & Axis.Z) == Axis.Z && !fullGrid, m_GridButtonStyle, EditorStyles.miniButton))
				SetRenderPlane(Axis.Z);

			r.y += r.height + PAD;

			if (pg_ToggleContent.ToggleButton(r, m_RenderPerspectiveGridContent, fullGrid, m_GridButtonStyle, EditorStyles.miniButton))
			{
				fullGrid = !fullGrid;
				EditorPrefs.SetBool(pg_PreferenceKeys.PerspGrid, fullGrid);
				DoGridRepaint();
			}

			r.y += r.height + PAD;

			extendoButtonRect.x = r.x;
			extendoButtonRect.y = r.y;
			extendoButtonRect.width = r.width;
			extendoButtonRect.height = r.height;

			GUI.backgroundColor = extendoButtonHovering ? extendoHoverColor : extendoNormalColor;
			m_ExtendMenuContent.text = m_ExtendoOpen == null ? (menuOpen ? "Close" : "Open") : "";
			if (GUI.Button(r, m_ExtendMenuContent, m_ExtendoOpen ? m_ExtendoStyle : m_GridButtonStyleBlank))
			{
				ToggleMenuVisibility();
				extendoButtonHovering = false;
			}
			GUI.backgroundColor = Color.white;
		}

		void ToggleMenuVisibility()
		{
			menuOpen = !menuOpen;
			EditorPrefs.SetBool(pg_PreferenceKeys.ProGridsIsExtended, menuOpen);

			m_ExtendoStyle.normal.background = menuOpen ? m_ExtendoClosed : m_ExtendoOpen;
			m_ExtendoStyle.hover.background = menuOpen ? m_ExtendoClosed : m_ExtendoOpen;

			foreach (System.Action<bool> listener in toolbarEventSubscribers)
				listener(menuOpen);

			DoGridRepaint();
		}

		// skip color fading and stuff
		void SetMenuIsExtended(bool isExtended)
		{
			menuOpen = isExtended;
			menuIsOrtho = ortho;
			menuStart = menuOpen ? MENU_EXTENDED : isMenuHidden;

			menuBackgroundColor.a = 0f;
			extendoNormalColor.a = menuBackgroundColor.a;
			extendoHoverColor.a = (menuBackgroundColor.a / .5f);

			m_ExtendoStyle.normal.background = menuOpen ? m_ExtendoClosed : m_ExtendoOpen;
			m_ExtendoStyle.hover.background = menuOpen ? m_ExtendoClosed : m_ExtendoOpen;

			foreach (System.Action<bool> listener in toolbarEventSubscribers)
				listener(menuOpen);

			EditorPrefs.SetBool(pg_PreferenceKeys.ProGridsIsExtended, menuOpen);
		}
#endregion

#region ONSCENEGUI

#if PROFILE_TIMES
		pb_Profiler profiler = new pb_Profiler();
#endif

		public void OnSceneGUI(SceneView scnview)
		{
			bool isCurrentView = scnview == SceneView.lastActiveSceneView;

			if (isCurrentView)
			{
				Handles.BeginGUI();
				DrawSceneGUI();
				Handles.EndGUI();
			}

			// don't snap stuff in play mode
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			Event e = Event.current;

			// repaint scene gui if mouse is near controls
			if (isCurrentView && e.type == EventType.MouseMove)
			{
				bool tmp = extendoButtonHovering;
				extendoButtonHovering = extendoButtonRect.Contains(e.mousePosition);

				if (extendoButtonHovering != tmp)
					DoGridRepaint();

				mouseOverMenu = backgroundRect.Contains(e.mousePosition);
			}

			if (e.Equals(Event.KeyboardEvent(k_AxisConstraintKey)))
			{
				m_ToggleAxisConstraint = true;
			}

			if (e.Equals(Event.KeyboardEvent(k_TempDisableKey)))
			{
				m_ToggleTempSnap = true;
			}

			if (e.isKey)
			{
				m_ToggleAxisConstraint = false;
				m_ToggleTempSnap = false;
				bool used = true;

				if (e.keyCode == m_IncreaseGridSizeShortcut)
				{
					if (e.type == EventType.KeyUp)
						IncreaseGridSize();
				}
				else if (e.keyCode == m_DecreaseGridSizeShortcut)
				{
					if (e.type == EventType.KeyUp)
						DecreaseGridSize();
				}
				else if (e.keyCode == m_NudgePerspectiveBackwardShortcut)
				{
					if (e.type == EventType.KeyUp && !fullGrid && !ortho && m_GridIsLocked)
						MenuNudgePerspectiveBackward();
				}
				else if (e.keyCode == m_NudgePerspectiveForwardShortcut)
				{
					if (e.type == EventType.KeyUp && !fullGrid && !ortho && m_GridIsLocked)
						MenuNudgePerspectiveForward();
				}
				else if (e.keyCode == m_NudgePerspectiveResetShortcut)
				{
					if (e.type == EventType.KeyUp && !fullGrid && !ortho && m_GridIsLocked)
						MenuNudgePerspectiveReset();
				}
				else if (e.keyCode == m_CyclePerspectiveShortcut)
				{
					if (e.type == EventType.KeyUp)
						CyclePerspective();
				}
				else
				{
					used = false;
				}

				if (used)
					e.Use();
			}

			Camera cam = Camera.current;

			if (cam == null)
				return;

			ortho = cam.orthographic && IsRounded(scnview.rotation.eulerAngles.normalized);

			camDir = pg_Util.CeilFloor(pivot - cam.transform.position);

			if (ortho && !prevOrtho || ortho != menuIsOrtho)
				OnSceneBecameOrtho(isCurrentView);

			if (!ortho && prevOrtho)
				OnSceneBecamePersp(isCurrentView);

			prevOrtho = ortho;

			float camDistance = Vector3.Distance(cam.transform.position, lastPivot);    // distance from camera to pivot

			if (fullGrid)
			{
				pivot = m_GridIsLocked || Selection.activeTransform == null ? pivot : Selection.activeTransform.position;
			}
			else
			{
				Vector3 sceneViewPlanePivot = pivot;

				Ray ray = new Ray(cam.transform.position, cam.transform.forward);
				Plane plane = new Plane(Vector3.up, pivot);
				float dist;

				// the only time a locked grid should ever move is if it's pivot is out
				// of the camera's frustum.
				if ((m_GridIsLocked && !cam.InFrustum(pivot)) || !m_GridIsLocked || scnview != SceneView.lastActiveSceneView)
				{
					if (plane.Raycast(ray, out dist))
						sceneViewPlanePivot = ray.GetPoint(Mathf.Min(dist, planeGridDrawDistance / 2f));
					else
						sceneViewPlanePivot = ray.GetPoint(Mathf.Min(cam.farClipPlane / 2f, planeGridDrawDistance / 2f));
				}

				if (m_GridIsLocked)
				{
					pivot = pg_Enum.InverseAxisMask(sceneViewPlanePivot, m_RenderPlane) + pg_Enum.AxisMask(pivot, m_RenderPlane);
				}
				else
				{
					pivot = Selection.activeTransform == null ? pivot : Selection.activeTransform.position;

					if (Selection.activeTransform == null || !cam.InFrustum(pivot))
					{
						pivot = pg_Enum.InverseAxisMask(sceneViewPlanePivot, m_RenderPlane) + pg_Enum.AxisMask(Selection.activeTransform == null ? pivot : Selection.activeTransform.position, m_RenderPlane);
					}
				}
			}

#if PG_DEBUG
		pivotGo.transform.position = pivot;
#endif

			if (m_DrawGrid)
			{
				if (ortho)
				{
					// ortho don't care about pivots
					DrawGridOrthographic(cam);
				}
				else
				{
#if PROFILE_TIMES
				profiler.LogStart("DrawGridPerspective");
#endif

					if (m_DoGridRepaint || pivot != lastPivot || Mathf.Abs(camDistance - lastDistance) > lastDistance / 2 || camDir != prevCamDir)
					{
						prevCamDir = camDir;
						m_DoGridRepaint = false;
						lastPivot = pivot;
						lastDistance = camDistance;

						if (fullGrid)
						{
							//  if perspective and 3d, use pivot like normal
							pg_GridRenderer.DrawGridPerspective(cam, pivot, m_SnapValue, new Color[3] { gridColorX, gridColorY, gridColorZ }, alphaBump);
						}
						else
						{
							if ((m_RenderPlane & Axis.X) == Axis.X)
								planeGridDrawDistance = pg_GridRenderer.DrawPlane(cam, pivot + Vector3.right * offset, Vector3.up, Vector3.forward, m_SnapValue, gridColorX, alphaBump);

							if ((m_RenderPlane & Axis.Y) == Axis.Y)
								planeGridDrawDistance = pg_GridRenderer.DrawPlane(cam, pivot + Vector3.up * offset, Vector3.right, Vector3.forward, m_SnapValue, gridColorY, alphaBump);

							if ((m_RenderPlane & Axis.Z) == Axis.Z)
								planeGridDrawDistance = pg_GridRenderer.DrawPlane(cam, pivot + Vector3.forward * offset, Vector3.up, Vector3.right, m_SnapValue, gridColorZ, alphaBump);

						}
					}
#if PROFILE_TIMES
				profiler.LogFinish("DrawGridPerspective");
#endif
				}
			}

			// Always keep track of the selection
			if (!Selection.transforms.Contains(m_LastActiveTransform))
			{
				if (Selection.activeTransform)
				{
					m_LastActiveTransform = Selection.activeTransform;
					m_LastPosition = Selection.activeTransform.position;
					m_LastScale = Selection.activeTransform.localScale;
				}
			}


			if (e.type == EventType.MouseUp)
				firstMove = true;

			if (!m_SnapEnabled || GUIUtility.hotControl < 1)
				return;

			// Bugger.SetKey("Toggle Snap Off", toggleTempSnap);

			/**
			 *	Snapping (for all the junk in PG, this method is literally the only code that actually affects anything).
			 */
			if (Selection.activeTransform && pg_Util.SnapIsEnabled(Selection.activeTransform))
			{
				if (!FuzzyEquals(m_LastActiveTransform.position, m_LastPosition))
				{
					Transform selected = m_LastActiveTransform;

					if (!m_ToggleTempSnap)
					{
						Vector3 old = selected.position;
						Vector3 mask = old - m_LastPosition;

						bool constraintsOn = m_ToggleAxisConstraint ? !useAxisConstraints : useAxisConstraints;

						if (constraintsOn)
							selected.position = pg_Util.SnapValue(old, mask, m_SnapValue);
						else
							selected.position = pg_Util.SnapValue(old, m_SnapValue);

						Vector3 offset = selected.position - old;

						if (predictiveGrid && firstMove && !fullGrid)
						{
							firstMove = false;
							Axis dragAxis = pg_Util.CalcDragAxis(offset, scnview.camera);

							if (dragAxis != Axis.None && dragAxis != m_RenderPlane)
								SetRenderPlane(dragAxis);
						}

						if (m_SnapAsGroup)
						{
							OffsetTransforms(Selection.transforms, selected, offset);
						}
						else
						{
							foreach (Transform t in Selection.transforms)
								t.position = constraintsOn ? pg_Util.SnapValue(t.position, mask, m_SnapValue) : pg_Util.SnapValue(t.position, m_SnapValue);
						}
					}

					m_LastPosition = selected.position;
				}

				if (!FuzzyEquals(m_LastActiveTransform.localScale, m_LastScale) && m_ScaleSnapEnabled)
				{
					if (!m_ToggleTempSnap)
					{
						Vector3 old = m_LastActiveTransform.localScale;
						Vector3 mask = old - m_LastScale;

						if (predictiveGrid)
						{
							Axis dragAxis = pg_Util.CalcDragAxis(Selection.activeTransform.TransformDirection(mask), scnview.camera);
							if (dragAxis != Axis.None && dragAxis != m_RenderPlane)
								SetRenderPlane(dragAxis);
						}

						foreach (Transform t in Selection.transforms)
							t.localScale = pg_Util.SnapValue(t.localScale, mask, m_SnapValue);

						m_LastScale = m_LastActiveTransform.localScale;
					}
				}
			}
		}

		void OnSelectionChange()
		{
			// Means we don't have to wait for script reloads
			// to respect IgnoreSnap attribute, and keeps the
			// cache small.
			pg_Util.ClearSnapEnabledCache();
		}

		void OnSceneBecameOrtho(bool isCurrentView)
		{
			pg_GridRenderer.Destroy();

			if (isCurrentView && ortho != menuIsOrtho)
				SetMenuIsExtended(menuOpen);
		}

		void OnSceneBecamePersp(bool isCurrentView)
		{
			if (isCurrentView && ortho != menuIsOrtho)
				SetMenuIsExtended(menuOpen);
		}
#endregion

#region GRAPHICS

		GameObject go;

		private void DrawGridOrthographic(Camera cam)
		{
			Axis camAxis = AxisWithVector(Camera.current.transform.TransformDirection(Vector3.forward).normalized);

			if (m_DrawGrid)
			{
				switch (camAxis)
				{
					case Axis.X:
					case Axis.NegX:
						DrawGridOrthographic(cam, camAxis, gridColorXPrimary, gridColorX);
						break;

					case Axis.Y:
					case Axis.NegY:
						DrawGridOrthographic(cam, camAxis, gridColorYPrimary, gridColorY);
						break;

					case Axis.Z:
					case Axis.NegZ:
						DrawGridOrthographic(cam, camAxis, gridColorZPrimary, gridColorZ);
						break;
				}
			}
		}

		int PRIMARY_COLOR_INCREMENT = 10;
		Color previousColor;
		private void DrawGridOrthographic(Camera cam, Axis camAxis, Color primaryColor, Color secondaryColor)
		{
			previousColor = Handles.color;
			Handles.color = primaryColor;

			Vector3 bottomLeft = pg_Util.SnapToFloor(cam.ScreenToWorldPoint(Vector2.zero), m_SnapValue);
			Vector3 bottomRight = pg_Util.SnapToFloor(cam.ScreenToWorldPoint(new Vector2(cam.pixelWidth, 0f)), m_SnapValue);
			Vector3 topLeft = pg_Util.SnapToFloor(cam.ScreenToWorldPoint(new Vector2(0f, cam.pixelHeight)), m_SnapValue);
			Vector3 topRight = pg_Util.SnapToFloor(cam.ScreenToWorldPoint(new Vector2(cam.pixelWidth, cam.pixelHeight)), m_SnapValue);

			Vector3 axis = VectorWithAxis(camAxis);

			float width = Vector3.Distance(bottomLeft, bottomRight);
			float height = Vector3.Distance(bottomRight, topRight);

			// Shift lines to 10m forward of the camera
			bottomLeft += axis * 10f;
			topRight += axis * 10f;
			bottomRight += axis * 10f;
			topLeft += axis * 10f;

			/**
			 *	Draw Vertical Lines
			 */
			Vector3 cam_right = cam.transform.right;
			Vector3 cam_up = cam.transform.up;

			float _snapVal = m_SnapValue;

			int segs = (int)Mathf.Ceil(width / _snapVal) + 2;

			float n = 2f;
			while (segs > k_MaxLines)
			{
				_snapVal = _snapVal * n;
				segs = (int)Mathf.Ceil(width / _snapVal) + 2;
				n++;
			}

			// Screen start and end
			Vector3 bl = cam_right.Sum() > 0 ? pg_Util.SnapToFloor(bottomLeft, cam_right, _snapVal * PRIMARY_COLOR_INCREMENT) : pg_Util.SnapToCeil(bottomLeft, cam_right, _snapVal * PRIMARY_COLOR_INCREMENT);
			Vector3 start = bl - cam_up * (height + _snapVal * 2);
			Vector3 end = bl + cam_up * (height + _snapVal * 2);

			segs += PRIMARY_COLOR_INCREMENT;

			// The current line start and end
			Vector3 line_start = Vector3.zero;
			Vector3 line_end = Vector3.zero;

			for (int i = -1; i < segs; i++)
			{
				line_start = start + (i * (cam_right * _snapVal));
				line_end = end + (i * (cam_right * _snapVal));
				Handles.color = i % PRIMARY_COLOR_INCREMENT == 0 ? primaryColor : secondaryColor;
				Handles.DrawLine(line_start, line_end);
			}

			/**
			 * Draw Horizontal Lines
			 */
			segs = (int)Mathf.Ceil(height / _snapVal) + 2;

			n = 2;
			while (segs > k_MaxLines)
			{
				_snapVal = _snapVal * n;
				segs = (int)Mathf.Ceil(height / _snapVal) + 2;
				n++;
			}

			Vector3 tl = cam_up.Sum() > 0 ? pg_Util.SnapToCeil(topLeft, cam_up, _snapVal * PRIMARY_COLOR_INCREMENT) : pg_Util.SnapToFloor(topLeft, cam_up, _snapVal * PRIMARY_COLOR_INCREMENT);
			start = tl - cam_right * (width + _snapVal * 2);
			end = tl + cam_right * (width + _snapVal * 2);

			segs += (int)PRIMARY_COLOR_INCREMENT;

			for (int i = -1; i < segs; i++)
			{
				line_start = start + (i * (-cam_up * _snapVal));
				line_end = end + (i * (-cam_up * _snapVal));
				Handles.color = i % PRIMARY_COLOR_INCREMENT == 0 ? primaryColor : secondaryColor;
				Handles.DrawLine(line_start, line_end);
			}

			if (m_DrawAngles)
			{
				Vector3 cen = pg_Util.SnapValue(((topRight + bottomLeft) / 2f), m_SnapValue);

				float half = (width > height) ? width : height;

				float opposite = Mathf.Tan(Mathf.Deg2Rad * angleValue) * half;

				Vector3 up = cam.transform.up * opposite;
				Vector3 right = cam.transform.right * half;

				Vector3 bottomLeftAngle = cen - (up + right);
				Vector3 topRightAngle = cen + (up + right);

				Vector3 bottomRightAngle = cen + (right - up);
				Vector3 topLeftAngle = cen + (up - right);

				Handles.color = primaryColor;

				// y = 1x+1
				Handles.DrawLine(bottomLeftAngle, topRightAngle);

				// y = -1x-1
				Handles.DrawLine(topLeftAngle, bottomRightAngle);
			}

			Handles.color = previousColor;
		}

#endregion

#region ENUM UTILITY

		public SnapUnit SnapUnitWithString(string str)
		{
			foreach (SnapUnit su in SnapUnit.GetValues(typeof(SnapUnit)))
			{
				if (su.ToString() == str)
					return su;
			}
			return (SnapUnit)0;
		}

		public Axis AxisWithVector(Vector3 val)
		{
			Vector3 v = new Vector3(Mathf.Abs(val.x), Mathf.Abs(val.y), Mathf.Abs(val.z));

			if (v.x > v.y && v.x > v.z)
			{
				if (val.x > 0)
					return Axis.X;
				else
					return Axis.NegX;
			}
			else
			if (v.y > v.x && v.y > v.z)
			{
				if (val.y > 0)
					return Axis.Y;
				else
					return Axis.NegY;
			}
			else
			{
				if (val.z > 0)
					return Axis.Z;
				else
					return Axis.NegZ;
			}
		}

		public Vector3 VectorWithAxis(Axis axis)
		{
			switch (axis)
			{
				case Axis.X:
					return Vector3.right;
				case Axis.Y:
					return Vector3.up;
				case Axis.Z:
					return Vector3.forward;
				case Axis.NegX:
					return -Vector3.right;
				case Axis.NegY:
					return -Vector3.up;
				case Axis.NegZ:
					return -Vector3.forward;

				default:
					return Vector3.forward;
			}
		}

		public bool IsRounded(Vector3 v)
		{
			return (Mathf.Approximately(v.x, 1f) || Mathf.Approximately(v.y, 1f) || Mathf.Approximately(v.z, 1f)) || v == Vector3.zero;
		}

		public Vector3 RoundAxis(Vector3 v)
		{
			return VectorWithAxis(AxisWithVector(v));
		}
		#endregion

#region MOVING TRANSFORMS

		static bool FuzzyEquals(Vector3 lhs, Vector3 rhs)
		{
			return Mathf.Abs(lhs.x - rhs.x) < .001f && Mathf.Abs(lhs.y - rhs.y) < .001f && Mathf.Abs(lhs.z - rhs.z) < .001f;
		}

		public void OffsetTransforms(Transform[] trsfrms, Transform ignore, Vector3 offset)
		{
			foreach (Transform t in trsfrms)
			{
				if (t != ignore)
					t.position += offset;
			}
		}

		void HierarchyWindowChanged()
		{
			if (Selection.activeTransform != null)
				m_LastPosition = Selection.activeTransform.position;
		}

#endregion

#region SETTINGS

		public void SetSnapEnabled(bool enable)
		{
			EditorPrefs.SetBool(pg_PreferenceKeys.SnapEnabled, enable);

			if (Selection.activeTransform)
			{
				m_LastActiveTransform = Selection.activeTransform;
				m_LastPosition = Selection.activeTransform.position;
			}

			m_SnapEnabled = enable;
			DoGridRepaint();
		}

		public void SetSnapValue(SnapUnit su, float val, int multiplier)
		{
			int clamp_multiplier = (int)(Mathf.Min(Mathf.Max(1, multiplier), int.MaxValue));

			float value_multiplier = clamp_multiplier / (float) pg_Defaults.DefaultSnapMultiplier;

			/**
			 * multiplier is a value modifies the snap val.  100 = no change,
			 * 50 is half val, 200 is double val, etc.
			 */
			m_SnapValue = pg_Enum.SnapUnitValue(su) * val * value_multiplier;
			DoGridRepaint();

			EditorPrefs.SetInt(pg_PreferenceKeys.GridUnit, (int)su);
			EditorPrefs.SetFloat(pg_PreferenceKeys.SnapValue, val);
			EditorPrefs.SetInt(pg_PreferenceKeys.SnapMultiplier, clamp_multiplier);


			// update gui (only necessary when calling with editorpref values)
			m_UiSnapValue = val * value_multiplier;
			m_SnapUnit = su;

			switch (su)
			{
				case SnapUnit.Inch:
					PRIMARY_COLOR_INCREMENT = 12;   // blasted imperial units
					break;

				case SnapUnit.Foot:
					PRIMARY_COLOR_INCREMENT = 3;
					break;

				default:
					PRIMARY_COLOR_INCREMENT = 10;
					break;
			}

			if (EditorPrefs.GetBool(pg_PreferenceKeys.SyncUnitySnap, true))
			{
				EditorPrefs.SetFloat(pg_PreferenceKeys.UnityMoveSnapX, m_SnapValue);
				EditorPrefs.SetFloat(pg_PreferenceKeys.UnityMoveSnapY, m_SnapValue);
				EditorPrefs.SetFloat(pg_PreferenceKeys.UnityMoveSnapZ, m_SnapValue);

				if (EditorPrefs.GetBool(pg_PreferenceKeys.SnapScale, true))
					EditorPrefs.SetFloat(pg_PreferenceKeys.UnityScaleSnap, m_SnapValue);

				// If Unity snap sync is enabled, refresh the Snap Settings window if it's open.
				System.Type snapSettings = typeof(EditorWindow).Assembly.GetType("UnityEditor.SnapSettings");

				if (snapSettings != null)
				{
					FieldInfo snapInitialized = snapSettings.GetField("s_Initialized", BindingFlags.NonPublic | BindingFlags.Static);

					if (snapInitialized != null)
					{
						snapInitialized.SetValue(null, (object)false);

						EditorWindow win = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x => x.ToString().Contains("SnapSettings"));

						if (win != null)
							win.Repaint();
					}
				}
			}

			DoGridRepaint();
		}

		public void SetRenderPlane(Axis axis)
		{
			offset = 0f;
			fullGrid = false;
			m_RenderPlane = axis;
			EditorPrefs.SetBool(pg_PreferenceKeys.PerspGrid, fullGrid);
			EditorPrefs.SetInt(pg_PreferenceKeys.GridAxis, (int)m_RenderPlane);
			DoGridRepaint();
		}

		public void SetGridEnabled(bool enable)
		{
			m_DrawGrid = enable;

			if (!m_DrawGrid)
				pg_GridRenderer.Destroy();
			else
				pg_Util.SetUnityGridEnabled(false);

			EditorPrefs.SetBool(pg_PreferenceKeys.ShowGrid, enable);

			DoGridRepaint();
		}

		public void SetDrawAngles(bool enable)
		{
			m_DrawAngles = enable;
			DoGridRepaint();
		}

		void SnapToGrid(Transform[] transforms)
		{
			if (transforms != null)
			{
				Undo.RecordObjects(transforms, "Snap to Grid");

				foreach (Transform t in transforms)
					t.position = pg_Util.SnapValue(t.position, m_SnapValue);
			}

			DoGridRepaint();
		}
#endregion

#region GLOBAL SETTING

		internal bool GetUseAxisConstraints() { return m_ToggleAxisConstraint ? !useAxisConstraints : useAxisConstraints; }
		internal float GetSnapValue() { return m_SnapValue; }
		internal bool GetSnapEnabled() { return (m_ToggleTempSnap ? !m_SnapEnabled : m_SnapEnabled); }

		/// <returns>the value of useAxisConstraints, accounting for the shortcut key toggle.</returns>
		/// <remarks>Used by ProBuilder.</remarks>
		public static bool UseAxisConstraints()
		{
			return instance != null ? instance.GetUseAxisConstraints() : false;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns>The current snap value.</returns>
		/// <remarks>Used by ProBuilder.</remarks>
		public static float SnapValue()
		{
			return instance != null ? instance.GetSnapValue() : 0f;
		}

		/// <summary>
		/// </summary>
		/// <returns>True if snapping is enabled, false otherwise.</returns>
		/// <remarks>Used by ProBuilder.</remarks>
		public static bool SnapEnabled()
		{
			return instance == null ? false : instance.GetSnapEnabled();
		}

		/// <remarks>Used by ProBuilder.</remarks>
		public static void AddPushToGridListener(System.Action<float> listener)
		{
			pushToGridListeners.Add(listener);
		}

		/// <remarks>Used by ProBuilder.</remarks>
		public static void RemovePushToGridListener(System.Action<float> listener)
		{
			pushToGridListeners.Remove(listener);
		}

		/// <remarks>Used by ProBuilder.</remarks>
		public static void AddToolbarEventSubscriber(System.Action<bool> listener)
		{
			toolbarEventSubscribers.Add(listener);
		}

		/// <remarks>Used by ProBuilder.</remarks>
		public static void RemoveToolbarEventSubscriber(System.Action<bool> listener)
		{
			toolbarEventSubscribers.Remove(listener);
		}

		/// <remarks>Used by ProBuilder.</remarks>
		public static bool SceneToolbarActive()
		{
			return instance != null;
		}

		[SerializeField]
		static List<System.Action<float>> pushToGridListeners = new List<System.Action<float>>();
		[SerializeField]
		static List<System.Action<bool>> toolbarEventSubscribers = new List<System.Action<bool>>();

		private void PushToGrid(float snapValue)
		{
			foreach (System.Action<float> listener in pushToGridListeners)
				listener(snapValue);
		}

		/// <remarks>Used by ProBuilder.</remarks>
		public static void OnHandleMove(Vector3 worldDirection)
		{
			if (instance != null)
				instance.OnHandleMove_Internal(worldDirection);
		}

		void OnHandleMove_Internal(Vector3 worldDirection)
		{
			if (predictiveGrid && firstMove && !fullGrid)
			{
				firstMove = false;
				Axis dragAxis = pg_Util.CalcDragAxis(worldDirection, SceneView.lastActiveSceneView.camera);

				if (dragAxis != Axis.None && dragAxis != m_RenderPlane)
					SetRenderPlane(dragAxis);
			}
		}
#endregion
	}
}
