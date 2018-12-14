
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyContextWindow : EditorWindow
    {
        [MenuItem("Tiny/Window/Tiny Context")]
        private static void ShowRegistry()
        {
            var window = GetWindow<TinyContextWindow>();
            window.Show();
        }

        [TinyInitializeOnLoad]
        private static void RegisterCallbacks()
        {
            TinyEditorApplication.OnLoadProject += CreateTreeView;
            TinyEditorApplication.OnCloseProject += (project, context) => m_TreeView = null;
        }

        private static RegistryTreeView m_TreeView;
        private static TinyToolbar m_Toolbar;
        private static Vector2 m_ScrollPosition = Vector2.zero;
        private static RegistryTreeView.Filters m_Filter = RegistryTreeView.Filters.Everything;
        private static TinyContext s_Context;

        private static void CreateTreeView(TinyProject project, TinyContext context)
        {
            s_Context = context;
            m_TreeView = new RegistryTreeView(project.Registry, new RegistryTreeView.State());
            m_Toolbar = CreateToolbar();
            var undo = context.GetManager<IUndoManager>();
            undo.OnUndoPerformed += changes => m_TreeView.Reload();
            undo.OnRedoPerformed += changes => m_TreeView.Reload();
            context.Caretaker.OnBeginUpdate += OnBeginUpdate;
            context.Caretaker.OnBeginUpdate += OnEndUpdate;
            var scripting = context.GetManager<IScriptingManager>();
            scripting.OnMetadataLoaded += (manager) => m_TreeView.Reload();
            m_TreeView.Reload();
        }

        private static bool s_HasChanges;

        private static void OnBeginUpdate()
        {
            s_HasChanges = false;
            s_Context.Caretaker.OnGenerateMemento += SetChanged;
        }

        private static void SetChanged(IOriginator originator, IMemento memento)
        {
            s_HasChanges = true;
            s_Context.Caretaker.OnGenerateMemento -= SetChanged;
        }

        private static void OnEndUpdate()
        {
            if (s_HasChanges)
            {
                m_TreeView.Reload();
            }
            else
            {
                s_Context.Caretaker.OnGenerateMemento -= SetChanged;
            }
        }

        private void OnEnable()
        {
            titleContent.text = "Tiny Context";
        }

        private void OnGUI()
        {
            if (null == m_TreeView)
            {
                EditorGUILayout.LabelField("No project or module is currently loaded.");
                return;
            }
            m_Toolbar.DrawLayout();

            m_TreeView.SetFilter(m_Filter);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            try
            {
                m_TreeView.OnGUI(GUILayoutUtility.GetRect(0, m_TreeView.totalHeight));
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private static TinyToolbar CreateToolbar()
        {
            var toolbar = new TinyToolbar();

            toolbar.Add(new TinyToolbar.Search
            {
                Alignment = TinyToolbar.Alignment.Center,
                SearchString = m_TreeView.searchString,
                Changed = searchString =>
                {
                    m_TreeView.searchString = searchString;
                }
            });

            toolbar.Add(new TinyToolbar.Popup
            {
                Alignment = TinyToolbar.Alignment.Right,
                Name = "Filter",
                Content = new TinyToolbar.FilterPopup
                {
                    Items = CreateItems()
                }
            });

            return toolbar;
        }

        private static TinyToolbar.FilterPopup.Item[] CreateItems()
        {
            return ((RegistryTreeView.Filters[])Enum.GetValues(typeof(RegistryTreeView.Filters)))
                .Select(CreateItem)
                .ToArray();
        }

        private static TinyToolbar.FilterPopup.Item CreateItem(RegistryTreeView.Filters filter)
        {
            return new TinyToolbar.FilterPopup.Item
            {
                Name = filter.ToString(),
                State = m_Filter.HasFlag(filter) && filter > 0,
                Changed = state =>
                {
                    if (filter == RegistryTreeView.Filters.Nothing)
                    {
                        m_Filter = filter;
                    }
                    else if (filter == RegistryTreeView.Filters.Everything)
                    {
                        m_Filter = filter;
                    }
                    else if (state)
                    {
                        m_Filter |= filter;
                    }
                    else
                    {
                        m_Filter &= ~filter;
                    }
                    UpdateItems();
                    m_TreeView.Repaint();
                }
            };
        }

        private static void UpdateItems()
        {
            var content = m_Toolbar.Items.OfType<TinyToolbar.Popup>().First().Content as TinyToolbar.FilterPopup;

            if (m_Filter == RegistryTreeView.Filters.Nothing)
            {
                foreach (var item in content.Items)
                {
                    item.State = item.Name == RegistryTreeView.Filters.Nothing.ToString();
                }
            }
            else if (m_Filter == RegistryTreeView.Filters.Everything)
            {
                foreach (var item in content.Items)
                {
                    item.State = item.Name != RegistryTreeView.Filters.Nothing.ToString();
                }
            }
            else
            {
                foreach (var item in content.Items)
                {
                    if (item.Name == "Nothing")
                    {
                        item.State = false;
                    }
                    else
                    {
                        item.State = m_Filter.HasFlag((RegistryTreeView.Filters)Enum.Parse(typeof(RegistryTreeView.Filters), item.Name)) && m_Filter != RegistryTreeView.Filters.Nothing;
                    }
                }
            }
        }
    }
}
