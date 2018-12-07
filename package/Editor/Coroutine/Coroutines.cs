
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Unity.Tiny
{
	internal static class Coroutines
	{
		#region Properties
		private static List<TinyCoroutine> CurrentCoroutines { get; } = new List<TinyCoroutine>();
		private static HashSet<IEnumerator> RegisteredEnumerators { get; } = new HashSet<IEnumerator>();
		private static HashSet<object> MarkTargetAsCancelled { get; } = new HashSet<object>();
		private static HashSet<TinyCoroutine> MarkCoroutineAsCancelled { get; } = new HashSet<TinyCoroutine>();
		#endregion

		#region API

		public static TinyCoroutine StartCoroutine(IEnumerator coroutine)
			=> StartCoroutine(coroutine, null);

		public static TinyCoroutine StartCoroutine(IEnumerator coroutine, object target)
		{
			if (null == coroutine)
			{
				throw new NullReferenceException(nameof(coroutine));
			}

			if (coroutine.GetType().IsValueType)
			{
				throw new NotSupportedException($"{TinyConstants.ApplicationName}: structs deriving from IEnumerator are not supported.");
			}

			if (!RegisteredEnumerators.Add(coroutine))
			{
				throw new NotSupportedException($"{TinyConstants.ApplicationName}: starting multiple coroutins on the same IEnumerator instance is not supported.");
			}

			var routine = new TinyCoroutine(coroutine, target);
			CurrentCoroutines.Add(routine);
			return routine;
		}

		public static void StopCoroutine(TinyCoroutine coroutine)
			=> MarkCoroutineAsCancelled.Add(coroutine);

		public static void StopAllCoroutines(object target)
			=> MarkTargetAsCancelled.Add(target);

		public static void StopAllCoroutines()
			=> MarkCoroutineAsCancelled.UnionWith(CurrentCoroutines);

		public static bool HasCoroutinesRunning
			=> CurrentCoroutines.Count > 0;
		#endregion

		#region Implementation
		[TinyInitializeOnLoad]
		private static void StartListening()
		{
			Bridge.EditorApplication.RegisterGlobalUpdate(MoveAllCoroutines);
		}

		[TinyInitializeOnLoad]
		[UsedImplicitly]
		private static void Init()
		{
			TinyEditorApplication.OnCloseProject += HandleCloseProject;
		}

		private static void HandleCloseProject(TinyProject project, TinyContext context)
		{
			foreach (var coroutine in CurrentCoroutines)
			{
				switch (coroutine.OnCloseProject)
				{
					case TinyCoroutineLifetime.Cancel:
						coroutine.Cancel();
						break;
				}
			}
		}

		private static void MoveAllCoroutines()
		{
			// While iterating the coroutines, we may spawn more coroutines or request to stop some, so we'll cache all
			// the current data and operate on it instead of mutating the actual data.
			var stopByTargetThisFrame = HashSetPool<object>.Get();
			var stopCoroutine = HashSetPool<TinyCoroutine>.Get();
			var currentlyRunning = ListPool<TinyCoroutine>.Get();
			var toRemove = ListPool<TinyCoroutine>.Get();

			try
			{
				stopByTargetThisFrame.UnionWith(MarkTargetAsCancelled);
				MarkTargetAsCancelled.Clear();
				stopCoroutine.UnionWith(MarkCoroutineAsCancelled);
				MarkCoroutineAsCancelled.Clear();

				currentlyRunning.AddRange(CurrentCoroutines);
				foreach (var coroutine in currentlyRunning)
				{
					if (stopByTargetThisFrame.Contains(coroutine.Target) ||
						stopCoroutine.Contains(coroutine)                ||
						!coroutine.MoveNext())
					{
						toRemove.Add(coroutine);
					}
				}
			}
			finally
			{
				ListPool<TinyCoroutine>.Release(currentlyRunning);
				foreach (var coroutine in toRemove)
				{
					CurrentCoroutines.Remove(coroutine);
					RegisteredEnumerators.Remove(coroutine.Routine);
				}

				ListPool<TinyCoroutine>.Release(toRemove);
				HashSetPool<TinyCoroutine>.Release(stopCoroutine);
				HashSetPool<object>.Release(stopByTargetThisFrame);
			}
		}
		#endregion
	}
}

