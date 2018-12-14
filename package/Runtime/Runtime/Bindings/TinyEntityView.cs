
using System;

using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// TinyEntityView acts as a proxy to the TinyEntity to facilitate scene edit behaviour
    /// </summary>
    [DisallowMultipleComponent, ExecuteInEditMode]
    internal sealed class TinyEntityView : MonoBehaviour
    {
        #region Properties

        /// <summary>
        /// The underlying entity for this behaviour
        /// </summary>
        public TinyEntity.Reference EntityRef { get; set; }
        public IRegistry Registry { get; set; }
        public TinyContext Context { get; set; }
        public bool ForceRelink { get; set; }
        public bool Disposed { get; set; }

        private static Action<TinyTrackerRegistration, TinyEntityView> Dispatch =>
            TinyEventDispatcher<TinyTrackerRegistration>.Dispatch;
        #endregion

        #region API
        public void RefreshName()
        {
            var entity = EntityRef.Dereference(Registry);
            gameObject.name = entity?.Name ?? gameObject.name;
        }
        #endregion

        #region Unity Event Handlers
        private void Awake()
        {
            Dispatch(TinyTrackerRegistration.Register, this);
        }

        private void OnDestroy()
        {
            Dispatch(TinyTrackerRegistration.Unregister, this);
        }

        private void Start()
        {
            DestroyIfUnlinked();
        }

        private void LateUpdate()
        {
            DestroyIfUnlinked();
        }

        public bool DestroyIfUnlinked()
        {
            if (null == Registry || null == EntityRef.Dereference(Registry))
            {
                DestroyImmediate(gameObject, false);
                return true;
            }

            // We got duplicated from Unity, kill self.
            return false;
        }
        #endregion
    }
}
