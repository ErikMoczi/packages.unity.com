

using System;
using UnityEngine;

namespace Unity.Tiny
{
    internal abstract class ComponentCallback : IComponentCallback
    {
        #region Properties
        public TinyType.Reference TypeRef { get; internal set; }
        #endregion

        #region API
        public void Run(ComponentCallbackType callbackType, TinyEntity entity, TinyObject component)
        {
            if (!ValidateParams(entity, component))
            {
                return;
            }

            switch (callbackType)
            {
                case ComponentCallbackType.OnAddComponent:
                    OnAddComponent(entity, component);
                    break;
                case ComponentCallbackType.OnRemoveComponent:
                    OnRemoveComponent(entity, component);
                    break;
                case ComponentCallbackType.OnValidate:
                    OnValidateComponent(entity, component);
                    break;
            }
        }

        protected TComponent GetComponent<TComponent>(TinyEntity entity)
            where TComponent : Component
        {
            return !ValidateParams(entity) ? null : entity.View.GetComponent<TComponent>();
        }

        protected virtual void OnAddComponent   (TinyEntity entity, TinyObject component) { }
        protected virtual void OnRemoveComponent(TinyEntity entity, TinyObject component) { }
        protected virtual void OnValidateComponent(TinyEntity entity, TinyObject component) { }
        #endregion

        #region Imeplementation
        private static bool ValidateParams(TinyEntity entity, TinyObject component)
        {
            return ValidateParams(entity) &&
                ValidateParams(component);
        }

        private static bool ValidateParams(TinyEntity entity)
        {
            return null != entity      &&
                null != entity.View &&
                entity.View;
        }

        private static bool ValidateParams(TinyObject component)
        {
            return null != component;
        }
        #endregion

    }
}

