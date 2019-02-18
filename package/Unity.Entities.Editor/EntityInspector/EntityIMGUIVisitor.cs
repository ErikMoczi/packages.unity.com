using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities.Properties;
using UnityEngine;
using Unity.Properties;
using Unity.Mathematics;
using UnityEditor;

namespace Unity.Entities.Editor
{

    internal class EntityIMGUIVisitor : PropertyVisitor
        , IPrimitivePropertyVisitor
        , ICustomVisitPrimitives
        , ICustomVisit<quaternion>
        , ICustomVisit<float2>
        , ICustomVisit<float3>
        , ICustomVisit<float4>
        , ICustomVisit<float4x4>
        , ICustomVisit<float3x3>
        , ICustomVisit<float2x2>
    {
        private static HashSet<Type> _primitiveTypes = new HashSet<Type>();

        static EntityIMGUIVisitor()
        {
            foreach (var it in typeof(EntityIMGUIVisitor).GetInterfaces())
            {
                if (it.IsGenericType && typeof(ICustomVisit<>) == it.GetGenericTypeDefinition())
                {
                    var genArgs = it.GetGenericArguments();
                    if (genArgs.Length == 1)
                    {
                        _primitiveTypes.Add(genArgs[0]);
                    }
                }
            }
            foreach (var it in typeof(PropertyVisitor).GetInterfaces())
            {
                if (it.IsGenericType && typeof(ICustomVisit<>) == it.GetGenericTypeDefinition())
                {
                    var genArgs = it.GetGenericArguments();
                    if (genArgs.Length == 1)
                    {
                        _primitiveTypes.Add(genArgs[0]);
                    }
                }
            }
        }

        public delegate void EntityDoubleClick(Entity entity);

        private EntityDoubleClick m_entityDoubleClickCallback;

        public EntityIMGUIVisitor(EntityDoubleClick entityDoubleClick)
        {
            m_entityDoubleClickCallback = entityDoubleClick;
        }

        public HashSet<Type> SupportedPrimitiveTypes()
        {
            return _primitiveTypes;
        }

        private static bool IsTypeIdMarker(string s)
        {
            return s == "$TypeId";
        }

        private class ComponentState
        {
            public ComponentState()
            {
                Showing = true;
            }
            public bool WasIndented { get; set; } = true;
            public bool Showing { get; set; }
        }
        private Dictionary<string, ComponentState> _states = new Dictionary<string, ComponentState>();
        private PropertyPath _currentPath = new PropertyPath();

        protected override void Visit<TValue>(TValue value)
        {
            var t = typeof(TValue);
            if (t.IsEnum)
            {
                var options = Enum.GetNames(t).ToArray();
                EditorGUILayout.Popup(
                    t.Name,
                    Array.FindIndex(options, name => name == value.ToString()),
                    options);
            }
            else
            {
                GUILayout.Label(Property.Name);
            }
        }

        private string GetContainerDisplayName<TValue>(VisitContext<TValue> context)
            where TValue : IPropertyContainer
        {
            var f = context.Value?.PropertyBag?.Properties?.First();
            if (f != null && IsTypeIdMarker(f.Name))
            {
                if (f is ValueStructProperty<StructProxy, string>)
                {
                    return (f as ValueStructProperty<StructProxy, string>).GetValue(context.Value);
                }

                if (f is ValueClassProperty<ObjectContainerProxy, string>)
                {
                    return (f as ValueClassProperty<ObjectContainerProxy, string>).GetValue(context.Value);
                }
            }
            return string.Empty;
        }

        private static readonly string s_EntityIdFieldName = "Index";
        private static readonly string s_EntityVersionFieldName = "Version";

        private GUIStyle m_EntityStyle;

        private void RenderEntityView<TValue>(VisitContext<TValue> context)
            where TValue : IPropertyContainer
        {
            if (m_EntityStyle == null)
            {
                m_EntityStyle = new GUIStyle(EditorStyles.label);
                m_EntityStyle.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
                m_EntityStyle.onHover.textColor = new Color(0.0f, 0.7f, 0.7f);
            }

            // @TODO register a TypeConverter

            string index = string.Empty;
            var f = context.Value?.PropertyBag?.FindProperty(s_EntityIdFieldName);
            if (f != null)
            {
                index = (f as IValueProperty).GetObjectValue(context.Value).ToString();
            }

            string version = string.Empty;
            f = context.Value?.PropertyBag?.FindProperty(s_EntityVersionFieldName);
            if (f != null)
            {
                version = (f as IValueProperty).GetObjectValue(context.Value).ToString();
            }

            {
                GUI.enabled = true;

                Rect pos = EditorGUILayout.GetControlRect();
                EditorGUI.LabelField(
                    pos,
                    string.Format("Entity - Index: {0}, Version: {1}", index, version),
                    m_EntityStyle);

                if (Event.current.type == EventType.MouseDown && pos.Contains(Event.current.mousePosition))
                {
                    if (Event.current.clickCount == 2)
                    {
                        Event.current.Use();
                        m_entityDoubleClickCallback?.Invoke(
                            new Entity { Index = int.Parse(index), Version = int.Parse(version)});
                    }
                }

                GUI.enabled = false;
            }
        }

        private static readonly string s_ComponentsPropertyName = "Components";

        private bool IsEmptyComponentDataType(IProperty property, int index, IPropertyBag bag)
        {
            var count = bag?.Properties?.Count();

            if (index == -1 && property != null && property.Name == s_ComponentsPropertyName)
            {
                return false;
            }

            return (count != null && count <= 1);
        }

        private bool ShowContainerFoldoutIfNecessary<TContainer, TValue>(string displayName, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : IPropertyContainer
        {
            var t = typeof(TValue);

            if (typeof(IPropertyContainer).IsAssignableFrom(t))
            {
                // @TODO: dont rely on string comparison
                if (displayName == typeof(Entity).Name)
                {
                    // special treatment for Entities to allow clickable behavior

                    // @TODO improve the visitcontext environment for better type coersion
                    RenderEntityView(new VisitContext<IPropertyContainer>
                    {
                        Property = context.Property,
                        Value = context.Value,
                        Index = context.Index,
                    }
                    );

                    return false;
                }

                if ( ! IsEmptyComponentDataType(Property, context.Index, context.Value?.PropertyBag))
                {
                    ComponentState state;
                    if (!_states.ContainsKey(_currentPath.ToString()))
                    {
                        _states[_currentPath.ToString()] = new ComponentState();
                    }
                    state = _states[_currentPath.ToString()];

                    state.Showing = EditorGUILayout.Foldout(
                        state.Showing,
                        new GUIContent(displayName, displayName),
                        new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }
                    );

                    return state.Showing;
                }

                EditorGUILayout.LabelField(displayName, new GUIStyle(EditorStyles.boldLabel) { fontStyle = FontStyle.Bold });

                return false;
            }
            return true;
        }

        public override bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            return DoBeginContainer(ref container, context);
        }

