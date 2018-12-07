using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    internal enum MaskType
    {
        FullyUnmasked = 0,
        BlockInteractions
    }

    internal enum MaskSizeModifier
    {
        NoModifications = 0,
        ExpandWidthToWholeWindow
    }

    internal struct MaskViewData
    {
        internal MaskType maskType;
        internal List<Rect> rects;
        internal MaskSizeModifier maskSizeModifier;

        internal static MaskViewData CreateEmpty(MaskType type)
        {
            return new MaskViewData()
            {
                maskType = type,
                rects = null,
            };
        }
    }

    [Serializable]
    public class UnmaskedView
    {
        
        public class MaskData : ICloneable
        {
            internal Dictionary<GUIViewProxy, MaskViewData> m_MaskData;

            public MaskData() : this(null) {}

            public int Count { get { return m_MaskData.Count; } }

            internal MaskData(Dictionary<GUIViewProxy, MaskViewData> maskData)
            {
                m_MaskData = maskData ?? new Dictionary<GUIViewProxy, MaskViewData>();
            }

            public void AddParentFullyUnmasked(EditorWindow window)
            {
                m_MaskData[window.GetParent()] = MaskViewData.CreateEmpty(MaskType.FullyUnmasked);
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
                    m_MaskData[tooltipView] = MaskViewData.CreateEmpty(MaskType.FullyUnmasked);
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
            var result = new Dictionary<GUIViewProxy, MaskViewData>();
            var unmaskedControls = new Dictionary<GUIViewProxy, List<GUIControlSelector>>();
            var viewsWithWindows = new Dictionary<GUIViewProxy, HashSet<EditorWindow>>();
            foreach (var unmaskedView in unmaskedViews)
            {
                foreach (var view in GetMatchingViews(unmaskedView, allViews, viewsWithWindows))
                {
                    MaskViewData maskViewData;
                    if (!result.TryGetValue(view, out maskViewData))
                    {
                        result[view] = new MaskViewData() { rects = new List<Rect>(8), maskType = unmaskedView.m_MaskType, maskSizeModifier = unmaskedView.m_MaskSizeModifier};
                    }

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
                    Rect regionRect = Rect.zero;
                    bool regionFound = false;
                    switch (controlSelector.selectorMode)
                    {
                        case GUIControlSelector.Mode.GUIContent:
                            var selectorContent = controlSelector.guiContent;
                            foreach (var instruction in drawInstructions)
                            {
                                if (AreEquivalent(instruction.usedGUIContent, selectorContent))
                                {
                                    regionFound = true;
                                    regionRect = instruction.rect;
                                }
                            }
                            break;
                        case GUIControlSelector.Mode.NamedControl:
                            foreach (var instruction in namedControlInstructions)
                            {
                                if (instruction.name == controlSelector.controlName)
                                {
                                    regionFound = true;
                                    regionRect = instruction.rect;
                                }
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
                                {
                                    regionFound = true;
                                    regionRect = instruction.rect;
                                }
                            }
                            break;
                        default:
                            Debug.LogErrorFormat(
                            "No method currently implemented for selecting using specified mode: {0}",
                            controlSelector.selectorMode
                            );
                            break;
                    }

                    if (regionFound)
                    {
                        if (viewRects.Value.maskSizeModifier == MaskSizeModifier.ExpandWidthToWholeWindow)
                        {
                            const int padding = 5;
                            regionRect.x = padding;
                            regionRect.width = viewRects.Key.position.width - padding *2;
                        }
                        viewRects.Value.rects.Add(regionRect);
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
        private MaskType m_MaskType = MaskType.FullyUnmasked; 

        [SerializeField]
        private MaskSizeModifier m_MaskSizeModifier = MaskSizeModifier.NoModifications;

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
