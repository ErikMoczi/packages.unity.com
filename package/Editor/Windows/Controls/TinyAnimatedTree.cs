

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Unity.Tiny
{
    internal class TinyAnimatedTree 
    {
        internal class Element
        {
            public struct Args
            {
                public string Name;
                public string Tooltip;
                public bool Included;
                public bool Warning;
                public bool Separator;
                public Action OnClick;
                public Color Background;
                public bool Searchable;
                public string SearchPostfix;
            }

            #region Fields
            private readonly List<Element> m_Children = new List<Element>();
            private readonly string m_Name;
            private readonly string m_Tooltip;
            private readonly Action m_OnClick;
            private readonly bool m_Included;
            private readonly bool m_IsWarning;
            private readonly bool m_IsSeparator;
            private readonly Color m_Color;
            private readonly bool m_Searchable;
            private readonly string m_SearchPostfix;
            #endregion

            #region Properties
            public List<Element> Children => m_Children;
            public string Name => m_Name;
            public string Tooltip => m_Tooltip;
            public bool IsWarning => m_IsWarning;
            public bool isSeparator => m_IsSeparator;
            public bool IsLeaf => m_Children.Count == 0;
            public bool Included => m_Included;
            public Color Color => m_Color;
            public bool Searchable => m_Searchable;
            public string SearchPostfix => m_SearchPostfix;

            internal Vector2 Scroll { get; set; }
            #endregion

            #region API
            public static Element MakeGroup(string name, string tooltip, bool included)
            {
                return new Element(new Args
                {
                    Name = name,
                    Tooltip = tooltip,
                    Included = included,
                    Searchable = false,
                });
            }

            public static Element MakeLeaf(string name, string tooltip, bool included, Action onClick)
            {
                return MakeLeaf(name, tooltip, included, onClick, TinyColors.AnimatedTree.Background);
            }

            public static Element MakeLeaf(Args args)
            {
                return new Element(args);
            }
            
            public static Element MakeLeaf(string name, string tooltip, bool included, Action onClick, Color backgroundColor)
            {
                return new Element(new Args
                {
                    Name = name,
                    Tooltip = tooltip,
                    Included = included,
                    OnClick = onClick,
                    Background = backgroundColor,
                    Searchable = true,
                });
            }

            public static Element MakeWarning(string warning)
            {
                return new Element(new Args
                {
                    Name = warning,
                    Included = true,
                    Warning = true,
                    Background = TinyColors.AnimatedTree.Warning,
                    Searchable = true,
                });
            }

            public static Element MakeSeparator()
            {
                return new Element(new Args
                {
                    Included = true,
                    Separator = true,
                    Searchable = false,
                });
            }

            public void Add(Element child)
            {
                if (m_Children.Contains(child))
                {
                    return;
                }
                m_Children.Add(child);
            }

            public void InvokeCallback()
            {
                m_OnClick?.Invoke();
            }

            public void SortChildren()
            {
                m_Children.Sort((lhs, rhs) =>
                {
                    if (lhs.IsWarning)
                    {
                        return -1;
                    }
                    else if (rhs.IsWarning)
                    {
                        return 1;
                    }
                    else
                    {
                        return string.Compare(lhs.Name, rhs.Name, StringComparison.InvariantCulture);
                    }
                });
            }
            #endregion

            #region Implementation

            private Element(Args args)
            {
                m_Name = args.Name;
                m_Tooltip = args.Tooltip;
                m_OnClick = args.OnClick;
                m_Included = args.Included;
                m_IsWarning = args.Warning;
                m_IsSeparator = args.Separator;
                m_Color = args.Background;
                m_Searchable = args.Searchable;
                m_SearchPostfix = args.SearchPostfix;
            }
            #endregion
        }

        #region Internal Elements
        private class GUIElement : IComparable<GUIElement>
        {
            public int level;
            public GUIContent content;
            public Element element;
            public bool Searchable => element.Searchable;
            public string SearchPostfix => element.SearchPostfix;

            public string name { get { return content.text; } }

            public int CompareTo(GUIElement other)
            {
                return name.CompareTo(other.name);
            }
        }

        private class WarningElement : GUIElement
        {
            public WarningElement(int level, string name)
            {
                this.level = level;
                content = new GUIContent(name);
            }
        }

        private class SeparatorElement : GUIElement
        {
            public SeparatorElement(int level)
            {
                this.level = level;
            }
        }

        private class LeafElement : GUIElement
        {
            public LeafElement(int level, string name, string tooltip)
            {
                this.level = level;
                content = new GUIContent(name, tooltip);
            }
        }

        private class GroupElement : GUIElement
        {
            public Vector2 scroll;
            public int selectedIndex = 0;

            public GroupElement(int level, string name, string tooltip)
            {
                this.level = level;
                content = new GUIContent(name, tooltip);
            }
        }
        #endregion

        #region Styles
        internal class Styles
        {
            public GUIStyle header = new GUIStyle("In BigTitle");
            public GUIStyle componentButton = new GUIStyle("PR Label");
            public GUIStyle warning = new GUIStyle("PR Label");
            public GUIStyle groupButton;
            public GUIStyle background = "grey_border";
            public GUIStyle previewHeader = new GUIStyle(EditorStyles.label);
            public GUIStyle previewText = new GUIStyle(EditorStyles.wordWrappedLabel);
            public GUIStyle rightArrow = "AC RightArrow";
            public GUIStyle leftArrow = "AC LeftArrow";

            public Styles()
            {
                header.font = EditorStyles.boldLabel.font;

                componentButton.alignment = TextAnchor.MiddleLeft;
                componentButton.padding.left -= 15;
                componentButton.fixedHeight = 0;

                warning.alignment = TextAnchor.MiddleCenter;
                warning.normal.textColor = Color.grey;
                warning.padding.left -= 15;
                warning.fixedHeight = 0;
                warning.wordWrap = true;

                groupButton = new GUIStyle(componentButton);
                groupButton.padding.left += 17;

                previewText.padding.left += 3;
                previewText.padding.right += 3;
                previewHeader.padding.left += 3 - 2;
                previewHeader.padding.right += 3;
                previewHeader.padding.top += 3;
                previewHeader.padding.bottom += 2;
            }
        }
        #endregion

        #region Events
        public delegate void OnEscapePressedEvent();
        public event OnEscapePressedEvent OnEscapePressed = delegate{ };

        public delegate void OnStateChangedEvent();
        public event OnStateChangedEvent OnStateChanged = delegate{ };

        public delegate void OnAnyLeafElementClickedEvent(Element element);
        public event OnAnyLeafElementClickedEvent OnAnyLeafElementClicked = delegate{ };
        #endregion

        #region Static
        private static Styles s_Styles;
        private SearchField s_ComponentSearchField;
        #endregion

        #region Constants
        private const string kSearchHeader = "Search";
        private const int kHeaderHeight = 30;
        #endregion

        #region Fields
        private string m_Search = "";
        private string m_DelayedSearch = "";

        private float m_Anim = 1;
        private int m_AnimTarget = 1;

        private int m_LastIndex = 0;
        private long m_LastTime = 0;

        private Vector2 m_Scroll;
        private bool m_ScrollToSelected = false;

        private GUIElement[] m_Tree;
        private GUIElement[] m_SearchResultTree;
        private List<GroupElement> m_Stack = new List<GroupElement>();

        private List<GUIElement> m_InternalElements = new List<GUIElement>();

        private readonly List<Element> m_Elements = new List<Element>();
        private bool m_Rebuild = true;
        #endregion

        #region Properties
        private Rect Position { get; set; }

        private bool IsAnimating { get { return m_Anim != m_AnimTarget; } }

        private GUIElement[] activeTree { get { return hasSearch ? m_SearchResultTree : m_Tree; } }

        private bool hasSearch { get { return !string.IsNullOrEmpty(m_Search); } }

        private GroupElement activeParent { get { return m_Stack[m_Stack.Count - 2 + m_AnimTarget]; } }

        private GUIElement activeElement
        {
            get
            {
                if (activeTree == null)
                    return null;

                List<GUIElement> children = GetChildren(activeTree, activeParent);
                if (children.Count == 0)
                    return null;

                return children[activeParent.selectedIndex];
            }
        }
        #endregion

        #region API
        public TinyAnimatedTree(string name)
        {
            s_ComponentSearchField = new SearchField();
            s_ComponentSearchField.SetFocus();
            Add(new GroupElement(0, name, ""));
        }

        public Element GetGroup(string name)
        {
            return m_Elements.Find(e => e.Name == name);
        }

        public void Add(Element element)
        {
            if (null == element)
            {
                return;
            }

            m_Elements.Add(element);
            m_Rebuild = true;
        }

        private void Add(Element element, int level = 1)
        {
            if (element.isSeparator)
            {
                m_InternalElements.Add(new SeparatorElement(level));
            }
            else if (element.IsWarning)
            {
                m_InternalElements.Add(new WarningElement(level, element.Name) { element = element});
            }
            else if (element.IsLeaf)
            {
                m_InternalElements.Add(new LeafElement(level, element.Name, element.Tooltip) { element = element});
            }
            else
            {
                var group = new GroupElement(level, element.Name, element.Tooltip) {element = element};
                m_InternalElements.Add(group);
                foreach(var subElement in element.Children)
                {
                    if (subElement.IsWarning)
                    {
                        group.selectedIndex = 1;
                    }
                    Add(subElement, level + 1);
                }
            }
        }

        private void Add(GUIElement element)
        {
            if (null == element)
            {
                return;
            }
            m_InternalElements.Add(element);
            m_Rebuild = true;
        }

        private void SetElements(List<GUIElement> elements)
        {
            Reset();

            m_Tree = elements.ToArray();

            if (m_Stack.Count == 0)
                m_Stack.Add(m_Tree[0] as GroupElement);
            else
            {
                // The root is always the match for level 0
                GroupElement match = m_Tree[0] as GroupElement;
                int level = 0;
                while (true)
                {
                    // Assign the match for the current level
                    GroupElement oldElement = m_Stack[level];
                    m_Stack[level] = match;
                    m_Stack[level].selectedIndex = oldElement.selectedIndex;
                    m_Stack[level].scroll = oldElement.scroll;

                    // See if we reached last element of stack
                    level++;
                    if (level == m_Stack.Count)
                        break;

                    // Try to find a child of the same name as we had before
                    List<GUIElement> children = GetChildren(activeTree, match);
                    GUIElement childMatch = children.FirstOrDefault(c => c.name == m_Stack[level].name);
                    if (childMatch != null && childMatch is GroupElement)
                    {
                        match = childMatch as GroupElement;
                    }
                    else
                    {
                        // If we couldn't find the child, remove all further elements from the stack
                        while (m_Stack.Count > level)
                            m_Stack.RemoveAt(level);
                    }
                }
            }

            RebuildSearch();

            m_Rebuild = false;
        }

        private void Reset()
        {
            m_Tree = new GUIElement[0];
            m_Stack.Clear();
            m_SearchResultTree = new GUIElement[0];
        }

        public void OnGUI(Rect position)
        {
            if (m_Rebuild)
            {
                // Merge groups recursively
                    // Sort them
                foreach(var element in m_Elements)
                {
                    Add(element, 1);
                }
                SetElements(m_InternalElements);
            }
            Position = position;

            if (s_Styles == null)
                s_Styles = new Styles();

            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, s_Styles.background);

            // Keyboard
            HandleKeyboard();

            GUILayout.Space(7);

            // Search
            Rect searchRect = GUILayoutUtility.GetRect(10, 20);
            searchRect.x += 8;
            searchRect.width -= 16;

            GUI.SetNextControlName("ComponentSearch");
            string newSearch = s_ComponentSearchField.OnGUI(searchRect, m_DelayedSearch ?? m_Search);

            if (newSearch != m_Search || m_DelayedSearch != null)
            {
                if (!IsAnimating)
                {
                    m_Search = m_DelayedSearch ?? newSearch;
                    RebuildSearch();
                    m_DelayedSearch = null;
                }
                else
                {
                    m_DelayedSearch = newSearch;
                }
            }

            // Show lists
            ListGUI(activeTree, m_Anim, GetElementRelative(0), GetElementRelative(-1));
            if (m_Anim < 1)
                ListGUI(activeTree, m_Anim + 1, GetElementRelative(-1), GetElementRelative(-2));

            // Animate
            if (IsAnimating && Event.current.type == EventType.Repaint)
            {
                long now = System.DateTime.Now.Ticks;
                float deltaTime = (now - m_LastTime) / (float)System.TimeSpan.TicksPerSecond;
                m_LastTime = now;
                m_Anim = Mathf.MoveTowards(m_Anim, m_AnimTarget, deltaTime * 4);
                if (m_AnimTarget == 0 && m_Anim == 0)
                {
                    m_Anim = 1;
                    m_AnimTarget = 1;
                    m_Stack.RemoveAt(m_Stack.Count - 1);
                }
                OnStateChanged.Invoke();
            }
        }

        #endregion

        #region Implementation
        private void HandleKeyboard()
        {
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                // Always do these
                if (evt.keyCode == KeyCode.DownArrow)
                {
                    activeParent.selectedIndex++;
                    activeParent.selectedIndex = Mathf.Min(activeParent.selectedIndex, GetChildren(activeTree, activeParent).Count - 1);
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.UpArrow)
                {
                    activeParent.selectedIndex--;
                    activeParent.selectedIndex = Mathf.Max(activeParent.selectedIndex, activeParent.element?.Children.Any(c => c.IsWarning) ?? false ? 1 : 0 );
                    m_ScrollToSelected = true;
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    GoToChild(activeElement, true);
                    evt.Use();
                }

                // Do these if we're not in search mode
                if (!hasSearch)
                {
                    if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.Backspace)
                    {
                        GoToParent();
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.RightArrow)
                    {
                        if (activeElement is GroupElement)
                        {
                            GoToChild(activeElement, false);
                        }

                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        GUIUtility.keyboardControl = 0;
                        OnEscapePressed.Invoke();
                        evt.Use();
                    }
                }

            }
        }

        private void GoToParent()
        {
            if (m_Stack.Count > 1)
            {
                m_AnimTarget = 0;
                m_LastTime = System.DateTime.Now.Ticks;
            }
        }

        private void GoToChild(GUIElement e, bool addIfComponent)
        {
            if (e is LeafElement)
            {
                var componentElement = e as LeafElement;
                componentElement.element.InvokeCallback();
                OnAnyLeafElementClicked.Invoke(componentElement.element);
            }
            else if (e is WarningElement || e is SeparatorElement)
            {

            }
            else if (!hasSearch)
            {
                m_LastTime = System.DateTime.Now.Ticks;
                if (m_AnimTarget == 0)
                    m_AnimTarget = 1;
                else if (m_Anim == 1)
                {
                    m_Anim = 0;
                    m_Stack.Add(e as GroupElement);
                }
            }
        }

        private void ListGUI(GUIElement[] tree, float anim, GroupElement parent, GroupElement grandParent)
        {
            // Smooth the fractional part of the anim value
            anim = Mathf.Floor(anim) + Mathf.SmoothStep(0, 1, Mathf.Repeat(anim, 1));

            // Calculate rect for animated area
            Rect animRect = Position;
            animRect.x = Position.width * (1 - anim) + 1;
            animRect.y = kHeaderHeight;
            animRect.height -= kHeaderHeight;
            animRect.width -= 2;

            // Start of animated area (the part that moves left and right)
            GUILayout.BeginArea(animRect);
            try
            {
                // Header
                Rect headerRect = GUILayoutUtility.GetRect(10, 25);
                string name = parent.name;
                GUI.Label(headerRect, name, s_Styles.header);

                // Back button
                if (grandParent != null)
                {
                    Rect arrowRect = new Rect(headerRect.x + 4, headerRect.y + 7, 13, 13);
                    if (Event.current.type == EventType.Repaint)
                        s_Styles.leftArrow.Draw(arrowRect, false, false, false, false);
                    if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                    {
                        GoToParent();
                        Event.current.Use();
                    }
                }

                ListGUI(tree, parent);
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void ListGUI(GUIElement[] tree, GroupElement parent)
        {
            // Start of scroll view list
            parent.scroll = GUILayout.BeginScrollView(parent.scroll);

            EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            List<GUIElement> children = GetChildren(tree, parent);

            Rect selectedRect = new Rect();

            // Iterate through the children
            for (int i = 0; i < children.Count; i++)
            {
                GUIElement e = children[i];

                Rect r;
                if (e is WarningElement)
                {
                    var height = s_Styles.warning.CalcHeight(e.content, Position.width);
                    var lines = (int)height / 20;
                    r = GUILayoutUtility.GetRect(16, (lines+1) * 20, GUILayout.ExpandWidth(true));
                }
                else if (e is SeparatorElement)
                {
                    r = GUILayoutUtility.GetRect(16, 3, GUILayout.ExpandWidth(true));
                }
                else
                {
                    r = GUILayoutUtility.GetRect(16, 20, GUILayout.ExpandWidth(true));
                }

                // Select the element the mouse cursor is over.
                // Only do it on mouse move - keyboard controls are allowed to overwrite this until the next time the mouse moves.
                if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown)
                {
                    if (parent.selectedIndex != i && r.Contains(Event.current.mousePosition))
                    {
                        parent.selectedIndex = i;
                        OnStateChanged.Invoke();
                    }
                }

                bool selected = false;
                // Handle selected item
                if (i == parent.selectedIndex)
                {
                    selected = true;
                    selectedRect = r;
                }

                // Draw element
                if (Event.current.type == EventType.Repaint)
                {
                    if (e is SeparatorElement)
                    {
                        TinyGUI.BackgroundColor(r, TinyColors.Inspector.Separator);
                        //continue;
                    }
                    else
                    {
                        GUIStyle labelStyle;
                        GUIContent labelContent = new GUIContent(e.content);
                        if (hasSearch)
                        {
                            labelContent.text += e.SearchPostfix;
                        }

                        bool isComponent = e is LeafElement;
                        bool isWarning = e is WarningElement;
                        bool isGroup = e is GroupElement;

                        if (isWarning)
                        {
                            labelStyle = s_Styles.warning;
                        }
                        else if (isComponent)
                        {
                            labelStyle = s_Styles.componentButton;
                        }
                        else
                        {
                            labelStyle = s_Styles.groupButton;
                        }

                        if (!isWarning)
                        {
                            labelStyle.normal.textColor = e.element.Included ? TinyColors.AnimatedTree.IncludedItem : TinyColors.AnimatedTree.NonIncludedItem;
                        }

                        labelStyle.normal.background = MakeTex(1, 1, e.element.Color);
                        GUI.Label(r, labelContent, labelStyle);
                        labelStyle.Draw(r, labelContent, false, false, selected, selected);

                        if (isGroup)
                        {
                            Rect arrowRect = new Rect(r.x + r.width - 13, r.y + 4, 13, 13);
                            s_Styles.rightArrow.Draw(arrowRect, false, false, false, false);
                        }
                    }
                }
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    parent.selectedIndex = i;
                    GoToChild(e, true);
                }
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);

            GUILayout.EndScrollView();

            // Scroll to show selected
            if (m_ScrollToSelected && Event.current.type == EventType.Repaint)
            {
                m_ScrollToSelected = false;
                Rect scrollRect = GUILayoutUtility.GetLastRect();
                if (selectedRect.yMax - scrollRect.height > parent.scroll.y)
                {
                    parent.scroll.y = selectedRect.yMax - scrollRect.height;
                    OnStateChanged.Invoke();
                }
                if (selectedRect.y < parent.scroll.y)
                {
                    parent.scroll.y = selectedRect.y;
                    OnStateChanged.Invoke();
                }
            }
        }

        private List<GUIElement> GetChildren(GUIElement[] tree, GUIElement parent)
        {
            List<GUIElement> children = new List<GUIElement>();
            int level = -1;
            int i = 0;
            for (i = 0; i < tree.Length; i++)
            {
                if (tree[i] == parent)
                {
                    level = parent.level + 1;
                    i++;
                    break;
                }
            }
            if (level == -1)
                return children;

            for (; i < tree.Length; i++)
            {
                GUIElement e = tree[i];

                if (e.level < level)
                    break;
                if (e.level > level && !hasSearch)
                    continue;

                children.Add(e);
            }

            return children;
        }

        private GroupElement GetElementRelative(int rel)
        {
            int i = m_Stack.Count + rel - 1;
            if (i < 0)
                return null;
            return m_Stack[i] as GroupElement;
        }

        private void RebuildSearch()
        {
            if (!hasSearch)
            {
                m_SearchResultTree = null;
                if (m_Stack[m_Stack.Count - 1].name == kSearchHeader)
                {
                    m_Stack.Clear();
                    m_Stack.Add(m_Tree[0] as GroupElement);
                }
                m_AnimTarget = 1;
                m_LastTime = System.DateTime.Now.Ticks;
                return;
            }

            // Support multiple search words separated by spaces.
            string[] searchWords = m_Search.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            List<GUIElement> matchesStart = new List<GUIElement>();
            List<GUIElement> matchesWithin = new List<GUIElement>();

            foreach (GUIElement e in m_Tree)
            {
                if (!(e is LeafElement) || !e.Searchable)
                    continue;

                string name = e.name.ToLower().Replace(" ", "");
                bool didMatchAll = true;
                bool didMatchStart = false;

                // See if we match ALL the search words.
                for (int w = 0; w < searchWords.Length; w++)
                {
                    string search = searchWords[w];
                    if (name.Contains(search))
                    {
                        // If the start of the item matches the first search word, make a note of that.
                        if (w == 0 && name.StartsWith(search))
                            didMatchStart = true;
                    }
                    else
                    {
                        // As soon as any word is not matched, we disregard this item.
                        didMatchAll = false;
                        break;
                    }
                }
                // We always need to match all search words.
                // If we ALSO matched the start, this item gets priority.
                if (didMatchAll)
                {
                    if (didMatchStart)
                        matchesStart.Add(e);
                    else
                        matchesWithin.Add(e);
                }
            }

            matchesStart.Sort();
            matchesWithin.Sort();

            // Create search tree
            List<GUIElement> tree = new List<GUIElement>();
            // Add parent
            tree.Add(new GroupElement(0, kSearchHeader, ""));
            // Add search results
            tree.AddRange(matchesStart);
            tree.AddRange(matchesWithin);
            // Add the new script element
            // Create search result tree
            m_SearchResultTree = tree.ToArray();
            m_Stack.Clear();
            m_Stack.Add(m_SearchResultTree[0] as GroupElement);

            // Always select the first search result when search is changed (e.g. a character was typed in or deleted),
            // because it's usually the best match.
            if (GetChildren(activeTree, activeParent).Count >= 1)
                activeParent.selectedIndex = 0;
            else
                activeParent.selectedIndex = -1;
        }

        private void MoveFocusOnKeyPress()
        {
            var key = Event.current.keyCode;

            if (Event.current.type != EventType.KeyDown)
                return;

            if (key == KeyCode.DownArrow)
            {
                m_LastIndex += 1;
            }
            else if (key == KeyCode.UpArrow)
            {
                m_LastIndex -= 1;
            }
            else if (key == KeyCode.KeypadEnter || key == KeyCode.Return)
            {
            }

            Event.current.Use();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width*height];

            for(int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        #endregion
    }
}

