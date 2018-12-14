

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
	internal static class TinyRepaintHelper
	{
		private static readonly HashSet<Type> s_ChangedTypes = new HashSet<Type>();
		private static readonly Dictionary<Type, List<Action>> s_RepaintMethods = new Dictionary<Type, List<Action>>();

		[TinyInitializeOnLoad]
		private static void Register()
		{
			TinyEditorApplication.OnLoadProject += HandleProjectLoaded;
			RegisterAutoRepaint();
		}

		private static void HandleProjectLoaded(TinyProject project, TinyContext context)
		{
			var caretaker = context.Caretaker;
			caretaker.OnGenerateMemento += HandleGenerateMemento;
			caretaker.OnBeginUpdate   += HandleBeginUpdate;
			caretaker.OnEndUpdate     += HandleEndUpdate;

			var undo = context.GetManager<IUndoManager>();
			undo.OnUndoPerformed += changes => RepaintAll();
			undo.OnRedoPerformed += changes => RepaintAll();
		}

		private static void RegisterAutoRepaint()
		{
			foreach (var typeAttribute in TinyAttributeScanner.GetTypeAttributes<AutoRepaintOnTypeChangeAttribute>())
			{
				var repaintType = typeAttribute.Type;
				var repaintAllMethod = repaintType.GetMethod("RepaintAll", BindingFlags.Static | BindingFlags.Public);
				if (null == repaintAllMethod || repaintAllMethod.GetParameters().Length > 0 || repaintAllMethod.IsGenericMethod || repaintAllMethod.IsAbstract)
				{
					Debug.Log($"{TinyConstants.ApplicationName}: To enable the AutoRepaint feature, the type must have the public static method `void RepaintAll()`.");
					continue;
				}

				var tinyType = typeAttribute.Attribute.TinyType;
				if (null == tinyType)
				{
					Debug.Log($"{TinyConstants.ApplicationName}: The AutoRepaint feature will not work if no type is provided.");
					continue;
				}

				if (!tinyType.IsSubclassOf(typeof(TinyRegistryObjectBase)))
				{
					Debug.Log($"{TinyConstants.ApplicationName}: The AutoRepaint feature will only work for subclasses of {nameof(TinyRegistryObjectBase)}.");
					continue;
				}

				if (!s_RepaintMethods.TryGetValue(tinyType, out var repaintMethods))
				{
					s_RepaintMethods[tinyType] = repaintMethods = new List<Action>();
				}

				repaintMethods.Add((Action)Delegate.CreateDelegate(typeof(Action), repaintAllMethod));
			}
		}

		private static void HandleBeginUpdate()
		{
			s_ChangedTypes.Clear();
		}

		private static void HandleEndUpdate()
		{
			foreach (var target in s_ChangedTypes)
			{
				if (s_RepaintMethods.TryGetValue(target, out var repaintMethods))
				{
					repaintMethods.ForEach(m => m());
				}
			}
		}

		private static void HandleGenerateMemento(IOriginator originator, IMemento memento)
		{
			s_ChangedTypes.Add(originator.GetType());
		}

		private static void RepaintAll()
		{
			TinyEditorUtility.RepaintAllWindows();
		}
	}
}

