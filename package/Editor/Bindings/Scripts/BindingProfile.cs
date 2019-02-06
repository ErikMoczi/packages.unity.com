

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Tiny
{
    internal abstract class BindingProfile
    {
        #region Properties
        public TinyContext Context { get; private set; }
        public IRegistry Registry { get; private set; }
        public IBindingsManager Bindings { get; private set; }
        public List<TinyId> WithComponents { get; set; } = new List<TinyId>();
        public List<TinyId> WithoutComponents { get; set; } = new List<TinyId>();
        #endregion

        #region public API

        public void SetContext(TinyContext context)
        {
            Context = context;
            Registry = Context.Registry;
            Bindings = Context.GetManager<IBindingsManager>();
        }

        private static bool ValidateBindingsParams(TinyEntity entity)
        {
            return null != entity      &&
                   null != entity.View &&
                   entity.View;
        }

        public virtual void LoadBindings(TinyEntity entity)
        {
        }

        public virtual void UnloadBindings(TinyEntity entity)
        {
        }

        public virtual void Transfer(TinyEntity entity)
        {
        }

        public virtual void Transfer(GameObject go)
        {
        }

        public bool Conforms(TinyEntity entity)
        {
            return WithComponents.All(id => null != entity.GetComponent(id)) &&
                   ShouldRun(entity);
        }

        protected virtual bool ShouldRun(TinyEntity entity)
        {
            return true;
        }
        #endregion // Public API

        #region Class API

        protected TinyEntity GetEntity(GameObject go)
        {
            return go.GetComponent<TinyEntityView>()?.EntityRef.Dereference(Registry);
        }

        protected TComponent GetComponent<TComponent>(TinyEntity entity)
            where TComponent : Component
        {
            return !ValidateBindingsParams(entity) ? null : entity.View.GetComponent<TComponent>();
        }

        protected TComponent AddComponent<TComponent>(TinyEntity entity)
            where TComponent : Component
        {
            return AddComponent<TComponent>(entity, null);
        }

        protected TComponent AddComponent<TComponent>(TinyEntity entity, Action<TComponent> init)
            where TComponent : Component
        {
            if (!ValidateBindingsParams(entity))
            {
                return null;
            }

            var component = entity.View.gameObject.AddComponent<TComponent>();
            if (null != component)
            {
                init?.Invoke(component);
            }
            return component;
        }

        protected TComponent AddMissingComponent<TComponent>(TinyEntity entity)
            where TComponent : Component
        {
            return AddMissingComponent<TComponent>(entity, null);
        }

        protected TComponent AddMissingComponent<TComponent>(TinyEntity entity, Action<TComponent> init)
            where TComponent : Component
        {
            var component = GetComponent<TComponent>(entity);
            if (null == component)
            {
                component = AddComponent(entity, init);
            }
            return component;
        }

        protected void RemoveComponent<TComponent>(TinyEntity entity)
            where TComponent : Component
        {
            var component = GetComponent<TComponent>(entity);
            if (null != component)
            {
                UnityEngine.Object.DestroyImmediate(component, false);
            }
        }
        #endregion // Class API
    }
}

