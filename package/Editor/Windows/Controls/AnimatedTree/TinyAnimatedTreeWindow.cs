

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

namespace Unity.Tiny
{
    internal abstract class TinyAnimatedTreeWindow<TDerived, TValue> : EditorWindow
        where TDerived : TinyAnimatedTreeWindow<TDerived, TValue>
        where TValue : class, IRegistryObject
    {
        #region Static
        private static TDerived s_Window = null;
        private static long s_LastClosedTime;
        #endregion

        #region Constants
        private const int kWindowHeight = 320;
        #endregion

        #region Fields
        private TinyAnimatedTree m_AnimatedTree;
        protected IRegistry Registry;
        protected TinyModule MainModule;
        protected HashSet<TinyModule> IncludedModules;
        protected Dictionary<TValue, TinyModule> ValueToModules = new Dictionary<TValue, TinyModule>();
        protected List<TValue> m_AvailableComponentTypes;
        #endregion

        #region Properties
        protected TDerived Window => s_Window;
        protected TinyAnimatedTree Tree => m_AnimatedTree;
        #endregion

        #region Unity
        void OnDisable()
        {
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_Window = null;
        }

        private void OnGUI()
        {
            wantsMouseMove = true;
            m_AnimatedTree.OnGUI(position);
        }

        #endregion

        #region API
        protected static TDerived GetWindow()
        {
            if (null != s_Window)
            {
                return s_Window;
            }
            // If the window is already open, close it instead.
            Object[] wins = Resources.FindObjectsOfTypeAll(typeof(TDerived));
            if (wins.Length > 0)
            {
                foreach (var win in wins)
                {
                    if (null != win)
                    {
                        ((EditorWindow)win)?.Close();
                    }
                }
                return null;
            }

            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exiting play mode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_Window == null)
                    s_Window = CreateInstance<TDerived>();
            }

            return s_Window;
        }

        protected static bool Show(Rect rect, IRegistry registry, bool showNotIncludedModules = false)
        {
            var window = GetWindow();
            window?.Init(rect, registry, showNotIncludedModules);
            return null != window;
        }

        protected bool IsIncluded(TinyModule module)
        {
            return IncludedModules.Contains(module);
        }

        protected bool IsIncluded(TValue value)
        {
            return IsIncluded((ValueToModules[value]));
        }
        #endregion

        #region Implementation
        private void Init(Rect buttonRect, IRegistry registry, bool showNotIncludedModules)
        {
            Registry = registry;
            if (null == Registry)
            {
                return;
            }
            CreateComponentTree(showNotIncludedModules);
            buttonRect = GUIToScreenRect(buttonRect);
            s_Window.ShowAsDropDown(buttonRect, new Vector2(Mathf.Max(buttonRect.width, 200.0f), kWindowHeight));
        }

        internal static Rect GUIToScreenRect(Rect guiRect)
        {
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.x, guiRect.y));
            guiRect.x = screenPoint.x;
            guiRect.y = screenPoint.y;
            return guiRect;
        }

        protected string MakeTooltip(IDocumented documented, string warning = null)
        {
            var summary = documented.Documentation.Summary;
            if (string.IsNullOrEmpty(warning))
            {
                return summary;
            }

            if (string.IsNullOrEmpty(summary))
            {
                return warning;
            }

            return $"{summary}\n{warning}";
        }

        private void CreateComponentTree(bool showNotIncludedModules)
        {
            m_AnimatedTree = new TinyAnimatedTree(TreeName());
            m_AnimatedTree.OnEscapePressed += CloseWindow;
            m_AnimatedTree.OnStateChanged += Repaint;

            m_AnimatedTree.OnAnyLeafElementClicked += elem =>
            {
                CloseWindow();
            };

            var project = Registry.FindAllByType<TinyProject>().FirstOrDefault();
            MainModule = project.Module.Dereference(Registry);
            IncludedModules = new HashSet<TinyModule>(project.Module.Dereference(Registry).EnumerateDependencies());

            OnBeforePopulateMenu();

            foreach (var module in Registry.FindAllByType<TinyModule>().OrderBy(m => (m.Name == TinyProject.MainProjectName ? "" : m.Name)))
            {
                var included = IncludedModules.Contains(module);

                if (!showNotIncludedModules && !included)
                {
                    continue;
                }

                var allComponentTypes = GetItems(module);
                if (!allComponentTypes.Any())
                {
                    continue;
                }

                var element = TinyAnimatedTree.Element.MakeGroup(module.Name == TinyProject.MainProjectName && null != project ? project.Name : module.Name, MakeTooltip(module), included);
                string warning = null;
                if (!included)
                {
                    var count = module.EnumerateDependencies().Count(m => !IncludedModules.Contains(m));
                    warning =
                        $"This will include the {module.Name} module{(count <= 1 ? "" : $", along with {count - 1} dependencies.")}.";
                    element.Add(TinyAnimatedTree.Element.MakeWarning(warning));
                }
                foreach (var type in allComponentTypes)
                {
                    ValueToModules.Add(type, module);

                    if (FilterItem(type))
                    {
                        var typeName = type.Name;
                        element.Add(TinyAnimatedTree.Element.MakeLeaf(typeName, MakeTooltip(type, warning), included, () =>
                        {
                            IncludeMissingModules(type);
                            OnItemClicked(type);
                        }));
                    }
                }

                // We do not want to show modules that have no more component to add.
                if (!element.IsLeaf)
                {
                    element.SortChildren();
                    m_AnimatedTree.Add(element);
                }
            }

            OnAfterPopulateMenu();

            m_AvailableComponentTypes = Registry.FindAllByType<TValue>().ToList();
        }
        
        protected abstract IEnumerable<TValue> GetItems(TinyModule module);
        protected abstract void OnItemClicked(TValue value);
        protected virtual void OnBeforePopulateMenu() { }
        protected virtual void OnAfterPopulateMenu() { }
        protected virtual bool FilterItem(TValue value) { return true; }
        protected abstract string TreeName();

        private void CloseWindow()
        {
            GUIUtility.keyboardControl = 0;
            Close();
        }

        private void IncludeMissingModules(TValue item)
        {
            var module = ValueToModules[item];
            
            if (!IsIncluded(module))
            {
                Debug.Log($"{TinyConstants.ApplicationName}: The '{module.Name}' module was included to the project because the '{item.Name}' component was added to an entity.");
            }

            MainModule.AddExplicitModuleDependency((TinyModule.Reference)module);
        }
        #endregion
    }
}

