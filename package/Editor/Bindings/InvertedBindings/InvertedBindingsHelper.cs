

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
	internal static class InvertedBindingsHelper
	{
		private static readonly Dictionary<Type, IInvertedBindings> k_InvertedBindings = new Dictionary<Type, IInvertedBindings>();

		public static IInvertedBindings GetInvertedBindings(Type type)
		{
			k_InvertedBindings.TryGetValue(type, out var bindings);
			return bindings;
		}

		[UsedImplicitly]
		public static void Register<TComponent>(IInvertedBindings<TComponent> invertedBindings) where TComponent : Component
		{
			var type = typeof(TComponent);
			if (k_InvertedBindings.ContainsKey(type))
			{
				Debug.LogError($"{TinyConstants.ApplicationName}: Inverted bindings for class {typeof(TComponent).Name} is already defined.");
			}
			else
			{
				k_InvertedBindings[type] = invertedBindings;
			}
		}
	}
}

