

using UnityEngine;

namespace Unity.Tiny
{
	internal interface IInvertedBindings
	{
		TinyType.Reference GetMainTinyType();
		void Create(TinyEntityView view, Component @from);
	}

	internal interface IInvertedBindings<TComponent> : IInvertedBindings
		where TComponent : Component
	{
		void Create(TinyEntityView view, TComponent component);
	}
}

