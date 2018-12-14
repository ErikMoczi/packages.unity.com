

using System.Collections.Generic;

using UnityEngine;

namespace Unity.Tiny
{
    internal class AddComponentWindow : TinyAnimatedTreeWindow<AddComponentWindow, TinyType>
    {
        private TinyEntity[] Entities { get; set; }

        public static bool Show(Rect rect, IRegistry registry, TinyEntity[] entities)
        {
            var window = GetWindow();
            if (null == window)
            {
                return false;
            }
            window.Entities = entities;
            return Show(rect, registry, true);
        }

        protected override IEnumerable<TinyType> GetItems(TinyModule module)
        {
            return module.Components.Deref(Registry);
        }

        protected override void OnItemClicked(TinyType type)
        {
            var requirements = type.Registry.Context.GetManager<ComponentRequirementsManager>();
            var module = ValueToModules[type];
            var typeRef = (TinyType.Reference)type;
            foreach (var entity in Entities)
            {
                var component = entity.GetOrAddComponent(typeRef);
                component.Refresh();
                requirements.AddRequiredComponent(entity, typeRef);
            }

            if (!IsIncluded(module))
            {
                Debug.Log($"{TinyConstants.ApplicationName}: The '{module.Name}' module was included to the project because the '{type.Name}' component was added to an entity.");
            }

            MainModule.AddExplicitModuleDependency((TinyModule.Reference)module);
            
            // This is called manually because we want the scene graphs to be recreated.
            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
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
    }
}

