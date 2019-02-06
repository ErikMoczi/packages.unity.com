
﻿
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
	[UsedImplicitly]
	internal class TransformInvertedBindings : InvertedBindingsBase<Transform>
	{
		#region Static
		[TinyInitializeOnLoad]
		[UsedImplicitly]
		private static void Register()
		{
			GameObjectTracker.RegisterForComponentModification<Transform>(SyncTransform);
		}

		public static void SyncTransform(Transform from, TinyEntityView view)
		{
			var registry = view.Registry;
			var entity = view.EntityRef.Dereference(registry);

			var tinyNode = entity.GetComponent(TypeRefs.Core2D.TransformNode);
			if (null != tinyNode)
			{
				SyncTransformNode(from, tinyNode);
			}

			TinyObject tinyPosition;
			if (from.localPosition != Vector3.zero)
			{
				tinyPosition = entity.GetOrAddComponent(TypeRefs.Core2D.TransformLocalPosition);
			}
			else
			{
				tinyPosition = entity.GetComponent(TypeRefs.Core2D.TransformLocalPosition);
			}

			if (null != tinyPosition)
			{
				SyncTransformLocalPosition(from, tinyPosition);
			}

			TinyObject tinyRotation;
			if (from.localRotation != Quaternion.identity)
			{
				tinyRotation = entity.GetOrAddComponent(TypeRefs.Core2D.TransformLocalRotation);
			}
			else
			{
				tinyRotation = entity.GetComponent(TypeRefs.Core2D.TransformLocalRotation);
			}

			if (null != tinyRotation)
			{
				SyncTransformLocalRotation(from, tinyRotation);
			}

			TinyObject tinyScale;
			if (from.localScale != Vector3.one)
			{
				tinyScale = entity.GetOrAddComponent(TypeRefs.Core2D.TransformLocalScale);
			}
			else
			{
				tinyScale = entity.GetComponent(TypeRefs.Core2D.TransformLocalScale);
			}

			if (null != tinyScale)
			{
				SyncTransformLocalScale(from, tinyScale);
			}
		}

		private static void SyncTransformNode(Transform t, [NotNull] TinyObject tiny)
		{
			if (t.parent)
			{
				tiny.AssignIfDifferent("parent", t.parent.GetComponent<TinyEntityView>()?.EntityRef ?? TinyEntity.Reference.None);
			}
			else
			{
				tiny.AssignIfDifferent("parent", TinyEntity.Reference.None);
			}
		}

		private static void SyncTransformLocalPosition(Transform t, [NotNull] TinyObject tiny)
		{
			tiny.AssignIfDifferent("position", t.localPosition);
		}

		private static void SyncTransformLocalRotation(Transform t, [NotNull] TinyObject tiny)
		{
			tiny.AssignIfDifferent("rotation", t.localRotation);
		}

		private static void SyncTransformLocalScale(Transform t, [NotNull] TinyObject tiny)
		{
			tiny.AssignIfDifferent("scale", t.localScale);
		}

		internal static void CreateNewFromExisting(TinyEntityView view, Transform t)
		{
			var registry = view.Registry;
			var nodeType = TypeRefs.Core2D.TransformNode;
			var entityGroupManager = view.Context.GetManager<IEntityGroupManagerInternal>();

			var parentRef = TinyEntity.Reference.None;
			if (t.parent)
			{
				parentRef = t.parent.GetComponent<TinyEntityView>()?.EntityRef ?? TinyEntity.Reference.None;
			}

			var graph = entityGroupManager.GetSceneGraph(
				parentRef.Equals(TinyEntity.Reference.None) ?
					entityGroupManager.ActiveEntityGroup
					: (TinyEntityGroup.Reference)parentRef.Dereference(registry).EntityGroup);

			if (null == graph)
			{
				return;
			}

			var node = new TinyObject(registry, nodeType);
			SyncTransformNode(t, node);
			var positionType = TypeRefs.Core2D.TransformLocalPosition;
			var position = new TinyObject(registry, positionType);
			SyncTransformLocalPosition(t, position);
			var rotationType = TypeRefs.Core2D.TransformLocalRotation;
			var rotation = new TinyObject(registry, rotationType);
			SyncTransformLocalRotation(t, rotation);
			var scaleType = TypeRefs.Core2D.TransformLocalScale;
			var scale = new TinyObject(registry, scaleType);
			SyncTransformLocalScale(t, scale);

			var entityNode = graph.CreateFromExisting(t, t.parent);
			var entity = entityNode.EntityRef.Dereference(registry);
			view.EntityRef = (TinyEntity.Reference)entity;

			{
				var tiny = entity.GetOrAddComponent(nodeType);
				tiny.CopyFrom(node);
			}
			{
				var tiny = entity.GetOrAddComponent(positionType);
				tiny.CopyFrom(position);
			}
			{
				var tiny = entity.GetOrAddComponent(rotationType);
				tiny.CopyFrom(rotation);
			}
			{
				var tiny = entity.GetOrAddComponent(scaleType);
				tiny.CopyFrom(scale);
			}
		}
		#endregion

		#region InvertedBindingsBase<Transform>
		public sealed override void Create(TinyEntityView view, Transform t)
		{
			CreateNewFromExisting(view, t);
		}

		public sealed override TinyType.Reference GetMainTinyType()
		{
			return TypeRefs.Core2D.TransformNode;
		}
		#endregion
	}
}

