using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

#if !(NET_4_0 || NET_4_6)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////TODO: allow setting whether the device should automatically become current and whether it wants noise filtering

////TODO: turn 'overrides' into feature where layouts can be registered as overrides and they get merged *into* the layout
////      they are overriding

////TODO: ensure that if a layout sets a device description, it is indeed a device layout

////TODO: make offset on InputControlAttribute relative to field instead of relative to entire state struct

////REVIEW: common usages are on all layouts but only make sense for devices

////REVIEW: kill layout namespacing for remotes and have remote players instantiate layouts from editor instead?
////        loses the ability for layouts to be different in the player than in the editor but if we take it as granted that
////           a) a given layout X always is the same regardless to which player it is deployed, and that
////           b) the editor always has all layouts
////        then we can just kill off the entire namespacing. This also makes it much easier to tweak layouts in the
////        editor.

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A control layout specifies the composition of an input control.
    /// </summary>
    /// <remarks>
    /// Control layouts can be created in three ways:
    ///
    /// <list type="number">
    /// <item><description>Loaded from JSON.</description></item>
    /// <item><description>Constructed through reflection from <see cref="InputControl">InputControls</see> classes.</description></item>
    /// <item><description>Through layout factories using <see cref="InputControlLayout.Builder"/>.</description></item>
    /// </list>
    ///
    /// Once constructed, control layouts are immutable (but you can always
    /// replace a registered layout in the system and it will affect
    /// everything constructed from the layout).
    ///
    /// Control layouts can be for arbitrary control rigs or for entire
    /// devices. Device layouts can use the <see cref="deviceMatcher"/> field
    /// to specify regexs that are to match against compatible devices.
    ///
    /// InputControlLayout objects are considered temporaries. Except in the
    /// editor, they are not kept around beyond device creation.
    /// </remarks>
    public class InputControlLayout
    {
        // String that is used to separate names from namespaces in layout names.
        public const string kNamespaceQualifier = "::";

        private static InternedString s_DefaultVariant = new InternedString("Default");
        public static InternedString DefaultVariant
        {
            get { return s_DefaultVariant; }
        }

        public enum ParameterType
        {
            Boolean,
            Integer,
            Float
        }

        // Both controls and processors can have public fields that can be set
        // directly from layouts. The values are usually specified in strings
        // (like "clampMin=-1") but we parse them ahead of time into instances
        // of this structure that tell us where to store the value in the control.
        public unsafe struct ParameterValue
        {
            public const int kMaxValueSize = 4;

            public string name;
            public ParameterType type;
            public fixed byte value[kMaxValueSize];

            public int sizeInBytes
            {
                get
                {
                    switch (type)
                    {
                        case ParameterType.Boolean: return sizeof(bool);
                        case ParameterType.Float: return sizeof(float);
                        case ParameterType.Integer: return sizeof(int);
                    }
                    return 0;
                }
            }

            public override string ToString()
            {
                fixed(byte* ptr = value)
                {
                    switch (type)
                    {
                        case ParameterType.Boolean:
                            if (*((bool*)ptr))
                                return name;
                            return string.Format("{0}=false", name);
                        case ParameterType.Integer:
                            var intValue = *((int*)ptr);
                            return string.Format("{0}={1}", name, intValue);
                        case ParameterType.Float:
                            var floatValue = *((float*)ptr);
                            return string.Format("{0}={1}", name, floatValue);
                    }
                }

                return string.Empty;
            }
        }

        public struct NameAndParameters
        {
            public string name;
            public ReadOnlyArray<ParameterValue> parameters;

            public override string ToString()
            {
                if (parameters.Count == 0)
                    return name;
                var parameterString = string.Join(",", parameters.Select(x => x.ToString()).ToArray());
                return string.Format("name({0})", parameterString);
            }
        }

        /// <summary>
        /// Specification for the composition of a direct or indirect child control.
        /// </summary>
        public struct ControlItem
        {
            [Flags]
            public enum Flags
            {
                IsModifyingChildControlByPath = 1 << 0,
                IsNoisy = 1 << 1,
            }

            /// <summary>
            /// Name of the control.
            /// </summary>
            /// <remarks>
            /// This may also be a path. This can be used to reach
            /// inside another layout and modify properties of a control inside
            /// of it. An example for this is adding a "leftStick" control using the
            /// Stick layout and then adding two control layouts that refer to
            /// "leftStick/x" and "leftStick/y" respectively to modify the state
            /// format used by the stick.
            ///
            /// This field is required.
            /// </remarks>
            /// <seealso cref="isModifyingChildControlByPath"/>
            public InternedString name;

            public InternedString layout;
            public InternedString variant;
            public string useStateFrom;

            /// <summary>
            /// Optional display name of the control.
            /// </summary>
            /// <seealso cref="InputControl.displayName"/>
            public string displayName;

            public string resourceName;
            public ReadOnlyArray<InternedString> usages;
            public ReadOnlyArray<InternedString> aliases;
            public ReadOnlyArray<ParameterValue> parameters;
            public ReadOnlyArray<NameAndParameters> processors;
            public uint offset;
            public uint bit;
            public uint sizeInBits;
            public FourCC format;
            public Flags flags;
            public int arraySize;

            // If true, the layout will not add a control but rather a modify a control
            // inside the hierarchy added by 'layout'. This allows, for example, to modify
            // just the X axis control of the left stick directly from within a gamepad
            // layout instead of having to have a custom stick layout for the left stick
            // than in turn would have to make use of a custom axis layout for the X axis.
            // Insted, you can just have a control layout with the name "leftStick/x".
            public bool isModifyingChildControlByPath
            {
                get { return (flags & Flags.IsModifyingChildControlByPath) == Flags.IsModifyingChildControlByPath; }
                set
                {
                    if (value)
                        flags |= Flags.IsModifyingChildControlByPath;
                    else
                        flags &= ~Flags.IsModifyingChildControlByPath;
                }
            }

            public bool isNoisy
            {
                get { return (flags & Flags.IsNoisy) == Flags.IsNoisy; }
                set
                {
                    if (value)
                        flags |= Flags.IsNoisy;
                    else
                        flags &= ~Flags.IsNoisy;
                }
            }

            public bool isArray
            {
                get { return (arraySize != 0); }
            }

            /// <summary>
            /// For any property not set on this control layout, take the setting from <paramref name="other"/>.
            /// </summary>
            /// <param name="other">Control layout providing settings.</param>
            /// <remarks>
            /// <see cref="name"/> will not be touched.
            /// </remarks>
            public ControlItem Merge(ControlItem other)
            {
                var result = new ControlItem();

                result.name = name;
                Debug.Assert(!name.IsEmpty());
                result.isModifyingChildControlByPath = isModifyingChildControlByPath;

                result.layout = layout.IsEmpty() ? other.layout : layout;
                result.variant = variant.IsEmpty() ? other.variant : variant;
                result.useStateFrom = useStateFrom ?? other.useStateFrom;
                result.arraySize = !isArray ? other.arraySize : arraySize;

                if (offset != InputStateBlock.kInvalidOffset)
                    result.offset = offset;
                else
                    result.offset = other.offset;

                if (bit != InputStateBlock.kInvalidOffset)
                    result.bit = bit;
                else
                    result.bit = other.bit;

                if (format != 0)
                    result.format = format;
                else
                    result.format = other.format;

                if (sizeInBits != 0)
                    result.sizeInBits = sizeInBits;
                else
                    result.sizeInBits = other.sizeInBits;

                result.aliases = new ReadOnlyArray<InternedString>(
                        ArrayHelpers.Merge(aliases.m_Array,
                            other.aliases.m_Array));

                result.usages = new ReadOnlyArray<InternedString>(
                        ArrayHelpers.Merge(usages.m_Array,
                            other.usages.m_Array));

                // We don't merge parameters. If a control sets parameters, it'll overwrite
                // parameters inherited from the base.
                if (parameters.Count == 0)
                    result.parameters = other.parameters;
                else
                    result.parameters = parameters;

                // Same for processors.
                if (processors.Count == 0)
                    result.processors = other.processors;
                else
                    result.processors = processors;

                if (!string.IsNullOrEmpty(displayName))
                    result.displayName = displayName;
                else
                    result.displayName = other.displayName;

                if (!string.IsNullOrEmpty(resourceName))
                    result.resourceName = resourceName;
                else
                    result.resourceName = other.resourceName;

                return result;
            }
        }

        // Unique name of the layout.
        // NOTE: Case-insensitive.
        public InternedString name
        {
            get { return m_Name; }
        }

        public Type type
        {
            get { return m_Type; }
        }

        public InternedString variant
        {
            get { return m_Variant; }
        }

        public FourCC stateFormat
        {
            get { return m_StateFormat; }
        }

        public string extendsLayout
        {
            get { return m_ExtendsLayout; }
        }

        public ReadOnlyArray<InternedString> commonUsages
        {
            get { return new ReadOnlyArray<InternedString>(m_CommonUsages); }
        }

        public InputDeviceMatcher deviceMatcher
        {
            get { return m_DeviceMatcher; }
        }

        public ReadOnlyArray<ControlItem> controls
        {
            get { return new ReadOnlyArray<ControlItem>(m_Controls); }
        }

        public bool isDeviceLayout
        {
            get { return typeof(InputDevice).IsAssignableFrom(m_Type); }
        }

        public bool isControlLayout
        {
            get { return !isDeviceLayout; }
        }

        /// <summary>
        /// Build a layout programmatically. Primarily for use by layout builders
        /// registered with the system.
        /// </summary>
        /// <seealso cref="InputSystem.RegisterControlLayoutBuilder"/>
        public struct Builder
        {
            public string name;
            public Type type;
            public FourCC stateFormat;
            public string extendsLayout;
            public bool? updateBeforeRender;
            public InputDeviceMatcher deviceMatcher;

            private int m_ControlCount;
            private ControlItem[] m_Controls;

            public struct ControlBuilder
            {
                internal Builder builder;
                internal ControlItem[] controls;
                internal int index;

                public ControlBuilder WithLayout(string layout)
                {
                    if (string.IsNullOrEmpty(layout))
                        throw new ArgumentException("Layout name cannot be null or empty", "layout");

                    controls[index].layout = new InternedString(layout);
                    return this;
                }

                public ControlBuilder WithFormat(FourCC format)
                {
                    controls[index].format = format;
                    return this;
                }

                public ControlBuilder WithFormat(string format)
                {
                    return WithFormat(new FourCC(format));
                }

                public ControlBuilder WithOffset(uint offset)
                {
                    controls[index].offset = offset;
                    return this;
                }

                public ControlBuilder WithBit(uint bit)
                {
                    controls[index].bit = bit;
                    return this;
                }

                public ControlBuilder WithUsages(params InternedString[] usages)
                {
                    if (usages == null || usages.Length == 0)
                        return this;

                    for (var i = 0; i < usages.Length; ++i)
                        if (usages[i].IsEmpty())
                            throw new ArgumentException(
                                string.Format("Empty usage entry at index {0} for control '{1}' in layout '{2}'", i,
                                    controls[index].name, builder.name), "usages");

                    controls[index].usages = new ReadOnlyArray<InternedString>(usages);
                    return this;
                }

                public ControlBuilder WithUsages(IEnumerable<string> usages)
                {
                    var usagesArray = usages.Select(x => new InternedString(x)).ToArray();
                    return WithUsages(usagesArray);
                }

                public ControlBuilder WithUsages(params string[] usages)
                {
                    return WithUsages((IEnumerable<string>)usages);
                }

                public ControlBuilder WithParameters(string parameters)
                {
                    var parsed = ParseParameters(parameters);
                    controls[index].parameters = new ReadOnlyArray<ParameterValue>(parsed);
                    return this;
                }

                public ControlBuilder AsArrayOfControlsWithSize(int arraySize)
                {
                    controls[index].arraySize = arraySize;
                    return this;
                }
            }

            // This invalidates the ControlBuilders from previous calls! (our array may move)
            /// <summary>
            /// Add a new control to the layout.
            /// </summary>
            /// <param name="name">Name or path of the control. If it is a path (e.g. <c>"leftStick/x"</c>,
            /// then the control either modifies the setup of a child control of another control in the layout
            /// or adds a new child control to another control in the layout. Modifying child control is useful,
            /// for example, to alter the state format of controls coming from the base layout. Likewise,
            /// adding child controls to another control is useful to modify the setup of of the control layout
            /// being used without having to create and register a custom control layout.</param>
            /// <returns>A control builder that permits setting various parameters on the control.</returns>
            /// <exception cref="ArgumentException"><paramref name="name"/> is null or empty.</exception>
            public ControlBuilder AddControl(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException(name);

                var index = ArrayHelpers.AppendWithCapacity(ref m_Controls, ref m_ControlCount,
                        new ControlItem {name = new InternedString(name)});

                return new ControlBuilder
                {
                    builder = this,
                    controls = m_Controls,
                    index = index
                };
            }

            public Builder WithName(string name)
            {
                this.name = name;
                return this;
            }

            public Builder WithType<T>()
                where T : InputControl
            {
                type = typeof(T);
                return this;
            }

            public Builder WithFormat(FourCC format)
            {
                stateFormat = format;
                return this;
            }

            public Builder WithFormat(string format)
            {
                return WithFormat(new FourCC(format));
            }

            public Builder ForDevice(InputDeviceMatcher matcher)
            {
                deviceMatcher = matcher;
                return this;
            }

            public Builder Extend(string baseLayoutName)
            {
                extendsLayout = baseLayoutName;
                return this;
            }

            public InputControlLayout Build()
            {
                ControlItem[] controls = null;
                if (m_ControlCount > 0)
                {
                    controls = new ControlItem[m_ControlCount];
                    Array.Copy(m_Controls, controls, m_ControlCount);
                }

                // Allow layout to be unnamed. The system will automatically set the
                // name that the layout has been registered under.
                var layout =
                    new InputControlLayout(new InternedString(name),
                        type == null && string.IsNullOrEmpty(extendsLayout) ? typeof(InputDevice) : type)
                {
                    m_StateFormat = stateFormat,
                    m_ExtendsLayout = new InternedString(extendsLayout),
                    m_DeviceMatcher = deviceMatcher,
                    m_Controls = controls,
                    m_UpdateBeforeRender = updateBeforeRender
                };

                return layout;
            }
        }

        // Uses reflection to construct a layout from the given type.
        // Can be used with both control classes and state structs.
        public static InputControlLayout FromType(string name, Type type)
        {
            var controlLayouts = new List<ControlItem>();
            var layoutAttribute = type.GetCustomAttribute<InputControlLayoutAttribute>(true);

            // If there's an InputControlLayoutAttribute on the type that has 'stateType' set,
            // add control layouts from its state (if present) instead of from the type.
            var stateFormat = new FourCC();
            if (layoutAttribute != null && layoutAttribute.stateType != null)
            {
                AddControlItems(layoutAttribute.stateType, controlLayouts, name);

                // Get state type code from state struct.
                if (typeof(IInputStateTypeInfo).IsAssignableFrom(layoutAttribute.stateType))
                {
                    stateFormat = ((IInputStateTypeInfo)Activator.CreateInstance(layoutAttribute.stateType))
                        .GetFormat();
                }
            }
            else
            {
                // Add control layouts from type contents.
                AddControlItems(type, controlLayouts, name);
            }

            if (layoutAttribute != null && !string.IsNullOrEmpty(layoutAttribute.stateFormat))
                stateFormat = new FourCC(layoutAttribute.stateFormat);

            // Determine variant (if any).
            var variant = new InternedString();
            if (layoutAttribute != null)
                variant = new InternedString(layoutAttribute.variant);

            ////TODO: make sure all usages are unique (probably want to have a check method that we can run on json layouts as well)
            ////TODO: make sure all paths are unique (only relevant for JSON layouts?)

            // Create layout object.
            var layout = new InputControlLayout(name, type);
            layout.m_Controls = controlLayouts.ToArray();
            layout.m_StateFormat = stateFormat;
            layout.m_Variant = variant;

            if (layoutAttribute != null && layoutAttribute.commonUsages != null)
                layout.m_CommonUsages =
                    ArrayHelpers.Select(layoutAttribute.commonUsages, x => new InternedString(x));

            return layout;
        }

        public string ToJson()
        {
            var layout = LayoutJson.FromLayout(this);
            return JsonUtility.ToJson(layout);
        }

        // Constructs a layout from the given JSON source.
        public static InputControlLayout FromJson(string json)
        {
            var layoutJson = JsonUtility.FromJson<LayoutJson>(json);
            return layoutJson.ToLayout();
        }

        ////REVIEW: shouldn't state be split between input and output? how does output fit into the layout picture in general?
        ////        should the control layout alone determine the direction things are going in?

        private InternedString m_Name;
        internal Type m_Type; // For extension chains, we can only discover types after loading multiple layouts, so we make this accessible to InputDeviceBuilder.
        internal InternedString m_Variant;
        internal FourCC m_StateFormat;
        internal int m_StateSizeInBytes; // Note that this is the combined state size for input and output.
        internal bool? m_UpdateBeforeRender;
        private InternedString m_ExtendsLayout;
#pragma warning disable 0414
        private InternedString[] m_OverridesLayouts; ////TODO
#pragma warning restore 0414
        private InternedString[] m_CommonUsages;
        internal ControlItem[] m_Controls;
        private InputDeviceMatcher m_DeviceMatcher;
        internal string m_DisplayName;
        internal string m_ResourceName;

        private InputControlLayout(string name, Type type)
        {
            m_Name = new InternedString(name);
            m_Type = type;
        }

        private static void AddControlItems(Type type, List<ControlItem> controlLayouts, string layoutName)
        {
            AddControlItemsFromFields(type, controlLayouts, layoutName);
            AddControlItemsFromProperties(type, controlLayouts, layoutName);
        }

        // Add ControlLayouts for every public property in the given type thas has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlItemsFromFields(Type type, List<ControlItem> controlLayouts, string layoutName)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            AddControlItemsFromMembers(fields, controlLayouts, layoutName);
        }

        // Add ControlLayouts for every public property in the given type thas has
        // InputControlAttribute applied to it or has an InputControl-derived value type.
        private static void AddControlItemsFromProperties(Type type, List<ControlItem> controlLayouts, string layoutName)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            AddControlItemsFromMembers(properties, controlLayouts, layoutName);
        }

        // Add ControlLayouts for every member in the list thas has InputControlAttribute applied to it
        // or has an InputControl-derived value type.
        private static void AddControlItemsFromMembers(MemberInfo[] members, List<ControlItem> controlItems, string layoutName)
        {
            foreach (var member in members)
            {
                // Skip anything declared inside InputControl itself.
                // Filters out m_Device etc.
                if (member.DeclaringType == typeof(InputControl))
                    continue;

                var valueType = TypeHelpers.GetValueType(member);

                // If the value type of the member is a struct type and implements the IInputStateTypeInfo
                // interface, dive inside and look. This is useful for composing states of one another.
                if (valueType != null && valueType.IsValueType && typeof(IInputStateTypeInfo).IsAssignableFrom(valueType))
                {
                    var controlCountBefore = controlItems.Count;

                    AddControlItems(valueType, controlItems, layoutName);

                    // If the current member is a field that is embedding the state structure, add
                    // the field offset to all control layouts that were added from the struct.
                    var memberAsField = member as FieldInfo;
                    if (memberAsField != null)
                    {
                        var fieldOffset = Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();
                        var countrolCountAfter = controlItems.Count;
                        for (var i = controlCountBefore; i < countrolCountAfter; ++i)
                        {
                            var controlLayout = controlItems[i];
                            if (controlItems[i].offset != InputStateBlock.kInvalidOffset)
                            {
                                controlLayout.offset += (uint)fieldOffset;
                                controlItems[i] = controlLayout;
                            }
                        }
                    }

                    ////TODO: allow attributes on the member to modify control layouts inside the struct
                }

                // Look for InputControlAttributes. If they aren't there, the member has to be
                // of an InputControl-derived value type.
                var attributes = member.GetCustomAttributes<InputControlAttribute>(false).ToArray();
                if (attributes.Length == 0)
                {
                    if (valueType == null || !typeof(InputControl).IsAssignableFrom(valueType))
                        continue;
                }

                AddControlItemsFromMember(member, attributes, controlItems, layoutName);
            }
        }

        private static void AddControlItemsFromMember(MemberInfo member,
            InputControlAttribute[] attributes, List<ControlItem> controlItems, string layoutName)
        {
            // InputControlAttribute can be applied multiple times to the same member,
            // generating a separate control for each ocurrence. However, it can also
            // not be applied at all in which case we still add a control layout (the
            // logic that called us already made sure the member is eligible for this kind
            // of operation).

            if (attributes.Length == 0)
            {
                var controlLayout = CreateControlItemFromMember(member, null, layoutName);
                ThrowIfControlItemIsDuplicate(ref controlLayout, controlItems, layoutName);
                controlItems.Add(controlLayout);
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    var controlLayout = CreateControlItemFromMember(member, attribute, layoutName);
                    ThrowIfControlItemIsDuplicate(ref controlLayout, controlItems, layoutName);
                    controlItems.Add(controlLayout);
                }
            }
        }

        private static ControlItem CreateControlItemFromMember(MemberInfo member, InputControlAttribute attribute, string layoutName)
        {
            ////REVIEW: make sure that the value type of the field and the value type of the control match?

            // Determine name.
            var name = attribute != null ? attribute.name : null;
            if (string.IsNullOrEmpty(name))
                name = member.Name;

            var isModifyingChildControlByPath = name.IndexOf('/') != -1;

            // Determine layout.
            var layout = attribute != null ? attribute.layout : null;
            if (string.IsNullOrEmpty(layout) && !isModifyingChildControlByPath &&
                (!(member is FieldInfo) || member.GetCustomAttribute<FixedBufferAttribute>(false) == null)) // Ignore fixed buffer fields.
            {
                var valueType = TypeHelpers.GetValueType(member);
                layout = InferLayoutFromValueType(valueType);
            }

            // Determine variant.
            string variant = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.variant))
                variant = attribute.variant;

            // Determine offset.
            var offset = InputStateBlock.kInvalidOffset;
            if (attribute != null && attribute.offset != InputStateBlock.kInvalidOffset)
                offset = attribute.offset;
            else if (member is FieldInfo && !isModifyingChildControlByPath)
                offset = (uint)Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();

            // Determine bit offset.
            var bit = InputStateBlock.kInvalidOffset;
            if (attribute != null)
                bit = attribute.bit;

            ////TODO: if size is not set, determine from type of field
            // Determine size.
            var sizeInBits = 0u;
            if (attribute != null)
                sizeInBits = attribute.sizeInBits;

            // Determine format.
            var format = new FourCC();
            if (attribute != null && !string.IsNullOrEmpty(attribute.format))
                format = new FourCC(attribute.format);
            else if (!isModifyingChildControlByPath && bit == InputStateBlock.kInvalidOffset)
            {
                var valueType = TypeHelpers.GetValueType(member);
                format = InputStateBlock.GetPrimitiveFormatFromType(valueType);
            }

            // Determine aliases.
            InternedString[] aliases = null;
            if (attribute != null)
            {
                var joined = ArrayHelpers.Join(attribute.alias, attribute.aliases);
                if (joined != null)
                    aliases = joined.Select(x => new InternedString(x)).ToArray();
            }

            // Determine usages.
            InternedString[] usages = null;
            if (attribute != null)
            {
                var joined = ArrayHelpers.Join(attribute.usage, attribute.usages);
                if (joined != null)
                    usages = joined.Select(x => new InternedString(x)).ToArray();
            }

            // Determine parameters.
            ParameterValue[] parameters = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.parameters))
                parameters = ParseParameters(attribute.parameters);

            // Determine processors.
            NameAndParameters[] processors = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.processors))
                processors = ParseNameAndParameterList(attribute.processors);

            // Determine whether to use state from another control.
            string useStateFrom = null;
            if (attribute != null && !string.IsNullOrEmpty(attribute.useStateFrom))
                useStateFrom = attribute.useStateFrom;

            // Determine if it's a noisy control.
            var isNoisy = false;
            if (attribute != null)
                isNoisy = attribute.noisy;

            // Determine array size.
            var arraySize = 0;
            if (attribute != null)
                arraySize = attribute.arraySize;

            return new ControlItem
            {
                name = new InternedString(name),
                layout = new InternedString(layout),
                variant = new InternedString(variant),
                useStateFrom = useStateFrom,
                format = format,
                offset = offset,
                bit = bit,
                sizeInBits = sizeInBits,
                parameters = new ReadOnlyArray<ParameterValue>(parameters),
                processors = new ReadOnlyArray<NameAndParameters>(processors),
                usages = new ReadOnlyArray<InternedString>(usages),
                aliases = new ReadOnlyArray<InternedString>(aliases),
                isModifyingChildControlByPath = isModifyingChildControlByPath,
                isNoisy = isNoisy,
                arraySize = arraySize,
            };
        }

        internal static NameAndParameters[] ParseNameAndParameterList(string text)
        {
            List<NameAndParameters> list = null;
            if (!ParseNameAndParameterList(text, ref list))
                return null;
            return list.ToArray();
        }

        internal static bool ParseNameAndParameterList(string text, ref List<NameAndParameters> list)
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
                return false;

            if (list == null)
                list = new List<NameAndParameters>();
            else
                list.Clear();

            var index = 0;
            var textLength = text.Length;

            while (index < textLength)
            {
                // Skip whitespace.
                while (index < textLength && char.IsWhiteSpace(text[index]))
                    ++index;

                // Parse name.
                var nameStart = index;
                while (index < textLength)
                {
                    var nextChar = text[index];
                    if (nextChar == '(' || nextChar == ',' || char.IsWhiteSpace(nextChar))
                        break;
                    ++index;
                }
                if (index - nameStart == 0)
                    throw new Exception(string.Format("Expecting name at position {0} in '{1}'", nameStart, text));
                var name = text.Substring(nameStart, index - nameStart);

                // Skip whitespace.
                while (index < textLength && char.IsWhiteSpace(text[index]))
                    ++index;

                // Parse parameters.
                ParameterValue[] parameters = null;
                if (index < textLength && text[index] == '(')
                {
                    ++index;
                    var closeParenIndex = text.IndexOf(')', index);
                    if (closeParenIndex == -1)
                        throw new Exception(string.Format("Expecting ')' after '(' at position {0} in '{1}'", index,
                                text));

                    var parameterString = text.Substring(index, closeParenIndex - index);
                    parameters = ParseParameters(parameterString);
                    index = closeParenIndex + 1;
                }

                if (index < textLength && text[index] == ',')
                    ++index;

                list.Add(new NameAndParameters { name = name, parameters = new ReadOnlyArray<ParameterValue>(parameters) });
            }

            return true;
        }

        private static ParameterValue[] ParseParameters(string parameterString)
        {
            parameterString = parameterString.Trim();
            if (string.IsNullOrEmpty(parameterString))
                return null;

            var parameterCount = parameterString.CountOccurrences(',') + 1;
            var parameters = new ParameterValue[parameterCount];

            var index = 0;
            for (var i = 0; i < parameterCount; ++i)
            {
                var parameter = ParseParameter(parameterString, ref index);
                parameters[i] = parameter;
            }

            return parameters;
        }

        private static unsafe ParameterValue ParseParameter(string parameterString, ref int index)
        {
            var parameter = new ParameterValue();
            var parameterStringLength = parameterString.Length;

            // Skip whitespace.
            while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                ++index;

            // Parse name.
            var nameStart = index;
            while (index < parameterStringLength)
            {
                var nextChar = parameterString[index];
                if (nextChar == '=' || nextChar == ',' || char.IsWhiteSpace(nextChar))
                    break;
                ++index;
            }
            parameter.name = parameterString.Substring(nameStart, index - nameStart);

            // Skip whitespace.
            while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                ++index;

            if (index == parameterStringLength || parameterString[index] != '=')
            {
                // No value given so take "=true" as implied.
                parameter.type = ParameterType.Boolean;
                *((bool*)parameter.value) = true;
            }
            else
            {
                ++index; // Skip over '='.

                // Skip whitespace.
                while (index < parameterStringLength && char.IsWhiteSpace(parameterString[index]))
                    ++index;

                // Parse value.
                var valueStart = index;
                while (index < parameterStringLength &&
                       !(parameterString[index] == ',' || char.IsWhiteSpace(parameterString[index])))
                    ++index;

                var value = parameterString.Substring(valueStart, index - valueStart);
                if (string.Compare(value, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    parameter.type = ParameterType.Boolean;
                    *((bool*)parameter.value) = true;
                }
                else if (string.Compare(value, "false", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    parameter.type = ParameterType.Boolean;
                    *((bool*)parameter.value) = false;
                }
                else if (value.IndexOf('.') != -1)
                {
                    parameter.type = ParameterType.Float;
                    *((float*)parameter.value) = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }
                else
                {
                    parameter.type = ParameterType.Integer;
                    *((int*)parameter.value) = int.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
                }
            }

            if (index < parameterStringLength && parameterString[index] == ',')
                ++index;

            return parameter;
        }

        ////REVIEW: this tends to cause surprises; is it worth its cost?
        private static string InferLayoutFromValueType(Type type)
        {
            var typeName = type.Name;
            if (typeName.EndsWith("Control"))
                return typeName.Substring(0, typeName.Length - "Control".Length);
            if (!type.IsPrimitive)
                return typeName;
            return null;
        }

        /// <summary>
        /// Merge the settings from <paramref name="other"/> into the layout such that they become
        /// the base settings.
        /// </summary>
        /// <param name="other"></param>
        /// <remarks>
        /// This is the central method for allowing layouts to 'inherit' settings from their
        /// base layout. It will merge the information in <paramref name="other"/> into the current
        /// layout such that the existing settings in the current layout acts as if applied on top
        /// of the settings in the base layout.
        /// </remarks>
        public void MergeLayout(InputControlLayout other)
        {
            m_Type = m_Type ?? other.m_Type;
            m_UpdateBeforeRender = m_UpdateBeforeRender ?? other.m_UpdateBeforeRender;

            if (m_Variant.IsEmpty())
                m_Variant = other.m_Variant;

            // If the layout has a variant set on it, we want to merge away information coming
            // from 'other' than isn't relevant to that variant.
            var layoutIsTargetingSpecificVariant = !m_Variant.IsEmpty();

            if (m_StateFormat == new FourCC())
                m_StateFormat = other.m_StateFormat;

            if (string.IsNullOrEmpty(m_DisplayName))
                m_DisplayName = other.m_DisplayName;
            if (string.IsNullOrEmpty(m_ResourceName))
                m_ResourceName = other.m_ResourceName;

            // Combine common usages.
            m_CommonUsages = ArrayHelpers.Merge(other.m_CommonUsages, m_CommonUsages);

            // Merge controls.
            if (m_Controls == null)
                m_Controls = other.m_Controls;
            else
            {
                var baseControls = other.m_Controls;

                // Even if the counts match we don't know how many controls are in the
                // set until we actually gone through both control lists and looked at
                // the names.

                var controls = new List<ControlItem>();
                var baseControlVariants = new List<string>();

                ////REVIEW: should setting a variant directly on a layout force that variant to automatically
                ////        be set on every control item directly defined in that layout?

                var baseControlTable = CreateLookupTableForControls(baseControls, baseControlVariants);
                var thisControlTable = CreateLookupTableForControls(m_Controls);

                // First go through every control we have in this layout. Add every control from
                // `thisControlTable` while removing corresponding control items from `baseControlTable`.
                foreach (var pair in thisControlTable)
                {
                    ControlItem baseControlItem;
                    if (baseControlTable.TryGetValue(pair.Key, out baseControlItem))
                    {
                        var mergedLayout = pair.Value.Merge(baseControlItem);
                        controls.Add(mergedLayout);

                        // Remove the entry so we don't hit it again in the pass through
                        // baseControlTable below.
                        baseControlTable.Remove(pair.Key);
                    }
                    ////REVIEW: is this really the most useful behavior?
                    // We may be looking at a control that is using variants on the base layout but
                    // isn't targeting a specific variant on the derived layout. In that case, we
                    // want to take each of the variants from the base layout and merge them with
                    // the control layout in the derived layout.
                    else if (pair.Value.variant.IsEmpty() || pair.Value.variant == DefaultVariant)
                    {
                        var isTargetingVariants = false;
                        if (layoutIsTargetingSpecificVariant)
                        {
                            // We're only looking for one specific variant so try only that one.
                            if (baseControlVariants.Contains(m_Variant))
                            {
                                var key = string.Format("{0}@{1}", pair.Key, m_Variant.ToLower());
                                if (baseControlTable.TryGetValue(key, out baseControlItem))
                                {
                                    var mergedLayout = pair.Value.Merge(baseControlItem);
                                    controls.Add(mergedLayout);
                                    baseControlTable.Remove(key);
                                    isTargetingVariants = true;
                                }
                            }
                        }
                        else
                        {
                            // Try each variant present in the base layout.
                            foreach (var variant in baseControlVariants)
                            {
                                var key = string.Format("{0}@{1}", pair.Key, variant);
                                if (baseControlTable.TryGetValue(key, out baseControlItem))
                                {
                                    var mergedLayout = pair.Value.Merge(baseControlItem);
                                    controls.Add(mergedLayout);
                                    baseControlTable.Remove(key);
                                    isTargetingVariants = true;
                                }
                            }
                        }

                        // Okay, this control item isn't corresponding to anything in the base layout
                        // so just add it as is.
                        if (!isTargetingVariants)
                            controls.Add(pair.Value);
                    }
                    // We may be looking at a control that is targeting a specific variant
                    // in this layout but not targeting a variant in the base layout. We still want to
                    // merge information from that non-targeted base control.
                    else if (baseControlTable.TryGetValue(pair.Value.name.ToLower(), out baseControlItem))
                    {
                        var mergedLayout = pair.Value.Merge(baseControlItem);
                        controls.Add(mergedLayout);
                        baseControlTable.Remove(pair.Value.name.ToLower());
                    }
                    // Seems like we can't match it to a control in the base layout. We already know it
                    // must have a variant setting (because we checked above) so if the variant setting
                    // doesn't prevent us, just include the control. It's most likely a path-modifying
                    // control (e.g. "rightStick/x").
                    else if (pair.Value.variant == m_Variant)
                    {
                        controls.Add(pair.Value);
                    }
                }

                // And then go through all the controls in the base and take the
                // ones we're missing. We've already removed all the ones that intersect
                // and had to be merged so the rest we can just slurp into the list as is.
                if (!layoutIsTargetingSpecificVariant)
                {
                    controls.AddRange(baseControlTable.Values);
                }
                else
                {
                    // Filter out controls coming from the base layout which are targeting variants
                    // that we're not interested in.
                    controls.AddRange(
                        baseControlTable.Values.Where(x => x.variant.IsEmpty() || x.variant == m_Variant || x.variant == DefaultVariant));
                }

                m_Controls = controls.ToArray();
            }
        }

        private static Dictionary<string, ControlItem> CreateLookupTableForControls(
            ControlItem[] controlItems, List<string> variants = null)
        {
            var table = new Dictionary<string, ControlItem>();
            for (var i = 0; i < controlItems.Length; ++i)
            {
                var key = controlItems[i].name.ToLower();
                // Need to take variant into account as well. Otherwise two variants for
                // "leftStick", for example, will overwrite each other.
                var variant = controlItems[i].variant;
                if (!variant.IsEmpty() && variant != DefaultVariant)
                {
                    key = string.Format("{0}@{1}", key, variant.ToLower());
                    if (variants != null)
                        variants.Add(variant.ToLower());
                }
                table[key] = controlItems[i];
            }
            return table;
        }

        private static void ThrowIfControlItemIsDuplicate(ref ControlItem controlItem,
            IEnumerable<ControlItem> controlLayouts, string layoutName)
        {
            var name = controlItem.name;
            foreach (var existing in controlLayouts)
                if (string.Compare(name, existing.name, StringComparison.OrdinalIgnoreCase) == 0 &&
                    existing.variant == controlItem.variant)
                    throw new Exception(string.Format("Duplicate control '{0}' in layout '{1}'", name, layoutName));
        }

        internal static string ParseHeaderFromJson(string json, out InputDeviceMatcher deviceMatcher, out string baseLayout)
        {
            var layoutJson = JsonUtility.FromJson<LayoutJsonNameAndDescriptorOnly>(json);
            deviceMatcher = layoutJson.device.ToMatcher();
            baseLayout = layoutJson.extend;
            return layoutJson.name;
        }

        [Serializable]
        private struct LayoutJsonNameAndDescriptorOnly
        {
            public string name;
            public string extend;
            public InputDeviceMatcher.MatcherJson device;
        }

        [Serializable]
        private struct LayoutJson
        {
            // Disable warnings that these fields are never assigned to. They are set
            // by JsonUtility.
            #pragma warning disable 0649
            // ReSharper disable MemberCanBePrivate.Local

            public string name;
            public string extend;
            public string @override; // Convenience to not have to create array for single override.
            public string[] overrides;
            public string format;
            public string beforeRender; // Can't be simple bool as otherwise we can't tell whether it was set or not.
            public string[] commonUsages;
            public string displayName;
            public string resourceName;
            public string type; // This is mostly for when we turn arbitrary InputControlLayouts into JSON; less for layouts *coming* from JSON.
            public string variant;
            public InputDeviceMatcher.MatcherJson device;
            public ControlItemJson[] controls;

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore 0649

            public InputControlLayout ToLayout()
            {
                // By default, the type of the layout is determined from the first layout
                // in its 'extend' property chain that has a type set. However, if the layout
                // extends nothing, we can't know what type to use for it so we default to
                // InputDevice.
                Type type = null;
                if (!string.IsNullOrEmpty(this.type))
                {
                    type = Type.GetType(this.type, false);
                    if (type == null)
                    {
                        Debug.Log(string.Format(
                                "Cannot find type '{0}' used by layout '{1}'; falling back to using InputDevice",
                                this.type, name));
                        type = typeof(InputDevice);
                    }
                    else if (!typeof(InputControl).IsAssignableFrom(type))
                    {
                        throw new Exception(string.Format("'{0}' used by layout '{1}' is not an InputControl",
                                this.type, name));
                    }
                }
                else if (string.IsNullOrEmpty(extend))
                    type = typeof(InputDevice);

                // Create layout.
                var layout = new InputControlLayout(name, type);
                layout.m_ExtendsLayout = new InternedString(extend);
                layout.m_DeviceMatcher = device.ToMatcher();
                layout.m_DisplayName = displayName;
                layout.m_ResourceName = resourceName;
                layout.m_Variant = new InternedString(variant);
                if (!string.IsNullOrEmpty(format))
                    layout.m_StateFormat = new FourCC(format);

                if (!string.IsNullOrEmpty(beforeRender))
                {
                    var beforeRenderLowerCase = beforeRender.ToLower();
                    if (beforeRenderLowerCase == "ignore")
                        layout.m_UpdateBeforeRender = false;
                    else if (beforeRenderLowerCase == "update")
                        layout.m_UpdateBeforeRender = true;
                    else
                        throw new Exception(string.Format("Invalid beforeRender setting '{0}'", beforeRender));
                }

                // Add common usages.
                if (commonUsages != null)
                {
                    layout.m_CommonUsages = ArrayHelpers.Select(commonUsages, x => new InternedString(x));
                }

                // Add overrides.
                if (!string.IsNullOrEmpty(@override) || overrides != null)
                {
                    var names = new List<InternedString>();
                    if (!string.IsNullOrEmpty(@override))
                        names.Add(new InternedString(@override));
                    if (overrides != null)
                        names.AddRange(overrides.Select(x => new InternedString(x)));
                    layout.m_OverridesLayouts = names.ToArray();
                }

                // Add controls.
                if (controls != null)
                {
                    var controlLayouts = new List<ControlItem>();
                    foreach (var control in controls)
                    {
                        if (string.IsNullOrEmpty(control.name))
                            throw new Exception(string.Format("Control with no name in layout '{0}", name));
                        var controlLayout = control.ToLayout();
                        ThrowIfControlItemIsDuplicate(ref controlLayout, controlLayouts, layout.name);
                        controlLayouts.Add(controlLayout);
                    }
                    layout.m_Controls = controlLayouts.ToArray();
                }

                return layout;
            }

            public static LayoutJson FromLayout(InputControlLayout layout)
            {
                return new LayoutJson
                {
                    name = layout.m_Name,
                    type = layout.type.AssemblyQualifiedName,
                    variant = layout.m_Variant,
                    displayName = layout.m_DisplayName,
                    resourceName = layout.m_ResourceName,
                    extend = layout.m_ExtendsLayout,
                    format = layout.stateFormat.ToString(),
                    device = InputDeviceMatcher.MatcherJson.FromMatcher(layout.m_DeviceMatcher),
                    controls = ControlItemJson.FromControlItems(layout.m_Controls),
                };
            }
        }

        // This is a class instead of a struct so that we can assign 'offset' a custom
        // default value. Otherwise we can't tell whether the user has actually set it
        // or not (0 is a valid offset). Sucks, though, as we now get lots of allocations
        // from the control array.
        [Serializable]
        private class ControlItemJson
        {
            // Disable warnings that these fields are never assigned to. They are set
            // by JsonUtility.
            #pragma warning disable 0649
            // ReSharper disable MemberCanBePrivate.Local

            public string name;
            public string layout;
            public string variant;
            public string usage; // Convenince to not have to create array for single usage.
            public string alias; // Same.
            public string useStateFrom;
            public uint offset;
            public uint bit;
            public uint sizeInBits;
            public string format;
            public int arraySize;
            public string[] usages;
            public string[] aliases;
            public string parameters;
            public string processors;
            public string displayName;
            public string resourceName;
            public bool noisy;

            // ReSharper restore MemberCanBePrivate.Local
            #pragma warning restore 0649

            public ControlItemJson()
            {
                offset = InputStateBlock.kInvalidOffset;
                bit = InputStateBlock.kInvalidOffset;
            }

            public ControlItem ToLayout()
            {
                var layout = new ControlItem
                {
                    name = new InternedString(name),
                    layout = new InternedString(this.layout),
                    variant = new InternedString(variant),
                    displayName = displayName,
                    resourceName = resourceName,
                    offset = offset,
                    useStateFrom = useStateFrom,
                    bit = bit,
                    sizeInBits = sizeInBits,
                    isModifyingChildControlByPath = name.IndexOf('/') != -1,
                    isNoisy = noisy,
                    arraySize = arraySize,
                };

                if (!string.IsNullOrEmpty(format))
                    layout.format = new FourCC(format);

                if (!string.IsNullOrEmpty(usage) || usages != null)
                {
                    var usagesList = new List<string>();
                    if (!string.IsNullOrEmpty(usage))
                        usagesList.Add(usage);
                    if (usages != null)
                        usagesList.AddRange(usages);
                    layout.usages = new ReadOnlyArray<InternedString>(usagesList.Select(x => new InternedString(x)).ToArray());
                }

                if (!string.IsNullOrEmpty(alias) || aliases != null)
                {
                    var aliasesList = new List<string>();
                    if (!string.IsNullOrEmpty(alias))
                        aliasesList.Add(alias);
                    if (aliases != null)
                        aliasesList.AddRange(aliases);
                    layout.aliases = new ReadOnlyArray<InternedString>(aliasesList.Select(x => new InternedString(x)).ToArray());
                }

                if (!string.IsNullOrEmpty(parameters))
                    layout.parameters = new ReadOnlyArray<ParameterValue>(ParseParameters(parameters));

                if (!string.IsNullOrEmpty(processors))
                    layout.processors = new ReadOnlyArray<NameAndParameters>(ParseNameAndParameterList(processors));

                return layout;
            }

            public static ControlItemJson[] FromControlItems(ControlItem[] items)
            {
                if (items == null)
                    return null;

                var count = items.Length;
                var result = new ControlItemJson[count];

                for (var i = 0; i < count; ++i)
                {
                    var item = items[i];
                    result[i] = new ControlItemJson
                    {
                        name = item.name,
                        layout = item.layout,
                        variant = item.variant,
                        displayName = item.displayName,
                        resourceName = item.resourceName,
                        bit = item.bit,
                        offset = item.offset,
                        sizeInBits = item.sizeInBits,
                        format = item.format.ToString(),
                        parameters = string.Join(",", item.parameters.Select(x => x.ToString()).ToArray()),
                        processors = string.Join(",", item.processors.Select(x => x.ToString()).ToArray()),
                        usages = item.usages.Select(x => x.ToString()).ToArray(),
                        aliases = item.aliases.Select(x => x.ToString()).ToArray(),
                        noisy = item.isNoisy,
                        arraySize = item.arraySize,
                    };
                }

                return result;
            }
        }


        internal struct Collection
        {
            public Dictionary<InternedString, Type> layoutTypes;
            public Dictionary<InternedString, string> layoutStrings;
            public Dictionary<InternedString, BuilderInfo> layoutBuilders;
            public Dictionary<InternedString, InternedString> baseLayoutTable;
            public Dictionary<InternedString, InputDeviceMatcher> layoutDeviceMatchers;

            public void Allocate()
            {
                layoutTypes = new Dictionary<InternedString, Type>();
                layoutStrings = new Dictionary<InternedString, string>();
                layoutBuilders = new Dictionary<InternedString, BuilderInfo>();
                baseLayoutTable = new Dictionary<InternedString, InternedString>();
                layoutDeviceMatchers = new Dictionary<InternedString, InputDeviceMatcher>();
            }

            public InternedString TryFindMatchingLayout(InputDeviceDescription deviceDescription)
            {
                var highestScore = 0f;
                var highestScoringLayout = new InternedString();

                foreach (var entry in layoutDeviceMatchers)
                {
                    var score = entry.Value.MatchPercentage(deviceDescription);
                    if (score > highestScore)
                    {
                        highestScore = score;
                        highestScoringLayout = entry.Key;
                    }
                }

                return highestScoringLayout;
            }

            public bool HasLayout(InternedString name)
            {
                return layoutTypes.ContainsKey(name) || layoutStrings.ContainsKey(name) ||
                    layoutBuilders.ContainsKey(name);
            }

            private InputControlLayout TryLoadLayoutInternal(InternedString name)
            {
                // Check builders.
                BuilderInfo builder;
                if (layoutBuilders.TryGetValue(name, out builder))
                {
                    var layout = (InputControlLayout)builder.method.Invoke(builder.instance, null);
                    if (layout == null)
                        throw new Exception(string.Format("Layout builder '{0}' returned null when invoked", name));
                    return layout;
                }

                // See if we have a string layout for it. These
                // always take precedence over ones from type so that we can
                // override what's in the code using data.
                string json;
                if (layoutStrings.TryGetValue(name, out json))
                    return FromJson(json);

                // No, but maybe we have a type layout for it.
                Type type;
                if (layoutTypes.TryGetValue(name, out type))
                    return FromType(name, type);

                return null;
            }

            public InputControlLayout TryLoadLayout(InternedString name, Dictionary<InternedString, InputControlLayout> table = null)
            {
                var layout = TryLoadLayoutInternal(name);
                if (layout != null)
                {
                    layout.m_Name = name;
                    if (table != null)
                        table[name] = layout;

                    // If the layout extends another layout, we need to merge the
                    // base layout into the final layout.
                    // NOTE: We go through the baseLayoutTable here instead of looking at
                    //       the extendsLayout property so as to make this work for all types
                    //       of layouts (FromType() does not set the property, for example).
                    var baseLayoutName = new InternedString();
                    if (baseLayoutTable.TryGetValue(name, out baseLayoutName))
                    {
                        ////TODO: catch cycles
                        var baseLayout = TryLoadLayout(baseLayoutName, table);
                        if (baseLayout == null)
                            throw new LayoutNotFoundException(string.Format(
                                    "Cannot find base layout '{0}' of layout '{1}'", baseLayoutName, name));
                        layout.MergeLayout(baseLayout);
                        layout.m_ExtendsLayout = baseLayoutName;
                    }

                    // If the layout has an associated device matcher,
                    // put it on the layout instance.
                    InputDeviceMatcher deviceMatcher;
                    if (layoutDeviceMatchers.TryGetValue(name, out deviceMatcher))
                        layout.m_DeviceMatcher = deviceMatcher;
                }

                return layout;
            }

            // Return name of layout at root of "extend" chain of given layout.
            public InternedString GetRootLayoutName(InternedString layoutName)
            {
                InternedString baseLayout;
                while (baseLayoutTable.TryGetValue(layoutName, out baseLayout))
                    layoutName = baseLayout;
                return layoutName;
            }

            // Get the type which will be instantiated for the given layout.
            // Returns null if no layout with the given name exists.
            public Type GetControlTypeForLayout(InternedString layoutName)
            {
                // Try layout strings.
                while (layoutStrings.ContainsKey(layoutName))
                {
                    InternedString baseLayout;
                    if (baseLayoutTable.TryGetValue(layoutName, out baseLayout))
                    {
                        // Work our way up the inheritance chain.
                        layoutName = baseLayout;
                    }
                    else
                    {
                        // Layout doesn't extend anything and ATM we don't support setting
                        // types explicitly from JSON layouts. So has to be InputDevice.
                        return typeof(InputDevice);
                    }
                }

                // Try layout types.
                Type result;
                layoutTypes.TryGetValue(layoutName, out result);
                return result;
            }
        }

        // This collection is owned and managed by InputManager.
        internal static Collection s_Layouts;

        internal struct BuilderInfo
        {
            public MethodInfo method;
            public object instance;
        }

        internal class LayoutNotFoundException : Exception
        {
            public string layout { get; private set; }
            public LayoutNotFoundException(string name, string message = null)
                : base(message ?? string.Format("Cannot find control layout '{0}'", name))
            {
                layout = name;
            }
        }

        // Constructs InputControlLayout instances and caches them.
        internal struct Cache
        {
            public Collection layouts;
            public Dictionary<InternedString, InputControlLayout> table;

            public InputControlLayout FindOrLoadLayout(string name)
            {
                var internedName = new InternedString(name);

                // See if we have it cached.
                InputControlLayout layout;
                if (table != null && table.TryGetValue(internedName, out layout))
                    return layout;

                if (table == null)
                    table = new Dictionary<InternedString, InputControlLayout>();

                layout = layouts.TryLoadLayout(internedName, table);
                if (layout != null)
                    return layout;

                // Nothing.
                throw new LayoutNotFoundException(name);
            }
        }
    }
}
