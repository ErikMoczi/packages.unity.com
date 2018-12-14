

using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
	[UsedImplicitly]
	internal class TextMeshInvertedBindings : InvertedBindingsBase<TextMesh>
	{
		#region Static
		[TinyInitializeOnLoad]
		[UsedImplicitly]
		private static void Register()
		{
			GameObjectTracker.RegisterForComponentModification<TextMesh>(SyncTextMesh);
		}

		private static void SyncTextMesh(TextMesh from, TinyEntityView view)
		{
			var registry = view.Registry;
			var entity = view.EntityRef.Dereference(registry);

			var tetRenderer = entity.GetComponent(TypeRefs.TextJS.TextRenderer);
			if (null != tetRenderer)
			{
				SyncTextMesh(from, tetRenderer);
			}
		}

		private static void SyncTextMesh(TextMesh from, [NotNull] TinyObject textRenderer)
		{
			from.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("GUI/Text Shader"));
			from.characterSize = 10;
			from.lineSpacing = 1;
			from.richText = false;
			from.alignment = TextAlignment.Left;

			textRenderer.Refresh();
			textRenderer.AssignIfDifferent("text", from.text);
			textRenderer.AssignIfDifferent("fontSize", from.fontSize);
			textRenderer.AssignIfDifferent("bold", (from.fontStyle & FontStyle.Bold) == FontStyle.Bold);
			textRenderer.AssignIfDifferent("italic", (from.fontStyle & FontStyle.Italic) == FontStyle.Italic);
			textRenderer.AssignIfDifferent("color", from.color);
			textRenderer.AssignIfDifferent("font", from.font);
			textRenderer.AssignIfDifferent("anchor", from.anchor);
		}

		#endregion

		#region InvertedBindingsBase<TextMesh>
		public override void Create(TinyEntityView view, TextMesh from)
		{
			var tr = new TinyObject(view.Registry, GetMainTinyType());
			SyncTextMesh(from, tr);

			var entity = view.EntityRef.Dereference(view.Registry);
			var textRenderer = entity.GetOrAddComponent(GetMainTinyType());
			textRenderer.CopyFrom(tr);
		}

		public override TinyType.Reference GetMainTinyType()
		{
			return TypeRefs.TextJS.TextRenderer;
		}
		#endregion
	}
}

