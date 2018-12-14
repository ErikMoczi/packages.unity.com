
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace Unity.Tiny
{
    internal sealed class GUIVisitor : IPropertyVisitor
    {
        #region Fields

        private readonly ICustomUIAdapter[] m_Adapters;
        private readonly Dictionary<TypePairKey, ICustomUIAdapter> m_VisitUIAdapter = new Dictionary<TypePairKey, ICustomUIAdapter>();
        private readonly Dictionary<TypePairKey, ICustomUIAdapter> m_ExcludeUIAdapter = new Dictionary<TypePairKey, ICustomUIAdapter>();
        private readonly Dictionary<TypePairKey, ICustomUIAdapter> m_ContainerUIAdapter = new Dictionary<TypePairKey, ICustomUIAdapter>();
        private readonly Dictionary<TypePairKey, ICustomUIAdapter> m_CollectionUIAdapter = new Dictionary<TypePairKey, ICustomUIAdapter>();
        private readonly IGUIVisitorStateTracker m_StateTracker;
        public readonly IGUIChangeTracker ChangeTracker;

        public int RemoveAtIndex = -1;

        #endregion // Fields

        #region Properties

        private List<IPropertyContainer> Targets { get; set; }
        private List<IPropertyContainer> UnwrappedTargets { get; set; }

        public Dictionary<IPropertyContainer, bool> FolderCache { get; } = new Dictionary<IPropertyContainer, bool>();
        public INameResolver NameResolver { get; set; }

        public readonly bool ContinueVisit = false;
        public readonly bool StopVisit = true;

        #endregion // Properties

        #region API
        public GUIVisitor(params ICustomUIAdapter[] adapters)
        {
            m_Adapters = adapters ?? new ICustomUIAdapter[0];
            ChangeTracker = new DefaultGUIChangeTracker();
            m_StateTracker = new IMGUIVisitorStateTracker();
            NameResolver = new DefaultNameResolver();
        }

        public void SetTargets(List<IPropertyContainer> targets)
        {
            SetTargetsImpl(targets);
            UnwrappedTargets = targets;
        }

        public void SetTargets<T>(List<Wrapper<T>> targets)
            where T : class, IPropertyContainer
        {
            SetTargetsImpl(targets.Cast<IPropertyContainer>().ToList());
            UnwrappedTargets = targets.Select(w => w.Value).Cast<IPropertyContainer>().ToList();
        }

        public void SetTargetsImpl(List<IPropertyContainer> targets)
        {
            Targets = targets;
            ChangeTracker.ClearResolvers();
            ChangeTracker.PushResolvers(Targets);
        }

        public void VisitTargets()
        {
            Targets[0].Visit(this);
        }

        // TODO: Add all the methods here as needed.
        public bool VisitValueClassProperty<TContainer, TValue>(TContainer container, string propertyName)
            where TContainer : class, IPropertyContainer
        {
            var property = container.PropertyBag.FindProperty(propertyName)as IValueClassProperty<TContainer, TValue>;
            property.Accept(container, this);
            return StopVisit;
        }

        public bool VisitValueStructProperty<TContainer, TValue>(TContainer container, string propertyName)
            where TContainer : struct, IPropertyContainer
        {
            var property = container.PropertyBag.FindProperty(propertyName)as IValueStructProperty<TContainer, TValue>;
            property?.Accept(ref container, this);
            return StopVisit;
        }

        public bool VisitContainer<TContainer, TValue>(TContainer container, string propertyName)
            where TContainer : class, IPropertyContainer
            where TValue : class, IPropertyContainer
        {
            var property = container.PropertyBag.FindProperty(propertyName) as ClassValueClassProperty<TContainer, TValue>;
            property?.Accept(container, this);
            return StopVisit;
        }

        public bool VisitList<TContainer, TValue>(TContainer container, string propertyName)
            where TContainer : class, IPropertyContainer
            where TValue : class, IPropertyContainer
        {
            var property = container.PropertyBag.FindProperty(propertyName) as ClassValueClassProperty<TContainer, TValue>;
            property?.Accept(container, this);
            return StopVisit;
        }

        #endregion // API

        #region IPropertyVisitor

        bool IPropertyVisitor.ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            var uiContext = CreateUIContext(context);
            switch (GetExcludeAdaptor<TContainer, TValue>())
            {
                case IExcludeAdapter<TContainer, TValue> fullyTyped:
                    return fullyTyped.ExcludeVisit(ref container, ref uiContext);
                case IExcludeAdapter<TContainer> containerTyped:
                    return containerTyped.ExcludeVisit(ref container, ref uiContext);
                case IClassExcludeAdapter<TValue> partiallyTyped:
                    return partiallyTyped.ExcludeClassVisit(ref container, ref uiContext);
                case IClassExcludeAdapter untyped:
                    return untyped.ExcludeClassVisit(ref container, ref uiContext);
                case null:
                    return ContinueVisit;
            }

            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: an {nameof(IPropertyVisitor.ExcludeVisit)} related interface is not handled properly.");
        }

        bool IPropertyVisitor.ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            var uiContext = CreateUIContext(context);
            switch (GetExcludeAdaptor<TContainer, TValue>())
            {
                case IExcludeAdapter<TContainer, TValue> fullyTyped:
                    return fullyTyped.ExcludeVisit(ref container, ref uiContext);
                case IExcludeAdapter<TContainer> containerTyped:
                    return containerTyped.ExcludeVisit(ref container, ref uiContext);
                case IStructExcludeAdapter<TValue> partiallyTyped:
                    return partiallyTyped.ExcludeStructVisit(ref container, ref uiContext);
                case IStructExcludeAdapter untyped:
                    return untyped.ExcludeStructVisit(ref container, ref uiContext);
                case null:
                    return ContinueVisit;
            }

            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: an {nameof(IPropertyVisitor.ExcludeVisit)} related interface is not handled properly.");
        }

        bool IPropertyVisitor.CustomVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            var uiContext = CreateUIContext(context);
            m_StateTracker.CacheState();
            var previous = uiContext.Value;

            try
            {
                switch (GetVisitAdaptor<TContainer, TValue>())
                {
                    case IVisitAdapter<TContainer, TValue> fullyTyped:
                        return fullyTyped.CustomVisit(ref container, ref uiContext);
                    case IVisitAdapter<TContainer> containerTyped:
                        return containerTyped.CustomVisit(ref container, ref uiContext);
                    case IClassVisitAdapter<TValue> partiallyTyped:
                        return partiallyTyped.CustomClassVisit(ref container, ref uiContext);
                    case IClassVisitAdapter untyped:
                        return untyped.CustomClassVisit(ref container, ref uiContext);
                    case IClassUnsupportedAdapter unsupported:
                        return unsupported.UnsupportedClass(ref container, ref uiContext);
                    case null:
                        return ContinueVisit;
                }

                throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.CustomVisit)} related interface is not handled properly.");
            }
            finally
            {
                m_StateTracker.RestoreState();

                var next = uiContext.Value;
                var property = uiContext.Property;

                // Handle list elements
                if (context.Property is IListProperty && context.Index >= 0)
                {
                    if (ChangeTracker.ValuesAreDifferent(previous, next))
                    {
                        var typedProperty = (IListClassProperty<TContainer, TValue>)property;
                        typedProperty.SetAt(container, context.Index, next);
                        ChangeTracker.PushChange(container, property);
                    }
                }
                else
                {
                    var isReadOnly = (property as IValueProperty)?.IsReadOnly ?? false;

                    if (ChangeTracker.ValuesAreDifferent(previous, next) && !isReadOnly)
                    {
                        var typedProperty = (IValueClassProperty<TContainer, TValue>)property;
                        typedProperty.SetValue(container, next);
                        ChangeTracker.PushChange(container, property);
                    }
                }
            }
        }

        bool IPropertyVisitor.CustomVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            var uiContext = CreateUIContext(context);
            m_StateTracker.CacheState();
            var previous = uiContext.Value;

            try
            {
                switch (GetVisitAdaptor<TContainer, TValue>())
                {
                    case IVisitAdapter<TContainer, TValue> fullyTyped:
                        return fullyTyped.CustomVisit(ref container, ref uiContext);
                    case IVisitAdapter<TContainer> containerTyped:
                        return containerTyped.CustomVisit(ref container, ref uiContext);
                    case IStructVisitAdapter<TValue> partiallyTyped:
                        return partiallyTyped.CustomStructVisit(ref container, ref uiContext);
                    case IStructVisitAdapter untyped:
                        return untyped.CustomStructVisit(ref container, ref uiContext);
                    case IStructUnsupportedAdapter unsupported:
                        return unsupported.UnsupportedStruct(ref container, ref uiContext);
                    case null:
                        return ContinueVisit;
                }

                throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.CustomVisit)} related interface is not handled properly.");
            }
            finally
            {
                m_StateTracker.RestoreState();

                var next = uiContext.Value;
                var property = uiContext.Property;

                // Handle list elements
                if (uiContext.IsListItem)
                {
                    if (ChangeTracker.ValuesAreDifferent(previous, next))
                    {
                        var typedProperty = (IListStructProperty<TContainer, TValue>)property;
                        typedProperty.SetAt(ref container, context.Index, next);
                        ChangeTracker.PushChange(container, property);
                    }
                }
                else
                {
                    var isReadOnly = (property as IValueProperty)?.IsReadOnly ?? false;

                    if (ChangeTracker.ValuesAreDifferent(previous, next) && !isReadOnly)
                    {
                        var typedProperty = (IValueStructProperty<TContainer, TValue>)property;
                        typedProperty.SetValue(ref container, next);
                        ChangeTracker.PushChange(container, property);
                    }
                }
            }
        }

        void IPropertyVisitor.Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            // Nothing should be done here.
        }

        void IPropertyVisitor.Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            // Nothing should be done here.
        }

        bool IPropertyVisitor.BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            var uiContext = CreateUIContext(context);

            switch (GetContainerAdaptor<TContainer, TValue>())
            {
                case IContainerAdapter<TContainer, TValue> fullyTyped:
                    return fullyTyped.BeginContainer(ref container, ref uiContext);
                case IContainerAdapter<TContainer> containerTyped:
                    return containerTyped.BeginContainer(ref container, ref uiContext);
                case IClassContainerAdapter<TValue> partiallyTyped:
                    return partiallyTyped.BeginClassContainer(ref container, ref uiContext);
                case IClassContainerAdapter untyped:
                    return untyped.BeginClassContainer(ref container, ref uiContext);
                case null:
                    return StopVisit;
            }
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.BeginContainer)} related interface is not handled properly.");
        }

        bool IPropertyVisitor.BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            var uiContext = CreateUIContext(context);

            switch (GetContainerAdaptor<TContainer, TValue>())
            {
                case IContainerAdapter<TContainer, TValue> fullyTyped:
                    return fullyTyped.BeginContainer(ref container, ref uiContext);
                case IContainerAdapter<TContainer> containerTyped:
                    return containerTyped.BeginContainer(ref container, ref uiContext);
                case IStructContainerAdapter<TValue> partiallyTyped:
                    return partiallyTyped.BeginStructContainer(ref container, ref uiContext);
                case IStructContainerAdapter untyped:
                    return untyped.BeginStructContainer(ref container, ref uiContext);
                case null:
                    return StopVisit;
            }
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.BeginContainer)} related interface is not handled properly.");
        }

        void IPropertyVisitor.EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            var uiContext = CreateUIContext(context);

            switch (GetContainerAdaptor<TContainer, TValue>())
            {
                case IContainerAdapter<TContainer, TValue> fullyTyped:
                    fullyTyped.EndContainer(ref container, ref uiContext);
                    return;
                case IContainerAdapter<TContainer> containerTyped:
                    containerTyped.EndContainer(ref container, ref uiContext);
                    return;
                case IClassContainerAdapter<TValue> partiallyTyped:
                    partiallyTyped.EndClassContainer(ref container, ref uiContext);
                    return;
                case IClassContainerAdapter untyped:
                    untyped.EndClassContainer(ref container, ref uiContext);
                    return;
                case null:
                    return;
            }
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.EndContainer)} related interface is not handled properly.");
        }

        void IPropertyVisitor.EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            var uiContext = CreateUIContext(context);
            switch (GetContainerAdaptor<TContainer, TValue>())
            {
                case IContainerAdapter<TContainer, TValue> fullyTyped:
                    fullyTyped.EndContainer(ref container, ref uiContext);
                    return;
                case IContainerAdapter<TContainer> containerTyped:
                    containerTyped.EndContainer(ref container, ref uiContext);
                    return;
                case IStructContainerAdapter<TValue> partiallyTyped:
                    partiallyTyped.EndStructContainer(ref container, ref uiContext);
                    return;
                case IStructContainerAdapter untyped:
                    untyped.EndStructContainer(ref container, ref uiContext);
                    return;
                case null:
                    return;
            }
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.EndContainer)} related interface is not handled properly.");
        }

        bool IPropertyVisitor.BeginCollection<TContainer, TValue>(TContainer container, VisitContext<IList<TValue>> context)
        {
            m_StateTracker.CacheState();
            var uiContext = CreateUIContext(context);
            switch (GetCollectionAdaptor<TContainer, TValue>()) {
                case ICollectionAdapter<TContainer, TValue> fullyTyped:
                    return fullyTyped.BeginCollection(ref container, ref uiContext);
                case ICollectionAdapter<TContainer> containerTyped:
                    return containerTyped.BeginCollection(ref container, ref uiContext);
                case IClassCollectionAdapter<TValue> partiallyTyped:
                    return partiallyTyped.BeginClassCollection(ref container, ref uiContext);
                case IClassCollectionAdapter untyped:
                    return untyped.BeginClassCollection(container, ref uiContext);
                case null :
                    return StopVisit;
            }
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.BeginCollection)} related interface is not handled properly.");
        }

        bool IPropertyVisitor.BeginCollection<TContainer, TValue>(ref TContainer container, VisitContext<IList<TValue>> context)
        {
            m_StateTracker.CacheState();
            var uiContext = CreateUIContext(context);
            switch (GetCollectionAdaptor<TContainer, TValue>())
            {
                case ICollectionAdapter<TContainer, TValue> fullyTyped:
                    return fullyTyped.BeginCollection(ref container, ref uiContext);
                case ICollectionAdapter<TContainer> containerTyped:
                    return containerTyped.BeginCollection(ref container, ref uiContext);
                case IStructCollectionAdapter<TValue> partiallyTyped:
                    return partiallyTyped.BeginStructCollection(ref container, ref uiContext);
                case IStructCollectionAdapter untyped:
                    return untyped.BeginStructCollection(container, ref uiContext);
                case null:
                    return StopVisit;
            }
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.BeginCollection)} related interface is not handled properly.");
        }

        void IPropertyVisitor.EndCollection<TContainer, TValue>(TContainer container, VisitContext<IList<TValue>> context)
        {
            var uiContext = CreateUIContext(context);
            try
            {
                switch (GetCollectionAdaptor<TContainer, TValue>())
                {
                    case ICollectionAdapter<TContainer, TValue> fullyTyped:
                        fullyTyped.EndCollection(ref container, ref uiContext);
                        return;
                    case ICollectionAdapter<TContainer> containerTyped:
                        containerTyped.EndCollection(ref container, ref uiContext);
                        return;
                    case IClassCollectionAdapter<TValue> partiallyTyped:
                        partiallyTyped.EndClassCollection(ref container, ref uiContext);
                        return;
                    case IClassCollectionAdapter untyped:
                        untyped.EndClassCollection(container, ref uiContext);
                        return;
                    case null:
                        return;
                }
                throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.EndCollection)} related interface is not handled properly.");
            }
            finally
            {
                if (RemoveAtIndex >= 0)
                {
                    if (context.Property is IListClassProperty<TContainer, TValue> list)
                    {
                        list.RemoveAt(container, RemoveAtIndex);
                        RemoveAtIndex = -1;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                m_StateTracker.RestoreState();
            }
        }

        void IPropertyVisitor.EndCollection<TContainer, TValue>(ref TContainer container, VisitContext<IList<TValue>> context)
        {
            var uiContext = CreateUIContext(context);
            try
            {
                switch (GetCollectionAdaptor<TContainer, TValue>())
                {
                    case ICollectionAdapter<TContainer, TValue> fullyTyped:
                        fullyTyped.EndCollection(ref container, ref uiContext);
                        return;
                    case ICollectionAdapter<TContainer> containerTyped:
                        containerTyped.EndCollection(ref container, ref uiContext);
                        return;
                    case IStructCollectionAdapter<TValue> partiallyTyped:
                        partiallyTyped.EndStructCollection(ref container, ref uiContext);
                        return;
                    case IStructCollectionAdapter untyped:
                        untyped.EndStructCollection(container, ref uiContext);
                        return;
                    case null:
                        return;
                }
                throw new InvalidOperationException($"{TinyConstants.ApplicationName}: a {nameof(IPropertyVisitor.EndCollection)} related interface is not handled properly.");
            }
            finally
            {
                if (RemoveAtIndex >= 0)
                {
                    if (context.Property is IListStructProperty<TContainer, TValue> list)
                    {
                        list.RemoveAt(ref container, RemoveAtIndex);
                        RemoveAtIndex = -1;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                m_StateTracker.RestoreState();
            }
        }

        #endregion // IPropertyVisitor

        #region Implementation

        private UIVisitContext<TValue> CreateUIContext<TValue>(VisitContext<TValue> originalContext)
        {
            return new UIVisitContext<TValue>(
                originalContext,
                this,
                UnwrappedTargets);
        }

        private ICustomUIAdapter GetExcludeAdaptor<TContainer, TValue>()
            where TContainer : IPropertyContainer
        {
            // Fast path, we already know the adapter for this combination of types.
            var key = TypePairKey.Make<TContainer, TValue>();
            if (m_ExcludeUIAdapter.TryGetValue(key, out var adapter))
            {
                return adapter;
            }

            if (TryCache<IExcludeAdapter<TContainer, TValue>>(m_ExcludeUIAdapter, key)  ||
                TryCache<IExcludeAdapter<TContainer>>(m_ExcludeUIAdapter, key) ||
                TryCache<IClassExcludeAdapter<TValue>>(m_ExcludeUIAdapter, key)         ||
                TryCache<IClassExcludeAdapter>(m_ExcludeUIAdapter, key)                 ||
                TryCache<IStructExcludeAdapter<TValue>>(m_ExcludeUIAdapter, key)        ||
                TryCache<IStructExcludeAdapter>(m_ExcludeUIAdapter, key))
            {
                return m_ExcludeUIAdapter[key];
            }
            return m_ExcludeUIAdapter[key] = null;
        }

        private ICustomUIAdapter GetVisitAdaptor<TContainer, TValue>()
            where TContainer : IPropertyContainer
        {
            // Fast path, we already know the adapter for this combination of types.
            var key = TypePairKey.Make<TContainer, TValue>();
            if (m_VisitUIAdapter.TryGetValue(key, out var adapter))
            {
                return adapter;
            }

            if (TryCache<IVisitAdapter<TContainer, TValue>>(m_VisitUIAdapter, key)  ||
                TryCache<IVisitAdapter<TContainer>>(m_VisitUIAdapter, key) ||
                TryCache<IClassVisitAdapter<TValue>>(m_VisitUIAdapter, key)         ||
                TryCache<IClassVisitAdapter>(m_VisitUIAdapter, key)                 ||
                TryCache<IClassUnsupportedAdapter>(m_VisitUIAdapter, key)           ||
                TryCache<IStructVisitAdapter>(m_VisitUIAdapter, key)                ||
                TryCache<IStructUnsupportedAdapter>(m_VisitUIAdapter, key))
            {
                return m_VisitUIAdapter[key];
            }
            return m_VisitUIAdapter[key] = null;
        }

        private ICustomUIAdapter GetContainerAdaptor<TContainer, TValue>()
            where TContainer : IPropertyContainer
        {
            // Fast path, we already know the adapter for this combination of types.
            var key = TypePairKey.Make<TContainer, TValue>();
            if (m_ContainerUIAdapter.TryGetValue(key, out var adapter))
            {
                return adapter;
            }

            if (TryCache<IContainerAdapter<TContainer, TValue>>(m_ContainerUIAdapter, key) ||
                TryCache<IContainerAdapter<TContainer>>(m_ContainerUIAdapter, key)         ||
                TryCache<IClassContainerAdapter<TValue>>(m_ContainerUIAdapter, key)        ||
                TryCache<IClassContainerAdapter>(m_ContainerUIAdapter, key)         ||
                TryCache<IStructContainerAdapter<TValue>>(m_ContainerUIAdapter, key)       ||
                TryCache<IStructContainerAdapter>(m_ContainerUIAdapter, key))
            {
                return m_ContainerUIAdapter[key];
            }
            return m_ContainerUIAdapter[key] = null;
        }

        private ICustomUIAdapter GetCollectionAdaptor<TContainer, TValue>()
            where TContainer : IPropertyContainer
        {
            // Fast path, we already know the adapter for this combination of types.
            var key = TypePairKey.Make<TContainer, TValue>();
            if (m_CollectionUIAdapter.TryGetValue(key, out var adapter))
            {
                return adapter;
            }

            if (TryCache<ICollectionAdapter<TContainer, TValue>>(m_CollectionUIAdapter, key)  ||
                TryCache<ICollectionAdapter<TContainer>>(m_CollectionUIAdapter, key) ||
                TryCache<IClassCollectionAdapter<TValue>>(m_CollectionUIAdapter, key)         ||
                TryCache<IClassCollectionAdapter>(m_CollectionUIAdapter, key)                 ||
                TryCache<IStructCollectionAdapter<TValue>>(m_CollectionUIAdapter, key)        ||
                TryCache<IStructCollectionAdapter>(m_CollectionUIAdapter, key))
            {
                return m_CollectionUIAdapter[key];
            }
            return m_CollectionUIAdapter[key] = null;
        }

        private bool TryCache<T>(IDictionary<TypePairKey, ICustomUIAdapter> dict, TypePairKey key)
            where T: ICustomUIAdapter
        {
            if (m_Adapters.OfType<T>().FirstOrDefault() is T adapter)
            {
                dict[key] = adapter;
                return true;
            }

            return false;
        }
        #endregion // Implementation
    }
}