        public override void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            DoEndContainer(ref container, context);
        }

        public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            return DoBeginContainer(ref container, context);
        }

        public override void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            DoEndContainer(ref container, context);
        }

        public override bool BeginCollection<TContainer, TValue>(ref TContainer container, VisitContext<IList<TValue>> context)
        {
            return DoBeginCollection(ref container, context);
        }

        public override bool BeginCollection<TContainer, TValue>(TContainer container, VisitContext<IList<TValue>> context)
        {
            return DoBeginCollection(ref container, context);
        }

        public override void EndCollection<TContainer, TValue>(ref TContainer container, VisitContext<IList<TValue>> context)
        {
            DoEndCollection(ref container, context);
        }

        public override void EndCollection<TContainer, TValue>(TContainer container, VisitContext<IList<TValue>> context)
        {
            DoEndCollection(ref container, context);
        }

        public bool DoBeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : IPropertyContainer
        {
            VisitSetup(ref container, ref context);

            _currentPath.Push(Property.Name, context.Index);

            var displayName = string.Empty;

            // @TODO Fix ReadOnlyComponentsProperty that treats entity components as list containers
            //       It messes up a potential generic list visitor here.
            if (Property is BufferListProxyProperty)
            {
                displayName = string.Format("Element {0}", context.Index);
            }
            else
            {
                displayName = GetContainerDisplayName(context);
            }

            // Bail out for empty display names, dont show the foldout
            if (string.IsNullOrEmpty(displayName))
                return true;

            if (context.Value is ObjectContainerProxy)
            {
                var c = context.Value as ObjectContainerProxy;

                if (typeof(UnityEngine.Object).IsAssignableFrom(c.o.GetType()))
                {
                    ComponentState state;
                    if (!_states.ContainsKey(_currentPath.ToString()))
                    {
                        _states[_currentPath.ToString()] = new ComponentState();
                    }
                    state = _states[_currentPath.ToString()];

                    state.WasIndented = false;

                    EditorGUILayout.ObjectField(
                        new GUIContent(displayName),
                        (UnityEngine.Object)c.o,
                        c.o.GetType(),
                        false
                        );

                    return false;
                }
            }

            EditorGUI.indentLevel++;

            return ShowContainerFoldoutIfNecessary<TContainer, TValue>(displayName, context);
        }

        public void DoEndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : IPropertyContainer
        {
            VisitSetup(ref container, ref context);

            bool wasIndented = true;
            
            ComponentState state;
            if (_states.TryGetValue(_currentPath.ToString(), out state))
            {
                wasIndented = state.WasIndented;
            }

            _currentPath.Pop();

            var displayName = GetContainerDisplayName(context);
            if (string.IsNullOrEmpty(displayName))
                return;

            if (wasIndented)
            {
                EditorGUI.indentLevel--;
            }
        }

        private static string GetListPropertyDisplayName(string name, int count)
        {
            return string.Format("{0} - [{1}]", name, count);
        }

        public bool DoBeginCollection<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            VisitSetup(ref container, ref context);

            _currentPath.Push(Property.Name, context.Index);

            EditorGUI.indentLevel++;

            ComponentState state;
            if (!_states.ContainsKey(_currentPath.ToString()))
            {
                _states[_currentPath.ToString()] = new ComponentState();
            }
            state = _states[_currentPath.ToString()];

            string displayName = Property.Name;
            if (context.Property != null && typeof(IListProperty).IsAssignableFrom(context.Property.GetType()))
            {
                int count = (context.Property as IListProperty).Count(container);
                displayName = GetListPropertyDisplayName(
                    Property.Name,
                    count
                    );
            }

            state.Showing = EditorGUILayout.Foldout(
                state.Showing,
                displayName,
                new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }
            );

            return state.Showing;
        }

        public void DoEndCollection<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            VisitSetup(ref container, ref context);
            _currentPath.Pop();

            EditorGUI.indentLevel--;
        }

        void ICustomVisit<quaternion>.CustomVisit(quaternion q)
        {
            EditorGUILayout.Vector4Field(Property.Name, new Vector4(q.value.x, q.value.y, q.value.z, q.value.w));
        }

        void ICustomVisit<float2>.CustomVisit(float2 f)
        {
            EditorGUILayout.Vector2Field(Property.Name, (Vector2) f);
        }

        void ICustomVisit<float3>.CustomVisit(float3 f)
        {
            EditorGUILayout.Vector3Field(Property.Name, (float3)f);
        }

        void ICustomVisit<float4>.CustomVisit(float4 f)
        {
            EditorGUILayout.Vector4Field(Property.Name, (float4)f);
        }

        void ICustomVisit<float2x2>.CustomVisit(float2x2 f)
        {
            GUILayout.Label(Property.Name);
            EditorGUILayout.Vector2Field("", (Vector2)f.c0);
            EditorGUILayout.Vector2Field("", (Vector2)f.c1);
        }

        void ICustomVisit<float3x3>.CustomVisit(float3x3 f)
        {
            GUILayout.Label(Property.Name);
            EditorGUILayout.Vector3Field("", (Vector3)f.c0);
            EditorGUILayout.Vector3Field("", (Vector3)f.c1);
            EditorGUILayout.Vector3Field("", (Vector3)f.c2);
        }

        void ICustomVisit<float4x4>.CustomVisit(float4x4 f)
        {
            GUILayout.Label(Property.Name);
            EditorGUILayout.Vector4Field("", (Vector4)f.c0);
            EditorGUILayout.Vector4Field("", (Vector4)f.c1);
            EditorGUILayout.Vector4Field("", (Vector4)f.c2);
            EditorGUILayout.Vector4Field("", (Vector4)f.c3);
        }

        #region ICustomVisitPrimitives

        void ICustomVisit<sbyte>.CustomVisit(sbyte f)
        {
            DoField(Property, f, (label, val) => (sbyte)Mathf.Clamp(EditorGUILayout.IntField(label, val), sbyte.MinValue, sbyte.MaxValue));
        }

        void ICustomVisit<short>.CustomVisit(short f)
        {
            DoField(Property, f, (label, val) => (short)Mathf.Clamp(EditorGUILayout.IntField(label, val), short.MinValue, short.MaxValue));
        }

        void ICustomVisit<int>.CustomVisit(int f)
        {
            DoField(Property, f, (label, val) => EditorGUILayout.IntField(label, val));
        }

        void ICustomVisit<long>.CustomVisit(long f)
        {
            DoField(Property, f, (label, val) => EditorGUILayout.LongField(label, val));
        }

        void ICustomVisit<byte>.CustomVisit(byte f)
        {
            DoField(Property, f, (label, val) => (byte)Mathf.Clamp(EditorGUILayout.IntField(label, val), byte.MinValue, byte.MaxValue));
        }

        void ICustomVisit<ushort>.CustomVisit(ushort f)
        {
            DoField(Property, f, (label, val) => (ushort)Mathf.Clamp(EditorGUILayout.IntField(label, val), ushort.MinValue, ushort.MaxValue));
        }

        void ICustomVisit<uint>.CustomVisit(uint f)
        {
            DoField(Property, f, (label, val) => (uint)Mathf.Clamp(EditorGUILayout.LongField(label, val), uint.MinValue, uint.MaxValue));
        }

        void ICustomVisit<ulong>.CustomVisit(ulong f)
        {
            DoField(Property, f, (label, val) =>
            {
                var text = EditorGUILayout.TextField(label, val.ToString());
                ulong num;
                ulong.TryParse(text, out num);
                return num;
            });
        }

        void ICustomVisit<float>.CustomVisit(float f)
        {
            DoField(Property, f, (label, val) => EditorGUILayout.FloatField(label, val));
        }

        void ICustomVisit<double>.CustomVisit(double f)
        {
            DoField(Property, f, (label, val) => EditorGUILayout.DoubleField(label, val));
        }

        void ICustomVisit<bool>.CustomVisit(bool f)
        {
            DoField(Property, f, (label, val) => EditorGUILayout.Toggle(label, val));
        }

        void ICustomVisit<char>.CustomVisit(char f)
        {
            DoField(Property, f, (label, val) =>
            {
                var text = EditorGUILayout.TextField(label, val.ToString());
                var c = (string.IsNullOrEmpty(text) ? '\0' : text[0]);
                return c;
            });
        }

        void ICustomVisit<string>.CustomVisit(string f)
        {
            if (Property == null || IsTypeIdMarker(Property.Name))
            {
                return;
            }

            DoField(Property, f, (label, val) =>
            {
                return EditorGUILayout.TextField(label, val.ToString());
            });
        }
        #endregion

        private void DoField<TValue>(IProperty property, TValue value, Func<GUIContent, TValue, TValue> onGUI)
        {
            if (property == null)
            {
                return;
            }

            var previous = value;
            onGUI(new GUIContent(property.Name), previous);

#if ENABLE_PROPERTY_SET
            var T = property.GetType();
            var typedProperty = Convert.ChangeType(property, T);

            if (!property.IsReadOnly && typedProperty != null)
            {
                // TODO doesn not work, ref container & container access
                T.GetMethod("SetValue").Invoke(property, new object[] { container, v });
            }
#endif
        }
    }
}
