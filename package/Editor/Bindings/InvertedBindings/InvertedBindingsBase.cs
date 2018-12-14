

using UnityEngine;
using UnityEngine.Assertions;


namespace Unity.Tiny
{
	internal abstract class InvertedBindingsBase<TComponent> : IInvertedBindings<TComponent>
		where TComponent : Component
	{
		public abstract void Create(TinyEntityView view, TComponent from);

		public abstract TinyType.Reference GetMainTinyType();

		public void Create(TinyEntityView view, Component from)
		{
			var c = (TComponent) from;
			Assert.IsNotNull(c);
			Create(view, c);
		}
	}
}

