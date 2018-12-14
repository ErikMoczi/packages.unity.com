using UnityEngine;
using Unity.Experimental.EditorMode;
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Unity.Tiny
{
    internal interface IProxy<TWindow>
    {
        TWindow Window { get; }
        VisualElement Root { get; }
        VisualElement DefaultRoot { get; }
        bool InvokeOnGUIEnabled { get; }
        void SetInvokeOnGUIEnabled(bool enabled);
        void DoSwitchToDefault();
        void DoSwitchToOverride();
    }

    internal sealed class EditorWindowOverrideProxy<TOverride, TWindow> : EditorWindowOverride<TWindow>, IProxy<TWindow>
        where TOverride : TinyEditorWindowOverride<TWindow>, new()
        where TWindow : EditorWindow
    {
        private TOverride m_Proxied = new TOverride();

        public override void OnEnable()
        {
            m_Proxied.m_Proxy = this;
            m_Proxied.OnEnable();
        }

        public void SetInvokeOnGUIEnabled(bool enabled)
        {
            InvokeOnGUIEnabled = enabled;
        }

        public override void OnDisable() => m_Proxied.OnDisable();
        public override void OnBecameVisible() => m_Proxied.OnBecameVisible();
        public override void OnBecameInvisible() => m_Proxied.OnBecameInvisible();
        public override void Update() => m_Proxied.Update();
        public override void OnFocus() => m_Proxied.OnFocus();
        public override void OnLostFocus() => m_Proxied.OnLostFocus();
        public override void OnSelectionChanged() => m_Proxied.OnSelectionChanged();
        public override void OnProjectChange() => m_Proxied.OnProjectChange();
        public override void OnDidOpenScene() => m_Proxied.OnDidOpenScene();
        public override void OnInspectorUpdate() => m_Proxied.OnInspectorUpdate();
        public override void OnHierarchyChange() => m_Proxied.OnHierarchyChange();
        public override void OnResize() => m_Proxied.OnResize();
        public override void ModifierKeysChanged() => m_Proxied.ModifierKeysChanged();
        public override void OnSwitchedToOverride() => m_Proxied.OnSwitchedToOverride();
        public override void OnSwitchedToDefault() => m_Proxied.OnSwitchedToDefault();
        public override bool OnAddItemsToMenu(GenericMenu menu) => m_Proxied.OnAddItemsToMenu(menu);

        public void DoSwitchToDefault()
        {
            SwitchToDefault();
        }

        public void DoSwitchToOverride()
        {
            SwitchToOverride();
        }
    }

    internal interface ITinyEditorWindowOverride
    {

    }

    internal abstract class TinyEditorWindowOverride<TWindow> : ITinyEditorWindowOverride
        where TWindow : EditorWindow
    {
        internal IProxy<TWindow> m_Proxy;

        public TWindow Window => m_Proxy.Window;

        public VisualElement Root => m_Proxy.Root;

        public VisualElement DefaultRoot => m_Proxy.DefaultRoot;

        public bool InvokeOnGUIEnabled
        {
            get { return m_Proxy.InvokeOnGUIEnabled; }
            protected set { m_Proxy.SetInvokeOnGUIEnabled(value); }
        }

        public virtual void OnEnable() { }

        public virtual void OnDisable() { }

        public virtual void OnBecameVisible() { }

        public virtual void OnBecameInvisible() { }

        public virtual void Update() { }

        public virtual void OnFocus() { }

        public virtual void OnLostFocus() { }

        public virtual void OnSelectionChanged() { }

        public virtual void OnProjectChange() { }

        public virtual void OnDidOpenScene() { }

        public virtual void OnInspectorUpdate() { }

        public virtual void OnHierarchyChange() { }

        public virtual void OnResize() { }

        public virtual void ModifierKeysChanged() { }

        public virtual void OnSwitchedToOverride() { }

        public virtual void OnSwitchedToDefault() { }

        public virtual bool OnAddItemsToMenu(GenericMenu menu)
        {
            return true;
        }

        protected void SwitchToDefault()
        {
            m_Proxy.DoSwitchToDefault();
        }

        protected void SwitchToOverride()
        {
            m_Proxy.DoSwitchToOverride();
        }
    }
}
