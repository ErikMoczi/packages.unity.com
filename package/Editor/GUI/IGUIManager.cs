
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;

namespace Unity.Tiny
{
    internal interface IGUIManager : IContextManager
    {
        GUIVisitor GetVisitor(Type targetType, InspectorMode mode, bool includeComponentFamilies);
        GUIVisitor GetVisitor(Type targetType, InspectorMode mode);
    }

    internal interface IGUIManagerInternal : IGUIManager
    {

    }

    [ContextManager(ContextUsage.Edit | ContextUsage.LiveLink), UsedImplicitly]
    internal class GUIManager : ContextManager, IGUIManagerInternal
    {
        private struct ModeTypePair : IEquatable<ModeTypePair>
        {
            public InspectorMode Mode;
            public Type Type;

            public bool Equals(ModeTypePair other)
            {
                return Mode == other.Mode && Equals(Type, other.Type);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ModeTypePair other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Mode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                }
            }
        }

        private static readonly IMGUIUnityTypesAdapter k_UnityTypesAdapter = new IMGUIUnityTypesAdapter();
        private static readonly IMGUIPrimitivesAdapter k_PrimitiveAdapter = new IMGUIPrimitivesAdapter();
        private static readonly IMGUIAdapter k_IMGUIAdapter = new IMGUIAdapter();

        private Dictionary<ModeTypePair, GUIVisitor> m_AllVisitors = new Dictionary<ModeTypePair, GUIVisitor>();
        private GUIVisitor m_BareboneVisitor;
        private ConfigurationAdapter m_ConfigurationAdapter;
        private EntityAdapter m_EntityAdapter;
        private TinyIMGUIAdapter m_TinyAdapter;
        private TinyVisibilityAdapter m_VisibilityAdapter;
        private ComponentFamilyAdapter m_ComponentFamilyAdapter;
        private GUIVisitor m_ComponentFamiliesVisitor;

        public GUIManager(TinyContext context)
            : base(context)
        {
        }

        public override void Load()
        {
            m_ConfigurationAdapter = new ConfigurationAdapter(Context);
            m_EntityAdapter = new EntityAdapter(Context);
            m_TinyAdapter = new TinyIMGUIAdapter(Context);
            m_VisibilityAdapter = new TinyVisibilityAdapter(Context);
            m_ComponentFamilyAdapter = new ComponentFamilyAdapter(Context);
            GenerateAllVisitors();
        }

        private void GenerateAllVisitors()
        {
            var familyVisitor = new GUIVisitor(
                m_EntityAdapter,
                m_TinyAdapter,
                m_VisibilityAdapter,
                k_UnityTypesAdapter,
                k_PrimitiveAdapter,
                k_IMGUIAdapter);

            m_ComponentFamiliesVisitor = new GUIVisitor(
                m_ComponentFamilyAdapter,
                m_EntityAdapter,
                m_TinyAdapter,
                m_VisibilityAdapter,
                k_UnityTypesAdapter,
                k_PrimitiveAdapter,
                k_IMGUIAdapter);

            m_AllVisitors.Add(new ModeTypePair { Mode = InspectorMode.Normal, Type = typeof(TinyEntity) }, familyVisitor );

            var entityVisitor = new GUIVisitor(
                m_EntityAdapter,
                m_TinyAdapter,
                m_VisibilityAdapter,
                k_UnityTypesAdapter,
                k_PrimitiveAdapter,
                k_IMGUIAdapter);

            m_AllVisitors.Add(new ModeTypePair { Mode = InspectorMode.Debug, Type = typeof(TinyEntity) }, entityVisitor);

            var configurationVisitor = new GUIVisitor(
                m_ConfigurationAdapter,
                m_EntityAdapter,
                m_TinyAdapter,
                m_VisibilityAdapter,
                k_UnityTypesAdapter,
                k_PrimitiveAdapter,
                k_IMGUIAdapter);

            m_BareboneVisitor = new GUIVisitor(
                m_TinyAdapter,
                k_UnityTypesAdapter,
                k_PrimitiveAdapter,
                k_IMGUIAdapter);

            m_AllVisitors.Add(new ModeTypePair { Mode = InspectorMode.DebugInternal, Type = typeof(TinyEntity) }, m_BareboneVisitor);

            m_AllVisitors.Add(new ModeTypePair { Mode = InspectorMode.Normal, Type = typeof(TinyConfigurationViewer) }, configurationVisitor);
            m_AllVisitors.Add(new ModeTypePair { Mode = InspectorMode.Debug, Type = typeof(TinyConfigurationViewer) }, configurationVisitor);

            m_AllVisitors.Add(new ModeTypePair { Mode = InspectorMode.DebugInternal, Type = typeof(TinyConfigurationViewer) }, new GUIVisitor(
                m_ConfigurationAdapter,
                m_TinyAdapter,
                k_UnityTypesAdapter,
                k_PrimitiveAdapter,
                k_IMGUIAdapter));
        }

        public GUIVisitor GetVisitor(Type targetType, InspectorMode mode, bool includeComponentFamilies)
        {
            // HACK
            if (mode == InspectorMode.Normal && includeComponentFamilies)
            {
                return m_ComponentFamiliesVisitor;
            }
            else
            {
                return GetVisitor(targetType, mode);
            }
        }

        public GUIVisitor GetVisitor(Type targetType, InspectorMode mode)
        {
            return m_AllVisitors.TryGetValue(new ModeTypePair { Mode = mode, Type = targetType }, out var v) ? v : m_BareboneVisitor;
        }
    }
}
