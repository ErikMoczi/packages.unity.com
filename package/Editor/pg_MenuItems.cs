using UnityEditor;

namespace ProGrids.Editor
{
	static class pg_MenuItems
	{
		[MenuItem("Tools/ProGrids/About", false, 0)]
		public static void MenuAboutProGrids()
		{
			pg_AboutWindow.Init(true);
		}

		[MenuItem("Tools/ProGrids/ProGrids Window", false, 15)]
		public static void InitProGrids()
		{
			pg_Editor.Init();
			SceneView.RepaintAll();
		}

		[MenuItem("Tools/ProGrids/Close ProGrids", true, 200)]
		public static bool VerifyCloseProGrids()
		{
			return pg_Editor.IsEnabled();
		}

		[MenuItem("Tools/ProGrids/Close ProGrids")]
		public static void CloseProGrids()
		{
			pg_Editor.Close();
		}

		[MenuItem("Tools/ProGrids/Cycle SceneView Projection", false, 101)]
		public static void CyclePerspective()
		{
			pg_Editor.CyclePerspective();
		}

		[MenuItem("Tools/ProGrids/Cycle SceneView Projection", true, 101)]
		[MenuItem("Tools/ProGrids/Increase Grid Size", true, 203)]
		[MenuItem("Tools/ProGrids/Decrease Grid Size", true, 202)]
		public static bool VerifyGridSizeAdjustment()
		{
			return pg_Editor.instance != null;
		}

		[MenuItem("Tools/ProGrids/Decrease Grid Size", false, 202)]
		public static void DecreaseGridSize()
		{
			pg_Editor.DecreaseGridSize();
		}

		[MenuItem("Tools/ProGrids/Increase Grid Size", false, 203)]
		public static void IncreaseGridSize()
		{
			pg_Editor.IncreaseGridSize();
		}

		[MenuItem("Tools/ProGrids/Nudge Perspective Backward", true, 304)]
		[MenuItem("Tools/ProGrids/Nudge Perspective Forward", true, 305)]
		[MenuItem("Tools/ProGrids/Reset Perspective Nudge", true, 306)]
		public static bool VerifyMenuNudgePerspective()
		{
			return pg_Editor.IsEnabled() && !pg_Editor.instance.fullGrid && !pg_Editor.instance.ortho && pg_Editor.instance.gridIsLocked;
		}

		[MenuItem("Tools/ProGrids/Nudge Perspective Backward", false, 304)]
		public static void MenuNudgePerspectiveBackward()
		{
			pg_Editor.MenuNudgePerspectiveBackward();
		}

		[MenuItem("Tools/ProGrids/Nudge Perspective Forward", false, 305)]
		public static void MenuNudgePerspectiveForward()
		{
			pg_Editor.MenuNudgePerspectiveForward();
		}

		[MenuItem("Tools/ProGrids/Reset Perspective Nudge", false, 306)]
		public static void MenuNudgePerspectiveReset()
		{
			pg_Editor.MenuNudgePerspectiveReset();
		}
	}
}
