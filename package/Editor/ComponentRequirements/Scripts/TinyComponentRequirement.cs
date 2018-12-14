

using System;
using System.Linq;
using UnityEngine;

namespace Unity.Tiny
{
    internal abstract class TinyComponentRequirement
    {
        public abstract void AddRequiredComponents(TinyEntity entity);

        protected static bool HasComponents(TinyEntity entity, params TinyType.Reference[] typeRefs)
        {
            return typeRefs.All(typeRef => null != entity.GetComponent(typeRef));
        }

        protected static TinyObject AddComponent(TinyEntity entity, TinyType.Reference typeRef)
        {
            return entity.GetOrAddComponent(typeRef);
        }

        protected static TinyObject AddComponentAfter(TinyEntity entity, TinyType.Reference typeRef, TinyType.Reference after)
        {
            var index = entity.GetComponentIndex(after);
            if (index >= 0)
            {
                index += 1;
            }
            return AddComponentAtIndex(entity, typeRef, index);
        }

        protected static TinyObject AddComponentAtIndex(TinyEntity entity, TinyType.Reference typeRef, int index)
        {
            var component = AddComponent(entity, typeRef);
            entity.Components.Remove(component);
            if (index < 0)
            {
                index = entity.Components.Count;
            }
            entity.Components.Insert(Mathf.Clamp(index, 0, entity.Components.Count), component);
            return component;
        }
    }
}

