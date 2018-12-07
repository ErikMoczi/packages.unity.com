

using System.Collections.Generic;
using System.Linq;

namespace Unity.Tiny
{
	/// <summary>
	/// Panel represents a high level class for drawing a collection of elements
	/// 
	/// NOTE: The sub elements system is WIP and still needs some work
	/// </summary>
	internal class TinyPanel : IDrawable, IDirtyable
	{
		private readonly List<IDrawable> m_Elements = new List<IDrawable>();
        
		public void AddElement(IDrawable element)
		{
			m_Elements.Add(element);
		}

		public void SetElement(int index, IDrawable element)
		{
			if ((uint) index > m_Elements.Count)
			{
				return;
			}
            
			m_Elements[index] = element;
		}

		public virtual bool DrawLayout()
		{
			return m_Elements.Aggregate(false, (current, element) => current | element?.DrawLayout() ?? false);
		}

		public void SetDirty()
		{
			foreach (var element in m_Elements)
			{
				var dirtyable = element as IDirtyable;
				dirtyable?.SetDirty();
			}
		}
	}
}

