using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.InteractiveTutorials
{
    public static class MaskingManager
    {
        internal static bool IsMasked(GUIViewProxy view, List<Rect> rects)
        {
            rects.Clear();
            List<Rect> rectList;

            var unmaskedViewsKeys = s_UnmaskedViews.Keys.ToList();
            Debug.Log(unmaskedViewsKeys.Count);

            if (s_UnmaskedViews.TryGetValue(view, out rectList))
            {
                rects.AddRange(rectList);
                return false;
            }
            return true;
        }

        internal static bool IsHighlighted(GUIViewProxy view, List<Rect> rects)
        {
            rects.Clear();
            List<Rect> rectList;
            if (!s_HighlightedViews.TryGetValue(view, out rectList))
                return false;
            rects.AddRange(rectList);
            return true;
        }

        static GUIViewProxyComparer s_GUIViewProxyComparer = new GUIViewProxyComparer();

        private static readonly Dictionary<GUIViewProxy, List<Rect>> s_UnmaskedViews = new Dictionary<GUIViewProxy, List<Rect>>(s_GUIViewProxyComparer);
        private static readonly Dictionary<GUIViewProxy, List<Rect>> s_HighlightedViews = new Dictionary<GUIViewProxy, List<Rect>>(s_GUIViewProxyComparer);

        private static readonly List<VisualElement> s_Masks = new List<VisualElement>();
        private static readonly List<VisualElement> s_Highlighters = new List<VisualElement>();

        private static double s_LastHighlightTime;

        public static float highlightAnimationDelay { get; set; }
        public static float highlightAnimationSpeed { get; set; }

        static MaskingManager()
        {
            EditorApplication.update += delegate
                {
                    // do not animate unless enough time has passed since masking was last applied
                    var t = EditorApplication.timeSinceStartup - s_LastHighlightTime - highlightAnimationDelay;
                    if (t < 0d)
                        return;

                    var alpha = Mathf.Cos((float)t * highlightAnimationSpeed) + 0.5f;
                    foreach (var highlighter in s_Highlighters)
                    {
                        if (highlighter == null)
                            continue;

                        var color = highlighter.style.borderColor.value;
                        color.a = alpha;
                        highlighter.style.borderColor = color;
                    }
                    foreach (var view in s_HighlightedViews)
                    {
                        if (view.Key.isValid)
                            view.Key.Repaint();
                    }
                };
        }

        public static void Unmask()
        {
            foreach (var mask in s_Masks)
            {
                if (mask != null && UIElementsHelper.GetParent(mask) != null)
                    UIElementsHelper.Remove(UIElementsHelper.GetParent(mask), mask);
            }
            s_Masks.Clear();
            foreach (var highlighter in s_Highlighters)
            {
                if (highlighter != null && UIElementsHelper.GetParent(highlighter) != null)
                    UIElementsHelper.Remove(UIElementsHelper.GetParent(highlighter), highlighter);
            }
            s_Highlighters.Clear();
        }

        private static void CopyMaskData(UnmaskedView.MaskData maskData, Dictionary<GUIViewProxy, List<Rect>> viewsAndResources)
        {
            viewsAndResources.Clear();
            foreach (var unmaskedView in maskData.m_MaskData)
            {
                if (unmaskedView.Key == null)
                    continue;
                var unmaskedRegions = unmaskedView.Value == null ? new List<Rect>(1) : unmaskedView.Value.ToList();
                if (unmaskedRegions.Count == 0)
                    unmaskedRegions.Add(new Rect(0f, 0f, unmaskedView.Key.position.width, unmaskedView.Key.position.height));
                viewsAndResources[unmaskedView.Key] = unmaskedRegions;
            }
        }

        public static void Mask(
            UnmaskedView.MaskData unmaskedViewsAndRegionsMaskData, Color maskColor,
            UnmaskedView.MaskData highlightedRegionsMaskData, Color highlightColor, float highlightThickness
            )
        {
            Unmask();

            CopyMaskData(unmaskedViewsAndRegionsMaskData, s_UnmaskedViews);
            CopyMaskData(highlightedRegionsMaskData, s_HighlightedViews);

            List<GUIViewProxy> views = new List<GUIViewProxy>();
            GUIViewDebuggerHelperProxy.GetViews(views);

            foreach (var view in views)
            {
                if (!view.isValid)
                    continue;

                List<Rect> rects;

                var viewRect =  new Rect(0, 0, view.position.width, view.position.height);

                // mask everything except the unmasked view rects
                if (s_UnmaskedViews.TryGetValue(view, out rects))
                {
                    var maskedRects = GetNegativeSpaceRects(viewRect, rects);
                    foreach (var rect in maskedRects)
                    {
                        var mask = new VisualElement();
                        mask.style.backgroundColor = maskColor;
                        mask.layout = rect;
                        UIElementsHelper.Add(UIElementsHelper.GetVisualTree(view), mask);
                        s_Masks.Add(mask);
                    }
                }
                // mask the whole view
                else
                {
                    var mask = new VisualElement();
                    mask.style.backgroundColor = maskColor;
                    mask.layout = viewRect;
                    UIElementsHelper.Add(UIElementsHelper.GetVisualTree(view), mask);
                    s_Masks.Add(mask);
                }

                if (s_HighlightedViews.TryGetValue(view, out rects))
                {
                    // unclip highlight to apply as "outer stroke" if it is being applied to some control(s) in the view
                    var unclip = rects.Count > 1 || rects[0] != viewRect;
                    foreach (var rect in rects)
                    {
                        var highlighter = new VisualElement();
                        highlighter.style.borderColor = highlightColor;
                        highlighter.style.borderLeftWidth = highlightThickness;
                        highlighter.style.borderRightWidth = highlightThickness;
                        highlighter.style.borderTopWidth = highlightThickness;
                        highlighter.style.borderBottomWidth = highlightThickness;
                        highlighter.pickingMode = PickingMode.Ignore;
                        var layout = rect;
                        if (unclip)
                        {
                            layout.xMin -= highlightThickness;
                            layout.xMax += highlightThickness;
                            layout.yMin -= highlightThickness;
                            layout.yMax += highlightThickness;
                        }
                        highlighter.layout = layout;
                        UIElementsHelper.Add(UIElementsHelper.GetVisualTree(view), highlighter);
                        s_Highlighters.Add(highlighter);
                    }
                }
            }

            s_LastHighlightTime = EditorApplication.timeSinceStartup;
        }

        static readonly HashSet<float> s_YCoords = new HashSet<float>();
        static readonly HashSet<float> s_XCoords = new HashSet<float>();

        static readonly List<float> s_SortedYCoords = new List<float>();
        static readonly List<float> s_SortedXCoords = new List<float>();

        internal static List<Rect> GetNegativeSpaceRects(Rect viewRect, List<Rect> positiveSpaceRects)
        {
            //TODO maybe its okay to round to int?

            s_YCoords.Clear();
            s_XCoords.Clear();

            for (int i = 0; i < positiveSpaceRects.Count; i++)
            {
                var hole = positiveSpaceRects[i];
                s_YCoords.Add(hole.y);
                s_YCoords.Add(hole.yMax);
                s_XCoords.Add(hole.x);
                s_XCoords.Add(hole.xMax);
            }

            s_YCoords.Add(0);
            s_YCoords.Add(viewRect.height);

            s_XCoords.Add(0);
            s_XCoords.Add(viewRect.width);

            s_SortedYCoords.Clear();
            s_SortedXCoords.Clear();

            s_SortedYCoords.AddRange(s_YCoords);
            s_SortedXCoords.AddRange(s_XCoords);

            s_SortedYCoords.Sort();
            s_SortedXCoords.Sort();

            var filledRects = new List<Rect>();

            for (var i = 1; i < s_SortedYCoords.Count; ++i)
            {
                var minY = s_SortedYCoords[i - 1];
                var maxY = s_SortedYCoords[i];
                var midY = (maxY + minY) / 2;
                var workingRect = new Rect(s_SortedXCoords[0], minY, 0, (maxY - minY));

                for (var j = 1; j < s_SortedXCoords.Count; ++j)
                {
                    var minX = s_SortedXCoords[j - 1];
                    var maxX = s_SortedXCoords[j];

                    var midX = (maxX + minX) / 2;


                    var potentialHole = positiveSpaceRects.Find((hole) => { return hole.Contains(new Vector2(midX, midY)); });
                    var cellIsHole = potentialHole.width > 0 && potentialHole.height > 0;

                    if (cellIsHole)
                    {
                        if (workingRect.width > 0 && workingRect.height > 0)
                            filledRects.Add(workingRect);

                        workingRect.x = maxX;
                        workingRect.xMax = maxX;
                    }
                    else
                    {
                        workingRect.xMax = maxX;
                    }
                }

                if (workingRect.width > 0 && workingRect.height > 0)
                    filledRects.Add(workingRect);
            }

            return filledRects;
        }
    }
}
