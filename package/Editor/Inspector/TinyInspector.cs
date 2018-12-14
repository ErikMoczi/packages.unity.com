using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Unity.Properties;
using UnityEngine.Events;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    [AutoRepaintOnTypeChange(typeof(TinyEntity))]
    [AutoRepaintOnTypeChange(typeof(TinyType))]
    [AutoRepaintOnTypeChange(typeof(TinyModule))]
    internal class TinyInspector : TinyEditorWindowOverride<EditorWindow>
    {
        #region Static
        private static readonly List<TinyInspector> s_ActiveWindows = new List<TinyInspector>();
        private static TinyContext Context;

        public static bool IsBeingInspected(IPropertyContainer container)
        {
            foreach (var window in s_ActiveWindows)
            {
                if (window.Backend.Targets.Contains(container))
                {
                    return true;
                }
            }
            return false;
        }

        public static void ForceRefreshSelection()
        {
            foreach (var window in s_ActiveWindows)
            {
                window.OnSelectionChanged();
            }
        }

        public static void RepaintAll()
        {
            foreach (var window in s_ActiveWindows)
            {
                window.Repaint();
            }
        }

        [TinyInitializeOnLoad]
        private static void Register()
        {
            TinyEditorApplication.OnLoadProject += CreateInitialBackend;
        }

        private static void CreateInitialBackend(TinyProject project, TinyContext context)
        {
            Context = context;
            foreach (var window in s_ActiveWindows)
            {
                window.m_Backend = null;
            }
        }
        #endregion

        /// <summary>
        /// These are the currently inspected targets. They might be of different types.
        /// </summary>
        private List<IPropertyContainer> m_Targets = new List<IPropertyContainer>();

        [SerializeField]
        private InspectorMode m_Mode = InspectorMode.Normal;

        private bool m_IncludeComponentFamilies = true;

        [SerializeField]
        private InspectorBackendType m_BackendType = InspectorBackendType.IMGUI;

        [SerializeField]
        private IInspectorBackend m_Backend;

        private UnityEvent<bool> m_LockedStateChanged;
        private ActiveEditorTracker m_Tracker;

        public IInspectorBackend Backend
        {
            get
            {
                if (null == m_Backend)
                {
                    SwitchToBackend(m_BackendType, true);
                }
                return m_Backend;
            }
        }

        private ActiveEditorTracker Tracker
        {
            get
            {
                if (null == m_Tracker)
                {
                    m_Tracker = GetTracker();
                }

                return m_Tracker;
            }
        }

        public VisualElement GetRoot()
        {
            return Root;
        }

        private bool InOverrideMode { get; set; } = false;

        #region Unity
        public override void OnEnable()
        {
            s_ActiveWindows.Add(this);

            SwitchToBackend(m_BackendType, true);
            m_Tracker = GetTracker();
            m_LockedStateChanged = GetLockedStateChanged();
            m_LockedStateChanged.AddListener(LockStateChanged);

            Repaint();

            Context.GetManager<IUndoManager>().OnUndoPerformed += OnUndoRedoPerformed;
            Context.GetManager<IUndoManager>().OnRedoPerformed += OnUndoRedoPerformed;
        }

        public override void OnDisable()
        {
            m_Targets.Clear();

            m_LockedStateChanged.RemoveListener(LockStateChanged);
            s_ActiveWindows.Remove(this);

            Context.GetManager<IUndoManager>().OnUndoPerformed -= OnUndoRedoPerformed;
            Context.GetManager<IUndoManager>().OnRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed(HashSet<Change> changes)
        {
            OnSelectionChanged();
        }

        public override void OnBecameVisible()
        {
            OnSelectionChanged();
        }

        private void OnGUI()
        {
            if (null == Backend)
            {
                EditorGUILayout.LabelField($"Backend not supported for backend type '{m_BackendType.ToString()}'");
                return;
            }

            try
            {
                m_Mode = GetInspectorMode();
                Backend.Targets = m_Targets;
                Backend.Mode = m_Mode;
                Backend.ShowFamilies = m_IncludeComponentFamilies;

                Backend.OnGUI();

                m_Targets = Backend.Targets;
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                TinyEditorAnalytics.SendExceptionOnce("Inspector.OnGUI", e);
                throw;
            }
        }

        private static bool Valid(Object obj)
        {
            return obj is GameObject || obj is IPropertyContainer;
        }

        private void HandleSelectionChanged(params Object[] targets)
        {
            try
            {
                if (null == targets)
                {
                    return;
                }

                var validObjects = targets.Where(Valid).ToList();
                m_Targets.Clear();

                m_Targets.AddRange(validObjects
                        .OfType<GameObject>()
                        .Select(go => go.GetComponent<TinyEntityView>())
                        .Where(view => null != view && view && null != view.Registry)
                        .Select(view => view.EntityRef.Dereference(view.Registry))
                    );

                m_Targets.AddRange(validObjects.OfType<IPropertyContainer>());
            }
            finally
            {
                if (!Backend.Locked)
                {
                    Repaint();
                }
            }
        }
        #endregion

        private IInspectorBackend GetBackend(InspectorBackendType type)
        {
            var root = GetRoot();
            root.Clear();

            IInspectorBackend backend = null;
            switch (type)
            {
                case InspectorBackendType.IMGUI:
                    {
                        backend = new IMGUIBackend(this, Context);
                        var imgui = new IMGUIContainer(OnGUI);
                        Root.Add(imgui);
                        imgui.StretchToParentSize();
                    }
                    break;
                case InspectorBackendType.UIElements:
                    backend = new UIElementsBackend(this);
                    break;
                default:
                    throw new ArgumentException("Unknown InspectorBackendType", nameof(type));
            }
            backend.Mode = m_Mode;
            backend.Build();
            return backend;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {

#if UNITY_TINY_INTERNAL
            menu.AddItem(new GUIContent("Backend/IMGUI"),
                m_BackendType == InspectorBackendType.IMGUI,
                () => SwitchToBackend(InspectorBackendType.IMGUI));

            // Not really supported yet.
            menu.AddItem(new GUIContent("Backend/UI Elements"),
                m_BackendType == InspectorBackendType.UIElements,
                () => SwitchToBackend(InspectorBackendType.UIElements));
#endif
            menu.AddItem(new GUIContent("Show component families"),
                m_IncludeComponentFamilies,
                () => m_IncludeComponentFamilies = !m_IncludeComponentFamilies);
            menu.AddSeparator("");

        }

        public void SwitchToBackend(InspectorBackendType type, bool force = false)
        {
            if (type == m_BackendType && !force)
            {
                return;
            }
            m_BackendType = type;
            m_Backend = GetBackend(m_BackendType);
        }

        public override bool OnAddItemsToMenu(GenericMenu menu)
        {
            AddItemsToMenu(menu);
            return true;
        }

        private void Repaint()
        {
            Window.Repaint();
        }

        private void LockStateChanged(bool lockeState)
        {
            OnSelectionChanged();
        }

        private ActiveEditorTracker GetTracker()
        {
            return (ActiveEditorTracker)Window.GetType().GetMethod("get_tracker", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Window, null);
        }

        private InspectorMode GetInspectorMode()
        {
            var fieldInfo = Window.GetType().GetField("m_InspectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
            return (InspectorMode)fieldInfo.GetValue(Window);
        }

        private UnityEvent<bool> GetLockedStateChanged()
        {
            var trackerFieldInfo = Window.GetType().GetField("m_LockTracker", BindingFlags.NonPublic | BindingFlags.Instance);
            var tracker = trackerFieldInfo.GetValue(Window);
            var lockedStateFieldInfo = tracker.GetType().GetField("lockStateChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            return (UnityEvent<bool>)lockedStateFieldInfo.GetValue(tracker);
        }

        public override void OnSelectionChanged()
        {
            HandleSelectionChanged(Tracker?.activeEditors.SelectMany(e => e.targets).ToArray());
            var containsGameObject = Tracker?.activeEditors.SelectMany(e => e.targets).Any(o => o is GameObject) ?? false;
            if (InOverrideMode)
            {
                InvokeOnGUIEnabled = !m_Targets.Any() && !containsGameObject;
                Root.visible = !InvokeOnGUIEnabled;
                DefaultRoot.visible = InvokeOnGUIEnabled;
            }
        }

        public override void OnSwitchedToDefault()
        {
            InOverrideMode = false;
        }

        public override void OnSwitchedToOverride()
        {
            InOverrideMode = true;
            OnSelectionChanged();
        }
    }
}

