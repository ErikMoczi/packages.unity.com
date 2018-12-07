

using UnityEngine;

namespace Unity.Tiny
{
    internal static class GameObjectExtensions
    {
        public static TComponent AddMissingComponent<TComponent>(this GameObject go) where TComponent: Component
        {
            var component = go.GetComponent<TComponent>();
            if (null == component)
            {
                component = go.AddComponent<TComponent>();
            }
            return component;
        }
    }
}

