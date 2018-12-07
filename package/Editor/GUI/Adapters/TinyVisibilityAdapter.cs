
using Unity.Properties;

namespace Unity.Tiny
{
    internal class TinyVisibilityAdapter : TinyAdapter
        ,IGenericExcludeAdapter
        ,IExcludeAdapter<TinyObject>
        ,IExcludeAdapter<TinyObject.PropertiesContainer>
        ,IExcludeAdapter<TinyEntity, TinyObject>
    {
        public TinyVisibilityAdapter(TinyContext tinyContext)
            : base(tinyContext) { }

        #region IExcludeGenericUIAdapter

        public bool ExcludeClassVisit<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
            => HasHideInInspectorAttribute(ref container, ref context);

        public bool ExcludeStructVisit<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
            => HasHideInInspectorAttribute(ref container, ref context);

        #endregion // IExcludeGenericUIAdapter

        #region IExcludeContainerUIAdapter<TinyObject>

        public bool ExcludeVisit<TValue>(ref TinyObject container, ref UIVisitContext<TValue> context)
        {
            var type = container.Type.Dereference(TinyContext.Registry);
            return HasHideInInspectorAttribute(ref container, ref context) || HasHiddenVisibility(type.FindFieldByName(context.Label));
        }

        #endregion // IExcludeContainerUIAdapter<TinyObject>

        #region IExcludeContainerUIAdapter<TinyObject.PropertiesContainer>

        public bool ExcludeVisit<TValue>(ref TinyObject.PropertiesContainer container, ref UIVisitContext<TValue> context)
        {
            var type = container.ParentObject.Type.Dereference(TinyContext.Registry);
            return HasHideInInspectorAttribute(ref container, ref context) || HasHiddenVisibility(type.FindFieldByName(context.Label));
        }

        #endregion // IExcludeContainerUIAdapter<TinyObject.PropertiesContainer>

        #region IExcludeContainerUIAdapter<TinyEntity>

        public bool ExcludeVisit(ref TinyEntity container, ref UIVisitContext<TinyObject> context)
        {
            var type = context.Value.Type.Dereference(TinyContext.Registry);

            if (null == type)
            {
                return false;
            }
            
            if (type.IsComponent)
            {
                return type.Visibility == TinyVisibility.HideInInspector;
            }
            return false;
        }

        #endregion // IExcludeContainerUIAdapter<TinyObject>

        #region Implementation

        private static bool HasHideInInspectorAttribute<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            => context.Property.HasAttribute<InspectorAttributes.HideInInspectorAttribute>();

        private static bool HasHiddenVisibility(TinyField field)
        {
            if (null == field)
            {
                return false;
            }

            return field.Visibility == TinyVisibility.HideInInspector;
        }

        #endregion // Implementation

    }
}
