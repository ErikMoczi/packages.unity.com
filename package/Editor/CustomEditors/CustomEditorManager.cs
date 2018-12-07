
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;

namespace Unity.Tiny
{
    internal interface ICustomEditorManager : IContextManager
    {
        IComponentEditor GetEditor(TinyType.Reference typeRef);
        IStructDrawer GetDrawer(TinyType.Reference typeRef);
    }

    internal interface ICustomEditorManagerInternal : ICustomEditorManager
    {
        InspectorMode Mode { get; set; }
    }

    [ContextManager(ContextUsage.Edit | ContextUsage.LiveLink), UsedImplicitly]
    internal class CustomEditorManager : ContextManager, ICustomEditorManagerInternal
    {
        private readonly Dictionary<TinyId, IComponentEditor> m_CustomEditors = new Dictionary<TinyId, IComponentEditor>();
        private IComponentEditor m_DefaultEditor;
        private readonly Dictionary<TinyId, IStructDrawer> m_CustomDrawers = new Dictionary<TinyId, IStructDrawer>();
        private IStructDrawer m_DefaultDrawer;

        public InspectorMode Mode { get; set; } = InspectorMode.Normal;

        public CustomEditorManager(TinyContext context)
            : base(context)
        {
            m_DefaultEditor = ComponentEditor.CreateDefault(context);
            m_DefaultDrawer = StructDrawer.CreateDefault(context);
        }

        public IComponentEditor GetEditor(TinyType.Reference typeRef)
        {
            if (Mode != InspectorMode.Normal)
            {
                return m_DefaultEditor;
            }
            return m_CustomEditors.TryGetValue(typeRef.Id, out var editor) ? editor: m_DefaultEditor;
        }

        public IStructDrawer GetDrawer(TinyType.Reference typeRef)
        {
            if (Mode != InspectorMode.Normal)
            {
                return m_DefaultDrawer;
            }
            return m_CustomDrawers.TryGetValue(typeRef.Id, out var drawer) ? drawer : m_DefaultDrawer;
        }

        public override void Load()
        {
            Register<IComponentEditor, ComponentEditor, TinyCustomEditorAttribute>(m_CustomEditors);
            Register<IStructDrawer, StructDrawer, TinyCustomDrawerAttribute>(m_CustomDrawers);
        }

        private void Register<TIEditorType, TBaseEditorType, TAttribute>(IDictionary<TinyId, TIEditorType> dict)
            where TBaseEditorType : TIEditorType
            where TAttribute : TinyAttribute, IIdentified<TinyId>
        {
            foreach (var typeAttribute in TinyAttributeScanner.GetTypeAttributes<TAttribute>())
            {
                var type = typeAttribute.Type;
                var attribute = typeAttribute.Attribute;

                if (!type.IsSubclassOf(typeof(TBaseEditorType)))
                {
                    continue;
                }

                dict[attribute.Id] = (TIEditorType)Activator.CreateInstance(type, Context);
            }
        }

    }
}
