//#define QUICKSEARCH_DEBUG
//#define QUICKSEARCH_DEBUG_WINDOW
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Unity.QuickSearch
{
    internal class FilterWindow : EditorWindow
    {
        static class Styles
        {
            public static float indent = 10f;
            public static Vector2 windowSize = new Vector2(200, 250);
            public static readonly GUIStyle filterHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                name = "quick-search-filter-header",
                margin = new RectOffset(4, 4, 0, 4)
            };

            public static readonly GUIStyle filterEntry = new GUIStyle(EditorStyles.label) { name = "quick-search-filter-entry" };
            public static readonly GUIStyle panelBorder = new GUIStyle("grey_border") { name = "quick-search-filter-panel-border" };
            public static readonly GUIStyle filterToggle = new GUIStyle("OL Toggle") { name = "quick-search-filter-toggle" };
            public static readonly GUIStyle filterExpanded = new GUIStyle("IN Foldout") { name = "quick-search-filter-expanded" };
            public static readonly GUIStyle separator = "sv_iconselector_sep";

            public static float foldoutIndent = filterExpanded.fixedWidth + 6;
        }

        public QuickSearchTool quickSearchTool;

        private Vector2 m_ScrollPos;

        internal static double s_CloseTime;
        internal static bool canShow
        {
            get
            {
                if (EditorApplication.timeSinceStartup - s_CloseTime < 0.250)
                    return false;
                return true;
            }
        }

        public static bool ShowAtPosition(QuickSearchTool quickSearchTool, Rect rect)
        {
            var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
            var screenRect = new Rect(screenPos, rect.size);
            var filterWindow = ScriptableObject.CreateInstance<FilterWindow>();
            filterWindow.quickSearchTool = quickSearchTool;
            filterWindow.ShowAsDropDown(screenRect, Styles.windowSize);
            return true;
        }

        [UsedImplicitly]
        internal void OnDestroy()
        {
            s_CloseTime = EditorApplication.timeSinceStartup;
        }

        [UsedImplicitly]
        internal void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                return;
            }

            GUI.Box(new Rect(0, 0, position.width, position.height), GUIContent.none, Styles.panelBorder);
            GUILayout.Space(Styles.indent);
            DrawHeader();
            GUILayout.Label(GUIContent.none, Styles.separator);

            m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);
             
            foreach (var providerDesc in SearchService.Filter.providerFilters.OrderBy(f => f.priority))
            {
                DrawSectionHeader(providerDesc);
                if (providerDesc.isExpanded)
                    DrawSubCategories(providerDesc);
            }

            GUILayout.Space(Styles.indent);
            GUILayout.EndScrollView();
        }

        void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search Providers", Styles.filterHeader);
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            bool isEnabled = GUILayout.Toggle(SearchService.Filter.providerFilters.All(p => p.entry.isEnabled), "", Styles.filterToggle, GUILayout.ExpandWidth(false));
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var provider in SearchService.Filter.providerFilters)
                {
                    SearchService.Filter.SetFilter(isEnabled, provider.entry.name.id);
                }
                quickSearchTool.Refresh();
            }

            GUILayout.EndHorizontal();
        }

        void DrawSectionHeader(SearchFilter.ProviderDesc desc)
        {
            // filterHeader
            GUILayout.BeginHorizontal();

            if (desc.categories.Count > 0)
            {
                EditorGUI.BeginChangeCheck();
                bool isExpanded = GUILayout.Toggle(desc.isExpanded, "", Styles.filterExpanded);
                if (EditorGUI.EndChangeCheck())
                {
                    SearchService.Filter.SetExpanded(isExpanded, desc.entry.name.id);
                }
            }
            else
            {
                GUILayout.Space(Styles.foldoutIndent);
            }

            GUILayout.Label(desc.entry.name.displayName, Styles.filterHeader);
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            bool isEnabled = GUILayout.Toggle(desc.entry.isEnabled, "", Styles.filterToggle, GUILayout.ExpandWidth(false));
            if (EditorGUI.EndChangeCheck())
            {
                SearchService.Filter.SetFilter(isEnabled, desc.entry.name.id);
                quickSearchTool.Refresh();
            }

            GUILayout.EndHorizontal();
        }

        void DrawSubCategories(SearchFilter.ProviderDesc desc)
        {
            foreach (var cat in desc.categories)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(Styles.foldoutIndent + 5);
                GUILayout.Label(cat.name.displayName, Styles.filterEntry);
                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                bool isEnabled = GUILayout.Toggle(cat.isEnabled, "", Styles.filterToggle);
                if (EditorGUI.EndChangeCheck())
                {
                    SearchService.Filter.SetFilter(isEnabled, desc.entry.name.id, cat.name.id);
                    quickSearchTool.Refresh();
                }

                GUILayout.EndHorizontal();
            }
        }
    }

    internal static class Icons
    {
        public static string iconFolder = "Packages/com.unity.quicksearch/Editor/Icons";
        public static Texture2D shortcut = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/shortcut.png");
        public static Texture2D quicksearch = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/quicksearch.png");
        public static Texture2D filter = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/filter.png");
        public static Texture2D settings = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/settings.png");
        public static Texture2D search = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/search.png");
        public static Texture2D clear = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/clear.png");
        public static Texture2D more = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/more.png");
        public static Texture2D store = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/store.png");

        static Icons()
        {
            if (EditorGUIUtility.isProSkin)
            {
                shortcut = LightenTexture(shortcut);
                quicksearch = LightenTexture(quicksearch);
                filter = LightenTexture(filter);
                settings = LightenTexture(settings);
                search = LightenTexture(search);
                clear = LightenTexture(clear);
                more = LightenTexture(more);
                store = LightenTexture(store);
            }
        }

        private static Texture2D LightenTexture(Texture2D texture)
        {
            Texture2D outTexture = new Texture2D(texture.width, texture.height);
            var outColorArray = outTexture.GetPixels();

            var colorArray = texture.GetPixels();
            for (var i = 0; i < colorArray.Length; ++i)
                outColorArray[i] = LightenColor(colorArray[i]);

            outTexture.hideFlags = HideFlags.HideAndDontSave;
            outTexture.SetPixels(outColorArray);
            outTexture.Apply();

            return outTexture;
        }

        public static Color LightenColor(Color color)
        {
            Color.RGBToHSV(color, out var h, out _, out _);
            var outColor = Color.HSVToRGB((h + 0.5f) % 1, 0f, 0.8f);
            outColor.a = color.a;
            return outColor;
        }
    }

    internal class QuickSearchTool : EditorWindow
    {
        public static EditorWindow s_FocusedWindow;

        [SerializeField] private Vector2 m_ScrollPosition;
        [SerializeField] public EditorWindow lastFocusedWindow;
        [SerializeField] private int m_SelectedIndex = -1;

        private SearchContext m_Context;
        private List<SearchItem> m_FilteredItems;
        private bool m_FocusSelectedItem = false;
        private Rect m_ScrollViewOffset;
        private bool m_SearchBoxFocus;
        private double m_ClickTime = 0;

        private const string k_QuickSearchBoxName = "QuickSearchBox";

        enum RectVisibility
        {
            None,
            HiddenTop,
            HiddenBottom,
            PartiallyHiddenTop,
            PartiallyHiddenBottom,
            Visible
        }

        static class Styles
        {
            static Styles()
            {
                if (!hasPro)
                {
                    selectedItemLabel.normal.textColor = Color.white;
                    selectedItemDescription.normal.textColor = Color.white;
                }
            }
            private const int itemRowPadding = 4;
            private const float actionButtonSize = 24f;
            private const float itemPreviewSize = 32f;
            private const int actionButtonMargin = (int)((itemRowHeight - actionButtonSize) / 2f);
            public const float itemRowHeight = itemPreviewSize + itemRowPadding * 2f;

            private static bool hasPro => EditorGUIUtility.isProSkin;

            private static readonly RectOffset marginNone = new RectOffset(0, 0, 0, 0);
            private static readonly RectOffset paddingNone = new RectOffset(0, 0, 0, 0);
            private static readonly RectOffset defaultPadding = new RectOffset(itemRowPadding, itemRowPadding, itemRowPadding, itemRowPadding);

            private static readonly Color darkColor1 = new Color(61 / 255f, 61 / 255f, 61 / 255f);
            private static readonly Color darkColor2 = new Color(71 / 255f, 106 / 255f, 155 / 255f);
            private static readonly Color darkColor3 = new Color(68 / 255f, 68 / 255f, 71 / 255f);
            private static readonly Color darkColor4 = new Color(111 / 255f, 111 / 255f, 111 / 255f);
            private static readonly Color darkColor5 = new Color(71 / 255f, 71 / 255f, 71 / 255f);
            private static readonly Color darkColor6 = new Color(63 / 255f, 63 / 255f, 63 / 255f);
            private static readonly Color darkColor7 = new Color(71 / 255f, 71 / 255f, 71 / 255f); // TODO: Update me

            private static readonly Color lightColor1 = new Color(171 / 255f, 171 / 255f, 171 / 255f);
            private static readonly Color lightColor2 = new Color(71 / 255f, 106 / 255f, 155 / 255f);
            private static readonly Color lightColor3 = new Color(168 / 255f, 168 / 255f, 171 / 255f);
            private static readonly Color lightColor4 = new Color(111 / 255f, 111 / 255f, 111 / 255f);
            private static readonly Color lightColor5 = new Color(181 / 255f, 181 / 255f, 181 / 255f);
            private static readonly Color lightColor6 = new Color(214 / 255f, 214 / 255f, 214 / 255f);
            private static readonly Color lightColor7 = new Color(230 / 255f, 230 / 255f, 230 / 255f);

            private static readonly Color darkSelectedRowColor = new Color(61 / 255f, 96 / 255f, 145 / 255f);
            private static readonly Color lightSelectedRowColor = new Color(61 / 255f, 128 / 255f, 223 / 255f);

            private static readonly Texture2D debugBackgroundImage = GenerateSolidColorTexture(new Color(1f, 0f, 0f));
            private static readonly Texture2D alternateRowBackgroundImage = GenerateSolidColorTexture(hasPro ? darkColor1 : lightColor1);
            private static readonly Texture2D selectedRowBackgroundImage = GenerateSolidColorTexture(hasPro ? darkSelectedRowColor : lightSelectedRowColor);
            private static readonly Texture2D selectedHoveredRowBackgroundImage = GenerateSolidColorTexture(hasPro ? darkColor2 : lightColor2);
            private static readonly Texture2D hoveredRowBackgroundImage = GenerateSolidColorTexture(hasPro ? darkColor3 : lightColor3);
            private static readonly Texture2D buttonPressedBackgroundImage = GenerateSolidColorTexture(hasPro ? darkColor4 : lightColor4);
            private static readonly Texture2D buttonHoveredBackgroundImage = GenerateSolidColorTexture(hasPro ? darkColor5 : lightColor5);

            private static readonly Texture2D searchFieldBg = GenerateSolidColorTexture(hasPro ? darkColor6 : lightColor6);
            private static readonly Texture2D searchFieldFocusBg = GenerateSolidColorTexture(hasPro ? darkColor7 : lightColor7);

            public static readonly GUIStyle panelBorder = new GUIStyle("grey_border")
            {
                name = "quick-search-border", 
                padding = new RectOffset(1, 1, 1, 1), 
                margin = new RectOffset(0, 0, 0, 0)
            };
            public static readonly GUIContent filterButtonContent = new GUIContent("", Icons.filter);

            public static readonly GUIStyle itemBackground1 = new GUIStyle
            {
                name = "quick-search-item-background1",
                fixedHeight = itemRowHeight,

                margin = marginNone,
                padding = defaultPadding,

                hover = new GUIStyleState { background = hoveredRowBackgroundImage, scaledBackgrounds = new[] { hoveredRowBackgroundImage } }
            };

            public static readonly GUIStyle itemBackground2 = new GUIStyle(itemBackground1)
            {
                name = "quick-search-item-background2",
                normal = new GUIStyleState { background = alternateRowBackgroundImage, scaledBackgrounds = new[] { alternateRowBackgroundImage } }
            };

            public static readonly GUIStyle selectedItemBackground = new GUIStyle(itemBackground1)
            {
                name = "quick-search-item-selected-background",
                normal = new GUIStyleState { background = selectedRowBackgroundImage, scaledBackgrounds = new[] { selectedRowBackgroundImage } },
                hover = new GUIStyleState { background = selectedHoveredRowBackgroundImage, scaledBackgrounds = new[] { selectedHoveredRowBackgroundImage } }
            };

            public static readonly GUIStyle preview = new GUIStyle
            {
                name = "quick-search-item-preview",
                fixedWidth = itemPreviewSize,
                fixedHeight = itemPreviewSize,

                margin = new RectOffset(2, 2, 2, 2),
                padding = paddingNone
            };

            public static readonly GUIStyle itemLabel = new GUIStyle(EditorStyles.label)
            {
                name = "quick-search-item-label",

                margin = new RectOffset(4, 4, 6, 2),
                padding = paddingNone
            };

            public static readonly GUIStyle selectedItemLabel = new GUIStyle(itemLabel)
            {
                name = "quick-search-item-selected-label",

                margin = new RectOffset(4, 4, 6, 2),
                padding = paddingNone
            };

            public static readonly GUIStyle noResult = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                name = "quick-search-no-result",
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                margin = marginNone,
                padding = paddingNone
            };

            public static readonly GUIStyle itemDescription = new GUIStyle(EditorStyles.label)
            {
                name = "quick-search-item-description",

                margin = new RectOffset(4, 4, 1, 4),
                padding = paddingNone,

                fontSize = Math.Max(9, itemLabel.fontSize - 2),
                fontStyle = FontStyle.Italic
            };

            public static readonly GUIStyle selectedItemDescription = new GUIStyle(itemDescription)
            {
                name = "quick-search-item-selected-description"
            };

            public static readonly GUIStyle actionButton = new GUIStyle("IconButton")
            {
                name = "quick-search-action-button",

                fixedWidth = actionButtonSize,
                fixedHeight = actionButtonSize,

                imagePosition = ImagePosition.ImageOnly,

                margin = new RectOffset(4, 4, actionButtonMargin, actionButtonMargin),
                padding = paddingNone,

                active = new GUIStyleState { background = buttonPressedBackgroundImage, scaledBackgrounds = new[] { buttonPressedBackgroundImage } },
                hover = new GUIStyleState { background = buttonHoveredBackgroundImage, scaledBackgrounds = new[] { buttonHoveredBackgroundImage } }
            };

            private const float k_ToolbarHeight = 40.0f;

            private static readonly GUIStyleState clear = new GUIStyleState()
            {
                background = null, 
                scaledBackgrounds = new Texture2D[] { null },
                textColor = hasPro ? new Color (210 / 255f, 210 / 255f, 210 / 255f) : Color.black
            };

            private static readonly GUIStyleState searchFieldBgNormal = new GUIStyleState() { background = searchFieldBg, scaledBackgrounds = new Texture2D[] { null } };
            private static readonly GUIStyleState searchFieldBgFocus = new GUIStyleState() { background = searchFieldFocusBg, scaledBackgrounds = new Texture2D[] { null } };

            public static readonly GUIStyle toolbar = new GUIStyle("Toolbar")
            {
                name = "quick-search-bar",
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                fixedHeight = k_ToolbarHeight,

                normal = searchFieldBgNormal,
                focused = searchFieldBgFocus, hover = searchFieldBgFocus, active = searchFieldBgFocus,
                onNormal = clear, onHover = searchFieldBgFocus, onFocused = searchFieldBgFocus, onActive = searchFieldBgFocus,
            };

            public static readonly GUIStyle searchField = new GUIStyle("ToolbarSeachTextFieldPopup")
            {
                name = "quick-search-search-field",
                fontSize = 28,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                fixedHeight = 0,
                normal = clear,
                focused = clear, hover = clear, active = clear,
                onNormal = clear, onHover = clear, onFocused = clear, onActive = clear,
            };
            public static readonly GUIStyle searchFieldClear = new GUIStyle()
            {
                name = "quick-search-search-field-clear",
                fixedHeight = 0,
                fixedWidth = 0,
                margin = new RectOffset(0, 5, 8, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal = clear,
                focused = clear, hover = clear, active = clear,
                onNormal = clear, onHover = clear, onFocused = clear, onActive = clear,
            };
            public static readonly GUIStyle filterButton = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                name = "quick-search-filter-button",
                margin = new RectOffset(-4, 0, 0, 0),
                padding = new RectOffset(0, 0, 1, 0),
                normal = clear,
                focused = clear, hover = clear, active = clear,
                onNormal = clear, onHover = clear, onFocused = clear, onActive = clear
            };

            private static Texture2D GenerateSolidColorTexture(Color fillColor)
            {
                Texture2D texture = new Texture2D(1, 1);
                var fillColorArray = texture.GetPixels();

                for (var i = 0; i < fillColorArray.Length; ++i)
                    fillColorArray[i] = fillColor;

                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.SetPixels(fillColorArray);
                texture.Apply();

                return texture;
            }
        }

        [UsedImplicitly]
        internal void OnEnable()
        {
            m_Context = new SearchContext() { searchText = SearchService.LastSearch, focusedWindow = lastFocusedWindow };
            m_SearchBoxFocus = true;
            lastFocusedWindow = s_FocusedWindow;
            UpdateWindowTitle();
        }

        [UsedImplicitly]
        internal void OnDisable()
        {
            SearchService.LastSearch = m_Context.searchText;
            SearchService.SaveAll();
        }

        private void UpdateWindowTitle()
        {
            titleContent.image = Icons.quicksearch;
            if (m_FilteredItems == null || m_FilteredItems.Count == 0)
                titleContent.text = "Search Anything!";
            else
                titleContent.text = $"Found {m_FilteredItems.Count} Anything!";
        }

        [UsedImplicitly]
        internal void OnGUI()
        {
            SearchService.SearchTextChanged(m_Context);

            HandleKeyboardNavigation(m_Context);

            EditorGUILayout.BeginVertical(Styles.panelBorder);
            {
                DrawToolbar(m_Context);
                DrawItems(m_Context);
            }
            EditorGUILayout.EndVertical();

            UpdateFocusControlState();
        }

        public void Refresh()
        {
            SearchService.SearchTextChanged(m_Context);
            m_FilteredItems = SearchService.GetItems(m_Context);
            m_SelectedIndex = -1;
            UpdateWindowTitle();
            Repaint();
        }

        private void UpdateFocusControlState()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (m_SearchBoxFocus)
            {
                m_SearchBoxFocus = false;
                GUI.FocusControl(k_QuickSearchBoxName);
            }
        }

        private void HandleKeyboardNavigation(SearchContext context)
        {
            // TODO: support page down and page up
            // TODO: add support for left and right arrow key to change action
            // TODO: add support for space and enter key to trigger selected action

            var evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                var prev = m_SelectedIndex;
                if (evt.keyCode == KeyCode.DownArrow)
                {
                    m_SelectedIndex = Math.Min(m_SelectedIndex + 1, m_FilteredItems.Count - 1);
                    Event.current.Use();
                }
                else if (evt.keyCode == KeyCode.UpArrow)
                {
                    m_SelectedIndex = Math.Max(0, m_SelectedIndex - 1);
                    Event.current.Use();
                }
                else if (m_SelectedIndex >= 0 && (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return))
                {
                    int actionIndex = 0;
                    if (evt.modifiers.HasFlag(EventModifiers.Alt))
                    {
                        actionIndex = 1;
                        if (evt.modifiers.HasFlag(EventModifiers.Control))
                        {
                            actionIndex = 2;
                            if (evt.modifiers.HasFlag(EventModifiers.Shift))
                                actionIndex = 3;
                        }
                    }
                    var item = m_FilteredItems.ElementAt(m_SelectedIndex);
                    if (item.provider.actions.Any())
                    {
                        Event.current.Use();
                        actionIndex = Math.Max(0, Math.Min(actionIndex, item.provider.actions.Count - 1));

                        ExecuteAction(item.provider.actions[actionIndex], item, context);
                        GUIUtility.ExitGUI();
                    }
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                }
                else
                    GUI.FocusControl(k_QuickSearchBoxName);

                if (prev != m_SelectedIndex)
                    m_FocusSelectedItem = true;
            }

            if (m_FilteredItems == null || m_FilteredItems.Count == 0)
                m_SearchBoxFocus = true;
        }

        private void HandleItemEvents(int itemTotalCount, SearchContext context)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                var clickedItemIndex = (int)(Event.current.mousePosition.y / Styles.itemRowHeight);
                if (clickedItemIndex >= 0 && clickedItemIndex < itemTotalCount)
                {
                    m_SelectedIndex = clickedItemIndex;
                    if ((EditorApplication.timeSinceStartup - m_ClickTime) < 0.2)
                    {
                        var item = m_FilteredItems.ElementAt(m_SelectedIndex);
                        ExecuteAction(item.provider.actions[0], item, context);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUI.FocusTextInControl(k_QuickSearchBoxName);
                    Event.current.Use();
                }
                m_ClickTime = EditorApplication.timeSinceStartup;
            }
        }

        private void DrawItems(SearchContext context)
        {
            UpdateScrollAreaOffset();

            context.totalItemCount = m_FilteredItems.Count;

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(m_ScrollPosition))
            {
                m_ScrollPosition = scrollViewScope.scrollPosition;

                var itemCount = m_FilteredItems.Count;
                var availableHeight = position.height - m_ScrollViewOffset.yMax;
                var itemSkipCount = Math.Max(0, (int)(m_ScrollPosition.y / Styles.itemRowHeight));
                var itemDisplayCount = Math.Max(0, Math.Min(itemCount, (int)(availableHeight / Styles.itemRowHeight) + 2));
                var topSpaceSkipped = itemSkipCount * Styles.itemRowHeight;

                int rowIndex = itemSkipCount;
                var limitCount = Math.Max(0, Math.Min(itemDisplayCount, itemCount - itemSkipCount));
                if (limitCount > 0)
                {
                    if (topSpaceSkipped > 0)
                        GUILayout.Space(topSpaceSkipped);

                    foreach (var item in m_FilteredItems.GetRange(itemSkipCount, limitCount))
                    {
                        try
                        {
                            DrawItem(item, context, rowIndex++);
                        }
                        #if QUICKSEARCH_DEBUG
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                        #else
                        catch
                        {
                            // ignored
                        }
                        #endif
                    }

                    var bottomSpaceSkipped = (itemCount - rowIndex) * Styles.itemRowHeight;
                    if (bottomSpaceSkipped > 0)
                        GUILayout.Space(bottomSpaceSkipped);

                    HandleItemEvents(itemCount, context);

                    // Fix selected index display if out of virtual scrolling area
                    if (Event.current.type == EventType.Repaint && m_FocusSelectedItem && m_SelectedIndex >= 0)
                    {
                        ScrollToItem(itemSkipCount + 1, itemSkipCount + itemDisplayCount - 2, m_SelectedIndex);
                        m_FocusSelectedItem = false;
                    }
                }
                else
                {
                    GUILayout.Box("What are you looking for?\nJust start typing...",
                        Styles.noResult, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                }
            }
        }

        private void ExecuteAction(SearchAction action, SearchItem item, SearchContext context)
        {
            SearchService.LastSearch = context.searchText;
            action.handler(item, context);

            // Close the window after execute the selected item
            Close();
        }

        private void ScrollToItem(int start, int end, int selection)
        {
            if (start <= selection && selection < end)
                return;

            Rect projectedSelectedItemRect = new Rect(0, selection * Styles.itemRowHeight, position.width, Styles.itemRowHeight);
            if (selection < start)
            {
                m_ScrollPosition.y = Mathf.Max(0, projectedSelectedItemRect.y - 2);
                Repaint();
            }
            else if (selection > end)
            {
                Rect visibleRect = GetVisibleRect();
                m_ScrollPosition.y += (projectedSelectedItemRect.yMax - visibleRect.yMax) + 2;
                Repaint();
            }
        }

        private void UpdateScrollAreaOffset()
        {
            var rect = GUILayoutUtility.GetLastRect();
            if (rect.height > 1)
                m_ScrollViewOffset = rect;
        }

        private void DrawToolbar(SearchContext context)
        {
            if (context == null)
                return;
            
            GUILayout.BeginHorizontal(Styles.toolbar);
            {
                var rightRect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(32f), GUILayout.ExpandHeight(true));
                if (EditorGUI.DropdownButton(rightRect, Styles.filterButtonContent, FocusType.Passive, Styles.filterButton))
                {
                    if (FilterWindow.canShow)
                    {
                        rightRect.x += 12f; rightRect.y -= 3f;
                        if (FilterWindow.ShowAtPosition(this, rightRect))
                            GUIUtility.ExitGUI();
                    }
                }

                EditorGUI.BeginChangeCheck();
                GUI.SetNextControlName(k_QuickSearchBoxName);
                context.searchText = EditorGUILayout.TextField(context.searchText, Styles.searchField, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                if (!String.IsNullOrEmpty(context.searchText))
                {
                    if (GUILayout.Button(Icons.clear, Styles.searchFieldClear, GUILayout.Width(24), GUILayout.Height(24)))
                    {
                        m_SelectedIndex = -1;
                        context.searchText = "";
                        GUI.changed = true;
                        GUI.FocusControl(null);
                    }
                }

                if (EditorGUI.EndChangeCheck() || m_FilteredItems == null)
                {
                    m_SelectedIndex = -1;
                    SearchService.SearchTextChanged(context);
                    m_FilteredItems = SearchService.GetItems(context);
                    UpdateWindowTitle();
                }

                #if QUICKSEARCH_DEBUG
                DrawDebugTools();
                #endif
            }
            GUILayout.EndHorizontal();
        }

        #if QUICKSEARCH_DEBUG
        private void DrawDebugTools()
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                SearchService.Refresh();
                Refresh();
            }
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                SearchService.SaveAll();
            }
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
            {
                SearchService.Reset();
                Refresh();
            }
        }
        #endif

        private Rect GetVisibleRect()
        {
            Rect visibleRect = position;
            visibleRect.x = m_ScrollPosition.x;
            visibleRect.y = m_ScrollPosition.y;
            visibleRect.height -= m_ScrollViewOffset.yMax;
            return visibleRect;
        }

        private RectVisibility GetRectVisibility(Rect rect, out Rect visibleRect)
        {
            visibleRect = GetVisibleRect();

            if (rect.yMin >= visibleRect.yMin &&
                rect.yMax <= visibleRect.yMax)
                return RectVisibility.Visible;

            if (rect.yMax < visibleRect.yMin)
                return RectVisibility.HiddenTop;
            if (rect.yMin > visibleRect.yMax)
                return RectVisibility.HiddenBottom;

            if (rect.yMin < visibleRect.yMin && rect.yMax > visibleRect.yMin)
                return RectVisibility.PartiallyHiddenTop;

            if (rect.yMin < visibleRect.yMax)
                return RectVisibility.PartiallyHiddenBottom;

            return RectVisibility.None;
        }

        private void DrawItem(SearchItem item, SearchContext context, int index)
        {
            var bgStyle = index % 2 == 0 ? Styles.itemBackground1 : Styles.itemBackground2;
            if (m_SelectedIndex == index)
                bgStyle = Styles.selectedItemBackground;

            using (new EditorGUILayout.HorizontalScope(bgStyle))
            {
                GUILayout.Label(item.thumbnail ?? item.provider.fetchThumbnail(item, context), Styles.preview);

                using (new EditorGUILayout.VerticalScope())
                {
                    var textMaxWidthLayoutOption = GUILayout.MaxWidth(position.width * 0.8f);
                    GUILayout.Label(item.label ?? item.id, m_SelectedIndex == index ? Styles.selectedItemLabel : Styles.itemLabel, textMaxWidthLayoutOption);
                    GUILayout.Label(item.description ?? item.provider.fetchDescription(item, context), 
                                    m_SelectedIndex == index ? Styles.selectedItemDescription : Styles.itemDescription, textMaxWidthLayoutOption);
                }

                GUILayout.FlexibleSpace();

                if (item.provider.actions.Count > 1)
                {
                    if (GUILayout.Button(Icons.more, Styles.actionButton))
                    {
                        var menu = new GenericMenu();
                        foreach (var action in item.provider.actions)
                        {
                            menu.AddItem(new GUIContent(action.content.tooltip, action.content.image), false, () => ExecuteAction(action, item, context));
                        }
                        menu.ShowAsContext();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        #if UNITY_2019_1_OR_NEWER
        [UsedImplicitly,
         Shortcut("Window/Quick Search", KeyCode.O, ShortcutModifiers.Alt | ShortcutModifiers.Shift),
         Shortcut("Window/Quick Search (alt)", KeyCode.Quote, ShortcutModifiers.Alt)]
        [MenuItem("Window/Quick Search")]
        #else
        [MenuItem("Window/Quick Search &'")]
        #endif
        public static void PopQuickSearch()
        {
            ShowWindow();
        }

        public static void ShowWindow()
        {
            s_FocusedWindow = focusedWindow;

            #if QUICKSEARCH_DEBUG_WINDOW
            var qsWindow = GetWindow<QuickSearchTool>();
            qsWindow.Focus();
            qsWindow.CenterOnMainWin();
            qsWindow.Show(true);
            #else
            var qsWindow = CreateInstance<QuickSearchTool>();
            qsWindow.autoRepaintOnSceneChange = true;
            qsWindow.minSize = new Vector2(550, 400);
            qsWindow.position = new Rect(0, 0, qsWindow.minSize.x, qsWindow.minSize.y);
            qsWindow.CenterOnMainWin();
            qsWindow.ShowAsDropDown(Rect.zero, qsWindow.minSize);
            qsWindow.CenterOnMainWin();
            #endif
        }

        #if QUICKSEARCH_DEBUG
        [MenuItem("Tools/Clear Editor Preferences")]
        public static void ClearPreferences()
        {
            EditorPrefs.DeleteAll();
        }
        #endif
    }
}
