
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using TypeRef = Unity.Tiny.TinyType.Reference;

namespace Unity.Tiny
{
    internal abstract class ComponentFamily : IPropertyContainer, IComponentFamily
    {
        private class ComponentSubSetList : ClassListClassProperty<ComponentFamily, TinyObject>
        {
            public ComponentSubSetList(string name, GetListMethod getList, CreateInstanceMethod createInstance = null)
                : base(name, getList, createInstance) { }
        }

        private static ComponentSubSetList Property = new ComponentSubSetList(
            "Family",
            c => c.Components);
        private static IPropertyBag s_PropertyBag = new PropertyBag(Property);

        public IVersionStorage VersionStorage { get; }
        public IPropertyBag PropertyBag => s_PropertyBag;

        protected TinyContext TinyContext { get; }

        private List<TinyObject> Components { get; }
        private List<TinyObject> RequiredComponents { get; }
        private List<TinyObject> OptionalComponents { get; }
        private HashSet<TinyId> HiddenComponents { get; }
        private IBindingsManager Bindings { get; }
        protected ICustomEditorManager CustomEditors { get; }

        public abstract string Name { get; }
        public FamilyDefinition Definition { get; }
        private bool VisitMethodIsOverriden { get; }

        public Color HeaderColor => VisitMethodIsOverriden
            ? new Color(0.0f ,0.0f, 0.0f, 0.0f)
            : TinyColors.Inspector.BoxBackground;

        public IEnumerable<TinyType.Reference> GetRequiredTypes()
        {
            foreach (var component in RequiredComponents)
            {
                yield return component.Type;
            }
        }

        public IEnumerable<TinyType.Reference> GetOptionalTypes()
        {
            foreach (var component in OptionalComponents)
            {
                yield return component.Type;
            }
        }

        public IEnumerable<TinyType.Reference> GetTypes()
        {
            foreach (var component in Components)
            {
                yield return component.Type;
            }
        }

        public bool Visit(ref UIVisitContext<TinyObject> context)
        {
            if (VisitMethodIsOverriden)
            {
                EditorGUILayout.BeginVertical();
                try
                {
                    return VisitFamilyComponent(ref context);
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
            }
            return VisitFamilyComponent(ref context);
        }

        protected virtual bool VisitFamilyComponent(ref UIVisitContext<TinyObject> context)
        {
            return context.Visitor.ContinueVisit;
        }

        public virtual void AddRequiredComponent(IEnumerable<TinyEntity> entities)
        {
            foreach (var entity in entities)
            {
                foreach (var required in Definition.Required)
                {
                    entity.GetOrAddComponent(required);
                }
            }
        }
        
        public void ResetValues(List<TinyEntity> targets)
        {
            ResetFamilyValues(targets);
            foreach (var target in targets)
            {
                Bindings.Transfer(target);
            }
        }

        protected virtual void ResetFamilyValues(List<TinyEntity> targets)
        {
            foreach (var type in GetTypes())
            {
                foreach (var entity in targets)
                {
                    var component = entity.GetComponent(type);
                    component.Reset();
                }
            }
        }

        public ComponentFamily(FamilyDefinition definition, TinyContext tinyContext)
        {
            Definition = definition;
            VersionStorage = null;
            TinyContext = tinyContext;
            Bindings = tinyContext.GetManager<IBindingsManager>();
            CustomEditors = tinyContext.GetManager<ICustomEditorManager>();
            Components = new List<TinyObject>();
            RequiredComponents = new List<TinyObject>();
            OptionalComponents = new List<TinyObject>();
            HiddenComponents = new HashSet<TinyId>();

            VisitMethodIsOverriden = GetType().GetMethod(nameof(VisitFamilyComponent), BindingFlags.NonPublic | BindingFlags.Instance).DeclaringType != typeof(ComponentFamily);
        }

        internal void MarkAsHidden(IEnumerable<TinyId> ids)
        {
            HiddenComponents.Clear();
            HiddenComponents.UnionWith(ids);
        }

        public bool SkipComponent(TypeRef typeRef)
        {
            return HiddenComponents.Contains(typeRef.Id);
        }

        public bool Refresh(TinyEntity entity)
        {
            RequiredComponents.Clear();
            RequiredComponents.AddRange(Definition.Required.Select(entity.GetComponent).NotNull());
            if (Definition.Required.Length != RequiredComponents.Count)
            {
                return false;
            }

            OptionalComponents.Clear();
            OptionalComponents.AddRange(Definition.Optional.Select(entity.GetComponent).NotNull());

            Components.Clear();
            Components.AddRange(RequiredComponents);
            Components.AddRange(OptionalComponents);
            return true;
        }
    }
}
