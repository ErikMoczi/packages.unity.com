using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [Serializable]
    public class UnmaskedView
    {
        public class MaskData : ICloneable
        {
            internal Dictionary<GUIViewProxy, List<Rect>> m_MaskData;

            public MaskData() : this(null) {}

            public int Count { get { return m_MaskData.Count; } }

            internal MaskData(Dictionary<GUIViewProxy, List<Rect>> maskData)
            {
                m_MaskData = maskData ?? new Dictionary<GUIViewProxy, List<Rect>>();
            }

            public void AddParent(EditorWindow window)
            {
                m_MaskData[window.GetParent()] = null;
            }

            public void RemoveParent(EditorWindow window)
            {
                m_MaskData.Remove(window.GetParent());
            }

            public void AddTooltipViews()
            {
                var allViews = new List<GUIViewProxy>();
                GUIViewDebuggerHelperProxy.GetViews(allViews);

                foreach (var tooltipView in allViews.Where(v => v.IsGUIViewAssignableTo(GUIViewProxy.tooltipViewType)))
                    m_MaskData[tooltipView] = null;
            }

            public void RemoveTooltipViews()
            {
                foreach (var view in m_MaskData.Keys.ToArray())
                {
                    if (view.IsGUIViewAssignableTo(GUIViewProxy.tooltipViewType))
                        m_MaskData.Remove(view);
                }
            }

            public object Clone()
            {
                return new MaskData(m_MaskData.ToDictionary(kv => kv.Key, kv => kv.Value));
            }
        }

        public static MaskData GetViewsAndRects(IEnumerable<UnmaskedView> unmaskedViews)
        {
            var allViews = new List<GUIViewProxy>();
            GUIViewDebuggerHelperProxy.GetViews(allViews);

            // initialize result
            var result = new Dictionary<GUIViewProxy, List<Rect>>();
            var unmaskedControls = new Dictionary<GUIViewProxy, List<GUIControlSelector>>();
            var viewsWithWindows = new Dictionary<GUIViewProxy, HashSet<EditorWindow>>();
            foreach (var unmaskedView in unmaskedViews)
            {
                foreach (var view in GetMatchingViews(unmaskedView, allViews, viewsWithWindows))
                {
                    List<Rect> rects;
                    if (!result.TryGetValue(view, out rects))
                        result[view] = new List<Rect>(8);

                    List<GUIControlSelector> controls;
                    if (!unmaskedControls.TryGetValue(view, out controls))
                        unmaskedControls[view] = controls = new List<GUIControlSelector>();

                    controls.AddRange(unmaskedView.m_UnmaskedControls);
                }
            }

            // validate input
            foreach (var viewWithWindow in viewsWithWindows)
            {
                if (viewWithWindow.Value.Count > 1)
                {
                    throw new ArgumentException(
                        string.Format(
                            "Tried to get controls from multiple EditorWindows docked in the same location: {0}",
                            string.Join(", ", viewWithWindow.Value.Select(w => w.GetType().Name).ToArray())
                            ),
                        "unmaskedViews"
                        );
                }
            }

            // populate result
            var drawInstructions = new List<IMGUIDrawInstructionProxy>(32);
            var namedControlInstructions = new List<IMGUINamedControlInstructionProxy>(32);
            var propertyInstructions = new List<IMGUIPropertyInstructionProxy>(32);
            foreach (var viewRects in result)
            {
                // prevents null exception when repainting in case e.g., user has accidentally maximized view
                if (!viewRects.Key.isWindowAndRootViewValid)
                    continue;

                var unmaskedControlSelectors = unmaskedControls[viewRects.Key];
                if (unmaskedControlSelectors.Count == 0)
                    continue;

                // if the view refers to an InspectorWindow, flush the optimized GUI blocks so that Editor control rects will be updated
                HashSet<EditorWindow> windows;
                if (viewsWithWindows.TryGetValue(viewRects.Key, out windows) && windows.Count > 0)
                    InspectorWindowProxy.DirtyAllEditors(windows.First());

                // TODO: use actual selectors when API is in place
                GUIViewDebuggerHelperProxy.DebugWindow(viewRects.Key);

                viewRects.Key.RepaintImmediately();

                GUIViewDebuggerHelperProxy.GetDrawInstructions(drawInstructions);
                GUIViewDebuggerHelperProxy.GetNamedControlInstructions(namedControlInstructions);
                GUIViewDebuggerHelperProxy.GetPropertyInstructions(propertyInstructions);

                foreach (var controlSelector in unmaskedControls[viewRects.Key])
                {
                    switch (controlSelector.selectorMode)
                    {
                        case GUIControlSelector.Mode.GUIContent:
                            var selectorContent = controlSelector.guiContent;
                            foreach (var instruction in drawInstructions)
                            {
                                if (AreEquivalent(instruction.usedGUIContent, selectorContent))
                                    viewRects.Value.Add(instruction.rect);
                            }
                            break;
                        case GUIControlSelector.Mode.NamedControl:
                            foreach (var instruction in namedControlInstructions)
                            {
                                if (instruction.name == controlSelector.controlName)
                                    viewRects.Value.Add(instruction.rect);
                            }
                            break;
                        case GUIControlSelector.Mode.Property:
                            if (controlSelector.targetType == null)
                                continue;
                            var targetTypeName = controlSelector.targetType.AssemblyQualifiedName;
                            foreach (var instruction in propertyInstructions)
                            {
                                if (
                                    instruction.targetTypeName == targetTypeName &&
                                    instruction.path == controlSelector.propertyPath
                                    )
                                    viewRects.Value.Add(instruction.rect);
                            }
                            break;
                        default:
                            Debug.LogErrorFormat(
                            "No method currently implemented for selecting using specified mode: {0}",
                            controlSelector.selectorMode
                            );
                            break;
                    }
                }

                GUIViewDebuggerHelperProxy.StopDebugging();
            }

            return new MaskData(result);
        }

        private static bool AreEquivalent(GUIContent gc1, GUIContent gc2)
        {
            return
                gc1.image == gc2.image &&
                (string.IsNullOrEmpty(gc1.text) ? string.IsNullOrEmpty(gc2.text) : gc1.text == gc2.text) &&
                (string.IsNullOrEmpty(gc1.tooltip) ? string.IsNullOrEmpty(gc2.tooltip) : gc1.tooltip == gc2.tooltip);
        }

        private static IEnumerable<GUIViewProxy> GetMatchingViews(
            UnmaskedView unmaskedView,
            List<GUIViewProxy> allViews,
            Dictionary<GUIViewProxy, HashSet<EditorWindow>> viewsWithWindows
            )
        {
            var matchingViews = new HashSet<GUIViewProxy>(new GUIViewProxyComparer());


            switch (unmaskedView.m_SelectorType)
            {
                case SelectorType.EditorWindow:
                    var targetEditorWindowType = unmaskedView.m_EditorWindowType.type;
                    if (targetEditorWindowType == null)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "Specified unmasked view does not refer to a known EditorWindow type:\n{0}",
                                JsonUtility.ToJson(unmaskedView, true)
                                ), "unmaskedView"
                            );
                    }
                    if (targetEditorWindowType != null)
                    {
                        // make sure desired window is in current layout
                        // TODO: allow trainer to specify desired dock area if window doesn't yet exist?
//                        var window = EditorWindow.GetWindow(targetEditorWindowType);
                        var window = Resources.FindObjectsOfTypeAll(targetEditorWindowType).Cast<EditorWindow>().ToArray().FirstOrDefault();
                        if (window == null || window.GetParent() == null)
                            return matchingViews;
                        window.Show();
                        if (!allViews.Contains(window.GetParent()))
                            allViews.Add(window.GetParent());
                        foreach (var view in allViews)
                        {
                            if (!view.IsActualViewAssignableTo(targetEditorWindowType))
                                continue;

                            HashSet<EditorWindow> windows;
                            if (!viewsWithWindows.TryGetValue(view, out windows))
                                viewsWithWindows[view] = windows = new HashSet<EditorWindow>();
                            windows.Add(window);

                            matchingViews.Add(view);
                        }
                    }
                    break;
                case SelectorType.GUIView:
                    var targetViewType = unmaskedView.m_ViewType.type;
                    if (targetViewType == null)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "Specified unmasked view does not refer to a known GUIView type:\n{0}",
                                JsonUtility.ToJson(unmaskedView, true)
                                ), "unmaskedView"
                            );
                    }
                    if (targetViewType != null)
                    {
                        foreach (var view in allViews)
                        {
                            if (view.IsGUIViewAssignableTo(targetViewType))
                                matchingViews.Add(view);
                        }
                    }
                    break;
            }

            if (matchingViews.Count == 0)
            {
                throw new ArgumentException(
                    string.Format(
                        "Specified unmasked view refers to a view that could not be found:\n{0}",
                        JsonUtility.ToJson(unmaskedView, true)
                        ), "unmaskedView"
                    );
            }

            return matchingViews;
        }

        public enum SelectorType
        {
            GUIView,
            EditorWindow,
        }

        [SerializeField]
        private SelectorType m_SelectorType;

        [SerializedTypeGUIViewFilter]
        [SerializeField]
        private SerializedType m_ViewType = new SerializedType(null);

        [SerializedTypeFilter(typeof(EditorWindow))]
        [SerializeField]
        private SerializedType m_EditorWindowType = new SerializedType(null);

        [SerializeField]
        private List<GUIControlSelector> m_UnmaskedControls = new List<GUIControlSelector>();

        public int GetUnmaskedControls(List<GUIControlSelector> unmaskedControls)
        {
            unmaskedControls.Clear();
            unmaskedControls.AddRange(m_UnmaskedControls);
            return unmaskedControls.Count;
        }

        protected UnmaskedView() {}

        internal static UnmaskedView CreateInstanceForGUIView<T>(IList<GUIControlSelector> unmaskedControls = null)
        {
            if (!GUIViewProxy.IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException("Type must be assignable to GUIView");

            UnmaskedView result = new UnmaskedView();
            result.m_SelectorType = SelectorType.GUIView;
            result.m_ViewType.type = typeof(T);
            if (unmaskedControls != null)
                result.m_UnmaskedControls.AddRange(unmaskedControls);
            return result;
        }

        public static UnmaskedView CreateInstanceForEditorWindow<T>(IList<GUIControlSelector> unmaskedControls = null) where T : EditorWindow
        {
            UnmaskedView result = new UnmaskedView();
            result.m_SelectorType = SelectorType.EditorWindow;
            result.m_EditorWindowType.type = typeof(T);
            if (unmaskedControls != null)
                result.m_UnmaskedControls.AddRange(unmaskedControls);
            return result;
        }
    }
}
