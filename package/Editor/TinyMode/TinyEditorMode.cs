

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace Unity.Tiny
{
	[UsedImplicitly]
	internal class TinyEditorMode : EditorModeProxy
	{
		[TinyInitializeOnLoad(int.MaxValue)]
		[UsedImplicitly]
		private static void Register()
		{
			TinyEditorApplication.OnLoadProject += HandleProjectLoaded;
			TinyEditorApplication.OnCloseProject += HandleProjectClosed;
		}

		private static void HandleProjectClosed(TinyProject project, TinyContext context)
		{
			TinyEditorBridge.RequestExitMode<TinyEditorMode>();
		}

		private static void HandleProjectLoaded(TinyProject project, TinyContext context)
		{
			TinyEditorBridge.RequestEnterMode<TinyEditorMode>();
		}

		public override void OnEnterMode(EditorModeContextProxy context)
		{
			Name = "Tiny Mode";
			foreach (var windowType in TinyEditorBridge.UnsupportedWindows)
			{
				context.RegisterAsUnsupported(windowType);
			}

			foreach (var windowName in TinyEditorBridge.UnsupportedWindowsByName)
			{
				if (TryFindType(windowName, out var type))
				{
					context.RegisterAsUnsupported(type);
				}
				else
				{
					Console.WriteLine($"{TinyConstants.ApplicationName}: Could not find the type of {windowName} to set it as unsupported.");
				}
			}

			// Overrides for Unity Types
			context.RegisterOverride<TinyInspector>(TinyEditorBridge.CoreWindowTypes.Inspector);
			context.RegisterOverride<TinyHierarchyWindow>(TinyEditorBridge.CoreWindowTypes.Hierarchy);
            context.RegisterOverride<TinyGameView>(TinyEditorBridge.CoreWindowTypes.GameView);
        }

		public override void OnExitMode()
		{
			if (null != TinyEditorApplication.Project)
			{
				TinyEditorApplication.Close(false);
			}
		}

		static Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
		public static bool TryFindType(string typeName, out Type t) {
			lock (typeCache) {
				if (!typeCache.TryGetValue(typeName, out t)) {
					foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
						t = a.GetType(typeName);
						if (t != null)
							break;
					}
					typeCache[typeName] = t; // perhaps null
				}
			}
			return t != null;
		}
	}
}

