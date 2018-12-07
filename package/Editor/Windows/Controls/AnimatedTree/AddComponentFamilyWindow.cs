
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Unity.Tiny
{
    internal class AddComponentFamilyWindow : TinyAnimatedTreeWindow<AddComponentFamilyWindow, TinyType>
    {
        private TinyEntity[] Entities { get; set; }
        private List<ComponentFamily> CurrentFamilies { get; set; }
        private readonly List<TinyType.Reference> RequiredComponents = new List<TinyType.Reference>();
        private static TinyType.Reference[] Empty = new TinyType.Reference[0];

        public static bool Show(Rect rect, IRegistry registry, List<ComponentFamily> families, TinyEntity[] entities)
        {
            var window = GetWindow();
            if (null == window)
            {
                return false;
            }
            window.Entities = entities;
            window.CurrentFamilies = families;
            return Show(rect, registry, true);
        }

        protected override IEnumerable<TinyType> GetItems(TinyModule module)
        {
            return module.Components.Deref(Registry);
        }

        private static void Fill(List<TinyType.Reference> buffer, IEnumerable<TinyType.Reference> fillings)
        {
            buffer.Clear();
            buffer.AddRange(fillings ?? Empty);
        }

        protected override void OnBeforePopulateMenu()
        {
            var familiesItem = TinyAnimatedTree.Element.MakeGroup("Component Families", "", true);
            var allFamilies = TinyEditorApplication.EditorContext.Context.GetManager<FamilyManager>().AllFamilies;

            foreach (var family in allFamilies)
            {
                var entityFamily = CurrentFamilies.Find(c => c == family);
                Fill(RequiredComponents, entityFamily?.GetRequiredTypes());
                if (RequiredComponents.Count < family.Definition.Required.Length)
                {
                    var f = family;
                    var name = f.Name + " Family";
                    familiesItem.Add(TinyAnimatedTree.Element.MakeLeaf(new TinyAnimatedTree.Element.Args
                    {
                        Name = name,
                        Tooltip = MakeFamilyTooltip(f),
                        Included = true,
                        OnClick = () =>
                        {
                            OnFamilyClicked(f);
                        },
                        Searchable = false
                    }));
                }
            }

            if (familiesItem.Children.Count > 0)
            {
                Tree.Add(familiesItem);
            }
        }

        protected override void OnAfterPopulateMenu()
        {
            var cacheManager = Registry.Context.GetManager<ICacheManagerInternal>();
            var project = Registry.AnyByType<TinyProject>();
            
            var allFamilies = TinyEditorApplication.EditorContext.Context.GetManager<FamilyManager>().AllFamilies;

            foreach (var family in allFamilies)
            {
                var entityFamily = CurrentFamilies.Find(c => c == family);
                Fill(RequiredComponents, entityFamily?.GetRequiredTypes());
                if (RequiredComponents.Count < family.Definition.Required.Length)
                {
                    var f = family;
                    var name = f.Name + " Family";
                    var typeRef = family.Definition.Required[0];
                    var module = cacheManager.GetModuleOf(typeRef);
                    var included = IsIncluded(module);

                    var groupName = module.Name == TinyProject.MainProjectName && null != project
                        ? project.Name
                        : module.Name;
                    var group = Tree.GetGroup(groupName);

                    group?.Add(TinyAnimatedTree.Element.MakeLeaf(new TinyAnimatedTree.Element.Args
                    {
                        Name = name,
                        Tooltip = MakeFamilyTooltip(f),
                        Included = included,
                        OnClick = () => { OnFamilyClicked(f); },
                        Searchable = false
                    }));
                    group?.SortChildren();
                }
            }
        }

        protected override void OnItemClicked(TinyType type)
        {
            Add(type.Ref);

            // This is called manually because we want the scene graphs to be recreated.
            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
        }

        private void OnFamilyClicked(ComponentFamily family)
        {
            family.AddRequiredComponent(Entities);

            var definition = family.Definition;
            var set = HashSetPool<TinyModule>.Get();
            try
            {
                foreach (var typeRef in definition.Required)
                {
                    Add(typeRef, set);
                }
            }
            finally
            {
                HashSetPool<TinyModule>.Release(set);
            }

            // This is called manually because we want the scene graphs to be recreated.
            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
        }

        private void Add(TinyType.Reference typeRef, HashSet<TinyModule> included = null)
        {
            var requirements = Registry.Context.GetManager<ComponentRequirementsManager>();
            var type = typeRef.Dereference(Registry);
            var module = ValueToModules[type];
            foreach (var entity in Entities)
            {
                var component = entity.GetOrAddComponent(typeRef);
                component.Refresh();
                requirements.AddRequiredComponent(entity, typeRef);
            }

            if (!IsIncluded(module))
            {
                if (null != included && included.Add(module))
                {
                    Debug.Log(
                        $"{TinyConstants.ApplicationName}: The '{module.Name}' module was included to the project because the '{type.Name}' component was added to an entity.");
                }
            }
            MainModule.AddExplicitModuleDependency((TinyModule.Reference)module);
        }

        protected override bool FilterItem(TinyType type)
        {
            if (type.Unlisted)
            {
                return false;
            }

            var typeRef = (TinyType.Reference)type;
            foreach(var entity in Entities)
            {
                if (null == entity.GetComponent(typeRef))
                {
                    return true;
                }
            }
            return false;
        }

        protected override string TreeName()
        {
            return $"{TinyConstants.ApplicationName} Components";
        }

        private string MakeFamilyTooltip(ComponentFamily family)
        {
            var definition = family.Definition;
            var sb = new StringBuilder();
            sb.AppendLine(family.Name + " family:");
            AppendTypes(sb, "Core", definition.Required);
            if (definition.Optional.Length > 0)
            {
                AppendTypes(sb, "Optional", definition.Optional);
            }

            return sb.ToString();
        }

        private static void AppendTypes(StringBuilder sb, string sectionName, TinyType.Reference[] references)
        {
            sb.AppendLine($"  {sectionName}:");
            foreach (var reference in references)
            {
                sb.AppendLine($"    - {reference.Name}");
            }
        }
    }
}


