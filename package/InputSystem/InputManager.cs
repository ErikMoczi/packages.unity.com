using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Experimental.Input.Composites;
using UnityEngine.Experimental.Input.Controls;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Modifiers;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections;
#if !(NET_4_0 || NET_4_6)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////TODO: allow pushing events into the system any which way; decouple from the buffer in NativeInputSystem being the only source

////TODO: merge InputManager into InputSystem and have InputSystemObject store SerializedState directly

////REVIEW: change the event properties over to using IObservable?

////REVIEW: instead of RegisterBindingModifier and RegisterControlProcessor, have a generic RegisterInterface (or something)?

namespace UnityEngine.Experimental.Input
{
    using DeviceChangeListener = Action<InputDevice, InputDeviceChange>;
    using LayoutChangeListener = Action<string, InputControlLayoutChange>;
    using EventListener = Action<InputEventPtr>;
    using UpdateListener = Action<InputUpdateType>;

    public delegate string DeviceFindControlLayoutCallback(int deviceId, ref InputDeviceDescription description, string matchedLayout,
        IInputRuntime runtime);

    // The hub of the input system.
    // All state is ultimately gathered here.
    // Not exposed. Use InputSystem as the public entry point to the system.
#if UNITY_EDITOR
    [Serializable]
#endif
    internal class InputManager
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        public ReadOnlyArray<InputDevice> devices
        {
            get { return new ReadOnlyArray<InputDevice>(m_Devices); }
        }

        public TypeTable processors
        {
            get { return m_Processors; }
        }

        public TypeTable modifiers
        {
            get { return m_Modifiers; }
        }

        public TypeTable composites
        {
            get { return m_Composites; }
        }

        public InputUpdateType updateMask
        {
            get { return m_UpdateMask; }
            set
            {
                if (m_UpdateMask == value)
                    return;

                m_UpdateMask = value;

                // Tell runtime.
                if (m_Runtime != null)
                    m_Runtime.updateMask = m_UpdateMask;

                // Recreate state buffers.
                if (m_Devices != null)
                    ReallocateStateBuffers();
            }
        }

        public event DeviceChangeListener onDeviceChange
        {
            add { m_DeviceChangeListeners.Append(value); }
            remove { m_DeviceChangeListeners.Remove(value); }
        }

        public event DeviceFindControlLayoutCallback onFindControlLayoutForDevice
        {
            add { m_DeviceFindLayoutCallbacks.Append(value); }
            remove { m_DeviceFindLayoutCallbacks.Remove(value); }
        }

        public event LayoutChangeListener onLayoutChange
        {
            add { m_LayoutChangeListeners.Append(value); }
            remove { m_LayoutChangeListeners.Remove(value); }
        }

        ////TODO: add InputEventBuffer struct that uses NativeArray underneath
        ////TODO: make InputEventTrace use NativeArray
        ////TODO: introduce an alternative that consumes events in bulk
        public event EventListener onEvent
        {
            add { m_EventListeners.Append(value); }
            remove { m_EventListeners.Remove(value); }
        }

        public event UpdateListener onUpdate
        {
            add
            {
                InstallBeforeUpdateHookIfNecessary();
                m_UpdateListeners.Append(value);
            }
            remove { m_UpdateListeners.Remove(value); }
        }

        ////TODO: when registering a layout that exists as a layout of a different type (type vs string vs constructor),
        ////      remove the existing registration

        // Add a layout constructed from a type.
        // If a layout with the same name already exists, the new layout
        // takes its place.
        public void RegisterControlLayout(string name, Type type, InputDeviceMatcher? deviceMatcher = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");
            if (type == null)
                throw new ArgumentNullException("type");

            // Note that since InputDevice derives from InputControl, isDeviceLayout implies
            // isControlLayout to be true as well.
            var isDeviceLayout = typeof(InputDevice).IsAssignableFrom(type);
            var isControlLayout = typeof(InputControl).IsAssignableFrom(type);

            if (!isDeviceLayout && !isControlLayout)
                throw new ArgumentException("Types used as layouts have to be InputControls or InputDevices",
                    "type");

            var internedName = new InternedString(name);
            var isReplacement = DoesLayoutExist(internedName);

            // All we do is enter the type into a map. We don't construct an InputControlLayout
            // from it until we actually need it in an InputDeviceBuilder to create a device.
            // This not only avoids us creating a bunch of objects on the managed heap but
            // also avoids us laboriously constructing a XRController layout, for example,
            // in a game that never uses XR.
            m_Layouts.layoutTypes[internedName] = type;

            ////TODO: make this independent of initialization order
            ////TODO: re-scan base type information after domain reloads

            // Walk class hierarchy all the way up to InputControl to see
            // if there's another type that's been registered as a layhout.
            // If so, make it a base layout for this one.
            string baseLayout = null;
            for (var baseType = type.BaseType; baseLayout == null && baseType != typeof(InputControl);
                 baseType = baseType.BaseType)
            {
                foreach (var entry in m_Layouts.layoutTypes)
                    if (entry.Value == baseType)
                    {
                        baseLayout = entry.Key;
                        break;
                    }
            }

            PerformLayoutPostRegistration(internedName, baseLayout, deviceMatcher, isReplacement,
                isKnownToBeDeviceLayout: isDeviceLayout);
        }

        ////TODO: nuke namespace argument
        // Add a layout constructed from a JSON string.
        public void RegisterControlLayout(string json, string name = null, string @namespace = null, InputDeviceMatcher? matcher = null)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("json");

            ////REVIEW: as long as no one has instantiated the layout, the base layout information is kinda pointless

            // Parse out name, device description, and base layout.
            InputDeviceMatcher deviceMatcher;
            string baseLayout;
            var nameFromJson = InputControlLayout.ParseHeaderFromJson(json, out deviceMatcher, out baseLayout);

            // If we have explicity been given a matcher, override the one
            // from JSON (if it even has one).
            if (matcher.HasValue)
                deviceMatcher = matcher.Value;

            // Decide whether to take name from JSON or from code.
            if (string.IsNullOrEmpty(name))
            {
                name = nameFromJson;

                // Make sure we have a name.
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Layout name has not been given and is not set in JSON layout",
                        "name");
            }

            if (@namespace != null)
            {
                name = string.Format("{0}::{1}", @namespace, name);
                if (!string.IsNullOrEmpty(baseLayout))
                    baseLayout = string.Format("{0}::{1}", @namespace, baseLayout);
            }

            var internedName = new InternedString(name);
            var isReplacement = DoesLayoutExist(internedName);

            // Add it to our records.
            m_Layouts.layoutStrings[internedName] = json;

            PerformLayoutPostRegistration(internedName, baseLayout, deviceMatcher, isReplacement);
        }

        public void RegisterControlLayoutBuilder(MethodInfo method, object instance, string name,
            string baseLayout = null, InputDeviceMatcher? deviceMatcher = null)
        {
            if (method == null)
                throw new ArgumentNullException("method");
            if (method.IsGenericMethod)
                throw new ArgumentException(string.Format("Method must not be generic ({0})", method), "method");
            if (method.GetParameters().Length > 0)
                throw new ArgumentException(string.Format("Method must not take arguments ({0})", method), "method");
            if (!typeof(InputControlLayout).IsAssignableFrom(method.ReturnType))
                throw new ArgumentException(string.Format("Method must return InputControlLayout ({0})", method), "method");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");

            // If we have an instance, make sure it is [Serializable].
            if (instance != null)
            {
                var type = instance.GetType();
                if (type.GetCustomAttribute<SerializableAttribute>(true) == null)
                    throw new ArgumentException(
                        string.Format(
                            "Instance used with {0} to construct a layout must be [Serializable] but {1} is not",
                            method, type),
                        "instance");
            }

            var internedName = new InternedString(name);
            var isReplacement = DoesLayoutExist(internedName);

            m_Layouts.layoutBuilders[internedName] = new InputControlLayout.BuilderInfo
            {
                method = method,
                instance = instance
            };

            PerformLayoutPostRegistration(internedName, baseLayout, deviceMatcher, isReplacement);
        }

        private void PerformLayoutPostRegistration(InternedString name, string baseLayout,
            InputDeviceMatcher? deviceMatcher, bool isReplacement, bool isKnownToBeDeviceLayout = false)
        {
            ++m_LayoutRegistrationVersion;

            if (!string.IsNullOrEmpty(baseLayout))
                m_Layouts.baseLayoutTable[name] = new InternedString(baseLayout);

            // Re-create any devices using the layout.
            RecreateDevicesUsingLayout(name, isKnownToBeDeviceLayout: isKnownToBeDeviceLayout);

            // If the layout has a device matcher, see if it allows us
            // to make sense of any device we couldn't make sense of so far.
            if (deviceMatcher != null && !deviceMatcher.Value.empty)
                AddSupportedDevice(deviceMatcher.Value, name);

            // Let listeners know.
            var change = isReplacement ? InputControlLayoutChange.Replaced : InputControlLayoutChange.Added;
            for (var i = 0; i < m_LayoutChangeListeners.Count; ++i)
                m_LayoutChangeListeners[i](name.ToString(), change);
        }

        private void AddSupportedDevice(InputDeviceMatcher matcher, InternedString layout)
        {
            m_Layouts.layoutDeviceMatchers[layout] = matcher;

            // See if the new description to layout mapping allows us to make
            // sense of a device we couldn't make sense of so far.
            for (var i = 0; i < m_AvailableDevices.Count; ++i)
            {
                var deviceId = m_AvailableDevices[i].deviceId;
                if (TryGetDeviceById(deviceId) != null)
                    continue;

                if (matcher.MatchPercentage(m_AvailableDevices[i].description) > 0f)
                {
                    // Re-enable device.
                    var command = EnableDeviceCommand.Create();
                    m_Runtime.DeviceCommand(deviceId, ref command);

                    // Create InputDevice instance.
                    AddDevice(layout, deviceId, m_AvailableDevices[i].description, m_AvailableDevices[i].isNative);
                }
            }
        }

        private void RecreateDevicesUsingLayout(InternedString layout, bool isKnownToBeDeviceLayout = false)
        {
            if (m_Devices == null)
                return;

            List<InputDevice> devicesUsingLayout = null;

            // Find all devices using the layout.
            for (var i = 0; i < m_Devices.Length; ++i)
            {
                var device = m_Devices[i];

                bool usesLayout;
                if (isKnownToBeDeviceLayout)
                    usesLayout = IsControlUsingLayout(device, layout);
                else
                    usesLayout = IsControlOrChildUsingLayoutRecursive(device, layout);

                if (usesLayout)
                {
                    if (devicesUsingLayout == null)
                        devicesUsingLayout = new List<InputDevice>();
                    devicesUsingLayout.Add(device);
                }
            }

            // If there's none, we're good.
            if (devicesUsingLayout == null)
                return;

            // Remove and re-add the matching devices.
            var setup = new InputDeviceBuilder(m_Layouts);
            for (var i = 0; i < devicesUsingLayout.Count; ++i)
            {
                var device = devicesUsingLayout[i];

                ////TODO: preserve state where possible

                // Remove.
                RemoveDevice(device);

                // Re-setup device.
                setup.Setup(device.m_Layout, device, device.m_Variant);
                var newDevice = setup.Finish();

                // Re-add.
                AddDevice(newDevice);
            }
        }

        private bool IsControlOrChildUsingLayoutRecursive(InputControl control, InternedString layout)
        {
            // Check control itself.
            if (IsControlUsingLayout(control, layout))
                return true;

            // Check children.
            var children = control.children;
            for (var i = 0; i < children.Count; ++i)
                if (IsControlOrChildUsingLayoutRecursive(children[i], layout))
                    return true;

            return false;
        }

        private bool IsControlUsingLayout(InputControl control, InternedString layout)
        {
            // Check direct match.
            if (control.layout == layout)
                return true;

            // Check base layout chain.
            var baseLayout = control.m_Layout;
            while (m_Layouts.baseLayoutTable.TryGetValue(baseLayout, out baseLayout))
                if (baseLayout == layout)
                    return true;

            return false;
        }

        public void RemoveControlLayout(string name, string @namespace = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");

            if (@namespace != null)
                name = string.Format("{0}::{1}", @namespace, name);

            var internedName = new InternedString(name);

            // Remove all devices using the layout.
            for (var i = 0; m_Devices != null && i < m_Devices.Length;)
            {
                var device = m_Devices[i];
                if (IsControlOrChildUsingLayoutRecursive(device, internedName))
                {
                    RemoveDevice(device);
                }
                else
                {
                    ++i;
                }
            }

            // Remove layout record.
            m_Layouts.layoutTypes.Remove(internedName);
            m_Layouts.layoutStrings.Remove(internedName);
            m_Layouts.layoutBuilders.Remove(internedName);
            m_Layouts.baseLayoutTable.Remove(internedName);

            ////TODO: check all layout inheritance chain for whether they are based on the layout and if so
            ////      remove those layouts, too

            // Let listeners know.
            for (var i = 0; i < m_LayoutChangeListeners.Count; ++i)
                m_LayoutChangeListeners[i](name, InputControlLayoutChange.Removed);
        }

        public InputControlLayout TryLoadControlLayout(InternedString name)
        {
            return m_Layouts.TryLoadLayout(name);
        }

        public string TryFindMatchingControlLayout(InputDeviceDescription deviceDescription)
        {
            ////TODO: this will want to take overrides into account

            // See if we can match by description.
            var layoutName = m_Layouts.TryFindMatchingLayout(deviceDescription);
            if (!layoutName.IsEmpty())
                return layoutName;

            // No, so try to match by device class. If we have a "Gamepad" layout,
            // for example, a device that classifies itself as a "Gamepad" will match
            // that layout.
            //
            // NOTE: Have to make sure here that we get a device layout and not a
            //       control layout.
            if (!string.IsNullOrEmpty(deviceDescription.deviceClass))
            {
                var deviceClassLowerCase = new InternedString(deviceDescription.deviceClass);
                var type = m_Layouts.GetControlTypeForLayout(deviceClassLowerCase);
                if (type != null && typeof(InputDevice).IsAssignableFrom(type))
                    return deviceDescription.deviceClass;
            }

            return null;
        }

        private bool DoesLayoutExist(InternedString name)
        {
            return m_Layouts.layoutTypes.ContainsKey(name) ||
                m_Layouts.layoutStrings.ContainsKey(name) ||
                m_Layouts.layoutBuilders.ContainsKey(name);
        }

        public int ListControlLayouts(List<string> layouts)
        {
            if (layouts == null)
                throw new ArgumentNullException("layouts");

            var countBefore = layouts.Count;

            ////FIXME: this may add a name twice; also allocates

            layouts.AddRange(m_Layouts.layoutTypes.Keys.Select(x => x.ToString()));
            layouts.AddRange(m_Layouts.layoutStrings.Keys.Select(x => x.ToString()));
            layouts.AddRange(m_Layouts.layoutBuilders.Keys.Select(x => x.ToString()));

            return layouts.Count - countBefore;
        }

        // Processes a path specification that may match more than a single control.
        // Adds all controls that match to the given list.
        // Returns true if at least one control was matched.
        // Must not generate garbage!
        public bool TryGetControls(string path, List<InputControl> controls)
        {
            throw new NotImplementedException();
        }

        // Return the first match for the given path or null if no control matches.
        // Must not generate garbage!
        public InputControl TryGetControl(string path)
        {
            throw new NotImplementedException();
        }

        public InputControl GetControl(string path)
        {
            throw new NotImplementedException();
        }

        // Adds all controls that match the given path spec to the given list.
        // Returns number of controls added to the list.
        // NOTE: Does not create garbage.
        public int GetControls(string path, ref ArrayOrListWrapper<InputControl> controls)
        {
            if (string.IsNullOrEmpty(path))
                return 0;
            if (m_Devices == null)
                return 0;

            var deviceCount = m_Devices.Length;
            var numMatches = 0;
            for (var i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                numMatches += InputControlPath.TryFindControls(device, path, 0, ref controls);
            }

            return numMatches;
        }

        public void SetLayoutVariant(InputControl control, string variant)
        {
            if (control == null)
                throw new ArgumentNullException("control");
            if (string.IsNullOrEmpty(variant))
                variant = "Default";

            //how can we do this efficiently without having to take the control's device out of the system?

            throw new NotImplementedException();
        }

        public void SetUsage(InputDevice device, InternedString usage)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            device.SetUsage(usage);

            // Notify listeners.
            for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.UsageChanged);

            // Usage may affect current device so update.
            device.MakeCurrent();
        }

        ////TODO: make sure that no device or control with a '/' in the name can creep into the system

        public InputDevice AddDevice(Type type, string name = null)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // Find the layout name that the given type was registered with.
            // First just try the name of the type and see if that produces a hit.
            var layoutName = new InternedString(type.Name);
            Type registeredType;
            if (!m_Layouts.layoutTypes.TryGetValue(layoutName, out registeredType)
                || registeredType != type)
            {
                // Didn't produce a hit so crawl through all registered layout types
                // and look for a match.
                layoutName = new InternedString();
                foreach (var entry in m_Layouts.layoutTypes)
                {
                    if (entry.Value == type)
                    {
                        layoutName = entry.Key;
                        break;
                    }
                }

                if (layoutName.IsEmpty())
                    throw new ArgumentException(string.Format("Cannot find layout registered for type '{0}'", type.Name),
                        "type");
            }

            Debug.Assert(!layoutName.IsEmpty(), name);

            // Note that since we go through the normal by-name lookup here, this will
            // still work if the layout from the type was override with a string layout.
            return AddDevice(layoutName);
        }

        // Creates a device from the given layout and adds it to the system.
        // NOTE: Creates garbage.
        public InputDevice AddDevice(string layout, string name = null)
        {
            if (string.IsNullOrEmpty(layout))
                throw new ArgumentException("layout");

            var internedLayoutName = new InternedString(layout);

            var setup = new InputDeviceBuilder(m_Layouts);
            setup.Setup(internedLayoutName, null, new InternedString());
            var device = setup.Finish();

            if (!string.IsNullOrEmpty(name))
                device.m_Name = new InternedString(name);

            AddDevice(device);

            return device;
        }

        // Add device with a forced ID. Used when creating devices reported to us by native.
        private InputDevice AddDevice(string layout, int deviceId, InputDeviceDescription description, bool isNative)
        {
            var setup = new InputDeviceBuilder(m_Layouts);
            setup.SetupWithDescription(new InternedString(layout), description, new InternedString());
            var device = setup.Finish();

            device.m_Id = deviceId;
            device.m_Description = description;

            // Default display name to product name.
            if (!string.IsNullOrEmpty(description.product))
                device.m_DisplayName = description.product;

            if (isNative)
                device.m_Flags |= InputDevice.Flags.Native;

            AddDevice(device);

            return device;
        }

        public void AddDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(device.layout))
                throw new ArgumentException("Device has no associated layout", "device");

            // Ignore if the same device gets added multiple times.
            if (ArrayHelpers.Contains(m_Devices, device))
                return;

            MakeDeviceNameUnique(device);
            AssignUniqueDeviceId(device);

            // Add to list.
            device.m_DeviceIndex = ArrayHelpers.Append(ref m_Devices, device);

            ////REVIEW: Not sure a full-blown dictionary is the right way here. Alternatives are to keep
            ////        a sparse array that directly indexes using the linearly increasing IDs (though that
            ////        may get large over time). Or to just do a linear search through m_Devices (but
            ////        that may end up tapping a bunch of memory locations in the heap to find the right
            ////        device; could be improved by sorting m_Devices by ID and picking a good starting
            ////        point based on the ID we have instead of searching from [0] always).
            m_DevicesById[device.id] = device;

            // Let InputStateBuffers know this device doesn't have any associated state yet.
            device.m_StateBlock.byteOffset = InputStateBlock.kInvalidOffset;

            // Let InputStateBuffers allocate state buffers.
            ReallocateStateBuffers();

            // Let actions re-resolve their paths.
            InputActionSet.RefreshAllEnabledActions();

            // If the device wants automatic callbacks before input updates,
            // put it on the list.
            var beforeUpdateCallbackReceiver = device as IInputUpdateCallbackReceiver;
            if (beforeUpdateCallbackReceiver != null)
                onUpdate += beforeUpdateCallbackReceiver.OnUpdate;

            // If the device has state callbacks, make a note of it.
            var stateCallbackReceiver = device as IInputStateCallbackReceiver;
            if (stateCallbackReceiver != null)
            {
                InstallBeforeUpdateHookIfNecessary();
                device.m_Flags |= InputDevice.Flags.HasStateCallbacks;
                m_HaveDevicesWithStateCallbackReceivers = true;
            }

            // If the device wants before-render updates, enable them if they
            // aren't already.
            if (device.updateBeforeRender)
                updateMask |= InputUpdateType.BeforeRender;

            // Notify device.
            device.NotifyAdded();

            // Make the device current.
            device.MakeCurrent();

            // Notify listeners.
            for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.Added);
        }

        public InputDevice AddDevice(InputDeviceDescription description)
        {
            return AddDevice(description, throwIfNoLayoutFound: true);
        }

        public InputDevice AddDevice(InputDeviceDescription description, bool throwIfNoLayoutFound, int deviceId = InputDevice.kInvalidDeviceId, bool isNative = false)
        {
            // Look for matching layout.
            var layout = TryFindMatchingControlLayout(description);

            ////REVIEW: listeners registering new layouts from in here may potentially lead to the creation of devices; should we disallow that?
            // Give listeners a shot to select/create a layout.
            for (var i = 0; i < m_DeviceFindLayoutCallbacks.Count; ++i)
            {
                var newLayout = m_DeviceFindLayoutCallbacks[i](deviceId, ref description, layout, m_Runtime);
                if (!string.IsNullOrEmpty(newLayout))
                {
                    layout = newLayout;
                    break;
                }
            }

            // If no layout was found, bail out.
            if (layout == null)
            {
                if (throwIfNoLayoutFound)
                    throw new ArgumentException(string.Format("Cannot find layout matching device description '{0}'", description), "description");

                // If it's a device coming from the runtime, disable it.
                if (deviceId != InputDevice.kInvalidDeviceId)
                {
                    var command = DisableDeviceCommand.Create();
                    m_Runtime.DeviceCommand(deviceId, ref command);
                }

                return null;
            }

            var device = AddDevice(layout, deviceId, description, isNative);
            device.m_Description = description;

            return device;
        }

        public void RemoveDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            // If device has not been added, ignore.
            if (device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                return;

            // Remove state monitors while device index is still valid.
            RemoveStateChangeMonitors(device);

            // Remove from device array.
            var deviceIndex = device.m_DeviceIndex;
            var deviceId = device.id;
            ArrayHelpers.EraseAt(ref m_Devices, deviceIndex);
            device.m_DeviceIndex = InputDevice.kInvalidDeviceIndex;
            m_DevicesById.Remove(deviceId);

            if (m_Devices != null)
            {
                var oldDeviceIndices = new int[m_Devices.Length];
                for (var i = 0; i < m_Devices.Length; ++i)
                {
                    oldDeviceIndices[i] = m_Devices[i].m_DeviceIndex;
                    m_Devices[i].m_DeviceIndex = i;
                }

                // Remove from state buffers.
                ReallocateStateBuffers(oldDeviceIndices);
            }
            else
            {
                // No more devices. Kill state buffers.
                m_StateBuffers.FreeAll();
            }

            // Remove from list of available devices if it's a device coming from
            // the runtime.
            if (device.native)
            {
                for (var i = 0; i < m_AvailableDevices.Count; ++i)
                {
                    if (m_AvailableDevices[i].deviceId == deviceId)
                    {
                        m_AvailableDevices.RemoveAt(i);
                        break;
                    }
                }
            }

            // Unbake offset into global state buffers.
            device.BakeOffsetIntoStateBlockRecursive((uint)(-device.m_StateBlock.byteOffset));

            // Force enabled actions to remove controls from the device.
            // We've already set the device index to be invalid so we any attempts
            // by actions to uninstall state monitors will get ignored.
            InputActionSet.RefreshAllEnabledActions();

            // Kill before update callback, if applicable.
            var beforeUpdateCallbackReceiver = device as IInputUpdateCallbackReceiver;
            if (beforeUpdateCallbackReceiver != null)
                onUpdate -= beforeUpdateCallbackReceiver.OnUpdate;

            // Disable before-render updates if this was the last device
            // that requires them.
            if (device.updateBeforeRender)
            {
                var haveDeviceRequiringBeforeRender = false;
                if (m_Devices != null)
                    for (var i = 0; i < m_Devices.Length; ++i)
                        if (m_Devices[i].updateBeforeRender)
                        {
                            haveDeviceRequiringBeforeRender = true;
                            break;
                        }

                if (!haveDeviceRequiringBeforeRender)
                    updateMask &= ~InputUpdateType.BeforeRender;
            }

            // Let device know.
            device.NotifyRemoved();

            // Let listeners know.
            for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                m_DeviceChangeListeners[i](device, InputDeviceChange.Removed);
        }

        public InputDevice TryGetDevice(string nameOrLayout)
        {
            if (string.IsNullOrEmpty(nameOrLayout))
                throw new ArgumentException("nameOrLayout");

            if (m_Devices == null)
                return null;

            var nameOrLayoutLowerCase = nameOrLayout.ToLower();

            for (var i = 0; i < m_Devices.Length; ++i)
            {
                var device = m_Devices[i];
                if (device.m_Name.ToLower() == nameOrLayoutLowerCase ||
                    device.m_Layout.ToLower() == nameOrLayoutLowerCase)
                    return device;
            }

            return null;
        }

        public InputDevice GetDevice(string nameOrLayout)
        {
            var device = TryGetDevice(nameOrLayout);
            if (device == null)
                throw new Exception(string.Format("Cannot find device with name or layout '{0}'", nameOrLayout));

            return device;
        }

        public InputDevice TryGetDeviceById(int id)
        {
            InputDevice result;
            if (m_DevicesById.TryGetValue(id, out result))
                return result;
            return null;
        }

        // Adds any device that's been reported to the system but could not be matched to
        // a layout to the given list.
        public int GetUnsupportedDevices(List<InputDeviceDescription> descriptions)
        {
            if (descriptions == null)
                throw new ArgumentNullException("descriptions");

            var numFound = 0;
            for (var i = 0; i < m_AvailableDevices.Count; ++i)
            {
                if (TryGetDeviceById(m_AvailableDevices[i].deviceId) != null)
                    continue;

                descriptions.Add(m_AvailableDevices[i].description);
                ++numFound;
            }

            return numFound;
        }

        private void ReportAvailableDevice(InputDeviceDescription description, int deviceId, bool isNative = false)
        {
            try
            {
                // Try to turn it into a device instance.
                AddDevice(description, throwIfNoLayoutFound: false, deviceId: deviceId, isNative: isNative);
            }
            finally
            {
                // Remember it. Do this *after* the AddDevice() call above so that if there's
                // a listener creating layouts on the fly we won't end up matching this device and
                // create an InputDevice right away (which would then conflict with the one we
                // create in AddDevice).
                m_AvailableDevices.Add(new AvailableDevice
                {
                    description = description,
                    deviceId = deviceId,
                    isNative = true
                });
            }
        }

        public void EnableOrDisableDevice(InputDevice device, bool enable)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            // Ignore if device already enabled/disabled.
            if (device.enabled == enable)
                return;

            // Set/clear flag.
            if (!enable)
                device.m_Flags |= InputDevice.Flags.Disabled;
            else
                device.m_Flags &= ~InputDevice.Flags.Disabled;

            // Send command to tell backend about status change.
            if (enable)
            {
                var command = EnableDeviceCommand.Create();
                device.ExecuteCommand(ref command);
            }
            else
            {
                var command = DisableDeviceCommand.Create();
                device.ExecuteCommand(ref command);
            }

            // Let listeners know.
            var deviceChange = enable ? InputDeviceChange.Enabled : InputDeviceChange.Disabled;
            for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                m_DeviceChangeListeners[i](device, deviceChange);
        }

        public void QueueEvent(InputEventPtr ptr)
        {
            m_Runtime.QueueEvent(ptr.data);
        }

        public unsafe void QueueEvent<TEvent>(ref TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            // Don't bother keeping the data on the managed side. Just stuff the raw data directly
            // into the native buffers. This also means this method is thread-safe.
            m_Runtime.QueueEvent((IntPtr)UnsafeUtility.AddressOf(ref inputEvent));
        }

        public void Update()
        {
            Update(InputUpdateType.Dynamic);
        }

        public void Update(InputUpdateType updateType)
        {
            m_Runtime.Update(updateType);
        }

        internal void Initialize(IInputRuntime runtime)
        {
            InitializeData();
            InstallRuntime(runtime);
            InstallGlobals();
        }

        internal void Destroy()
        {
            // We don't destroy devices here and don't release state buffers.
            // See InputSystem.Restore() for an explanation why.
            // However, we still want them to clear out statics so notify each device it
            // got removed.
            if (m_Devices != null)
                foreach (var device in m_Devices)
                    device.NotifyRemoved();

            // Uninstall globals.
            if (ReferenceEquals(InputControlLayout.s_Layouts.baseLayoutTable, m_Layouts.baseLayoutTable))
                InputControlLayout.s_Layouts = new InputControlLayout.Collection();
            if (ReferenceEquals(InputControlProcessor.s_Processors.table, m_Processors.table))
                InputControlProcessor.s_Processors = new TypeTable();
            if (ReferenceEquals(InputBindingModifier.s_Modifiers.table, m_Modifiers.table))
                InputBindingModifier.s_Modifiers = new TypeTable();
            if (ReferenceEquals(InputBindingComposite.s_Composites.table, m_Composites.table))
                InputBindingComposite.s_Composites = new TypeTable();

            // Detach from runtime.
            if (m_Runtime != null)
            {
                m_Runtime.onUpdate = null;
                m_Runtime.onDeviceDiscovered = null;
                m_Runtime.onBeforeUpdate = null;

                if (ReferenceEquals(InputRuntime.s_Instance, m_Runtime))
                    InputRuntime.s_Instance = null;
            }
        }

        internal void InitializeData()
        {
            m_Layouts.Allocate();
            m_Processors.Initialize();
            m_Modifiers.Initialize();
            m_Composites.Initialize();
            m_DevicesById = new Dictionary<int, InputDevice>();
            m_AvailableDevices = new List<AvailableDevice>();

            // Determine our default set of enabled update types. By
            // default we enable both fixed and dynamic update because
            // we don't know which one the user is going to use. The user
            // can manually turn off one of them to optimize operation.
            m_UpdateMask = InputUpdateType.Dynamic | InputUpdateType.Fixed;
#if UNITY_EDITOR
            m_UpdateMask |= InputUpdateType.Editor;
#endif

            // Register layouts.
            RegisterControlLayout("Button", typeof(ButtonControl)); // Controls.
            RegisterControlLayout("DiscreteButton", typeof(DiscreteButtonControl));
            RegisterControlLayout("Key", typeof(KeyControl));
            RegisterControlLayout("Axis", typeof(AxisControl));
            RegisterControlLayout("Analog", typeof(AxisControl));
            RegisterControlLayout("Digital", typeof(IntegerControl));
            RegisterControlLayout("Integer", typeof(IntegerControl));
            RegisterControlLayout("PointerPhase", typeof(PointerPhaseControl));
            RegisterControlLayout("TouchType", typeof(TouchTypeControl));
            RegisterControlLayout("Vector2", typeof(Vector2Control));
            RegisterControlLayout("Vector3", typeof(Vector3Control));
            RegisterControlLayout("Magnitude2", typeof(Magnitude2Control));
            RegisterControlLayout("Magnitude3", typeof(Magnitude3Control));
            RegisterControlLayout("Quaternion", typeof(QuaternionControl));
            RegisterControlLayout("Pose", typeof(PoseControl));
            RegisterControlLayout("Stick", typeof(StickControl));
            RegisterControlLayout("Dpad", typeof(DpadControl));
            RegisterControlLayout("AnyKey", typeof(AnyKeyControl));
            RegisterControlLayout("Touch", typeof(TouchControl));
            RegisterControlLayout("Color", typeof(ColorControl));
            RegisterControlLayout("Audio", typeof(AudioControl));

            RegisterControlLayout("Gamepad", typeof(Gamepad)); // Devices.
            RegisterControlLayout("Joystick", typeof(Joystick));
            RegisterControlLayout("Keyboard", typeof(Keyboard));
            RegisterControlLayout("Pointer", typeof(Pointer));
            RegisterControlLayout("Mouse", typeof(Mouse));
            RegisterControlLayout("Pen", typeof(Pen));
            RegisterControlLayout("Touchscreen", typeof(Touchscreen));
            RegisterControlLayout("Sensor", typeof(Sensor));
            RegisterControlLayout("Accelerometer", typeof(Accelerometer));
            RegisterControlLayout("Gyroscope", typeof(Gyroscope));
            RegisterControlLayout("Gravity", typeof(Gravity));
            RegisterControlLayout("Attitude", typeof(Attitude));
            RegisterControlLayout("LinearAcceleration", typeof(LinearAcceleration));

            // Register processors.
            processors.AddTypeRegistration("Invert", typeof(InvertProcessor));
            processors.AddTypeRegistration("Clamp", typeof(ClampProcessor));
            processors.AddTypeRegistration("Normalize", typeof(NormalizeProcessor));
            processors.AddTypeRegistration("Deadzone", typeof(DeadzoneProcessor));
            processors.AddTypeRegistration("Curve", typeof(CurveProcessor));
            processors.AddTypeRegistration("Sensitivity", typeof(SensitivityProcessor));

            #if UNITY_EDITOR
            processors.AddTypeRegistration("AutoWindowSpace", typeof(EditorWindowSpaceProcessor));
            #endif

            // Register modifiers.
            modifiers.AddTypeRegistration("Press", typeof(PressModifier));
            modifiers.AddTypeRegistration("Hold", typeof(HoldModifier));
            modifiers.AddTypeRegistration("Tap", typeof(TapModifier));
            modifiers.AddTypeRegistration("SlowTap", typeof(SlowTapModifier));
            //modifiers.AddTypeRegistration("DoubleTap", typeof(DoubleTapModifier));
            modifiers.AddTypeRegistration("Swipe", typeof(SwipeModifier));

            // Register composites.
            composites.AddTypeRegistration("ButtonAxis", typeof(ButtonAxis));
            composites.AddTypeRegistration("ButtonVector", typeof(ButtonVector));
        }

        internal void InstallRuntime(IInputRuntime runtime)
        {
            if (m_Runtime != null)
            {
                m_Runtime.onUpdate = null;
                m_Runtime.onBeforeUpdate = null;
                m_Runtime.onDeviceDiscovered = null;
            }

            m_Runtime = runtime;
            m_Runtime.onUpdate = OnUpdate;
            m_Runtime.onDeviceDiscovered = OnDeviceDiscovered;
            m_Runtime.updateMask = updateMask;

            // We only hook NativeInputSystem.onBeforeUpdate if necessary.
            if (m_UpdateListeners.Count > 0 || m_HaveDevicesWithStateCallbackReceivers)
            {
                m_Runtime.onBeforeUpdate = OnBeforeUpdate;
                m_NativeBeforeUpdateHooked = true;
            }
        }

        // Revive after domain reload.
        internal void InstallGlobals()
        {
            InputControlLayout.s_Layouts = m_Layouts;
            InputControlProcessor.s_Processors = m_Processors;
            InputBindingModifier.s_Modifiers = m_Modifiers;
            InputBindingComposite.s_Composites = m_Composites;

            // During domain reload, when called from RestoreState(), we will get here with m_Runtime being null.
            // InputSystemObject will invoke InstallGlobals() a second time after it has called InstallRuntime().
            InputRuntime.s_Instance = m_Runtime;

            // Reset update state.
            InputUpdate.lastUpdateType = 0;
            InputUpdate.dynamicUpdateCount = 0;
            InputUpdate.fixedUpdateCount = 0;

            InputStateBuffers.SwitchTo(m_StateBuffers, InputUpdateType.Dynamic);
        }

        [Serializable]
        internal struct AvailableDevice
        {
            public InputDeviceDescription description;
            public int deviceId;
            public bool isNative;
        }

        // Used by EditorInputControlLayoutCache to determine whether its state is outdated.
        [NonSerialized] internal int m_LayoutRegistrationVersion;

        [NonSerialized] internal InputControlLayout.Collection m_Layouts;
        [NonSerialized] private TypeTable m_Processors;
        [NonSerialized] private TypeTable m_Modifiers;
        [NonSerialized] private TypeTable m_Composites;

        [NonSerialized] private InputDevice[] m_Devices;
        [NonSerialized] private Dictionary<int, InputDevice> m_DevicesById;
        [NonSerialized] private List<AvailableDevice> m_AvailableDevices; // A record of all devices reported to the system (from native or user code).

        [NonSerialized] private InputUpdateType m_UpdateMask; // Which of our update types are enabled.
        [NonSerialized] internal InputStateBuffers m_StateBuffers;

        // We don't use UnityEvents and thus don't persist the callbacks during domain reloads.
        // Restoration of UnityActions is unreliable and it's too easy to end up with double
        // registrations what will lead to all kinds of misbehavior.
        [NonSerialized] private InlinedArray<DeviceChangeListener> m_DeviceChangeListeners;
        [NonSerialized] private InlinedArray<DeviceFindControlLayoutCallback> m_DeviceFindLayoutCallbacks;
        [NonSerialized] private InlinedArray<LayoutChangeListener> m_LayoutChangeListeners;
        [NonSerialized] private InlinedArray<EventListener> m_EventListeners;
        [NonSerialized] private InlinedArray<UpdateListener> m_UpdateListeners;
        [NonSerialized] private bool m_NativeBeforeUpdateHooked;
        [NonSerialized] private bool m_HaveDevicesWithStateCallbackReceivers;

        [NonSerialized] private IInputRuntime m_Runtime;

        #if UNITY_EDITOR
        [NonSerialized] internal IInputDiagnostics m_Diagnostics;
        #endif

        private static void AddTypeRegistration(Dictionary<InternedString, Type> table, string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");
            if (type == null)
                throw new ArgumentNullException("type");

            var internedName = new InternedString(name);
            table[internedName] = type;
        }

        private static Type LookupTypeRegisteration(Dictionary<InternedString, Type> table, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");

            Type type;
            var internedName = new InternedString(name);
            if (table.TryGetValue(internedName, out type))
                return type;
            return null;
        }

        ////REVIEW: Right now actions are pretty tightly tied into the system; should this be opened up more
        ////        to present mechanisms that the user could build different action systems on?

        // Maps a single control to an action interested in the control. If
        // multiple actions are interested in the same control, we will end up
        // processing the control repeatedly but we assume this is the exception
        // and so optimize for the case where there's only one action going to
        // a control.
        //
        // Split into two structures to keep data needed only when there is an
        // actual value change out of the data we need for doing the scanning.
        private struct StateChangeMonitorMemoryRegion
        {
            public uint offsetRelativeToDevice;
            public uint sizeInBits; // Size of memory region to compare.
            public uint bitOffset;
        }
        private struct StateChangeMonitorListener
        {
            public InputControl control;
            ////REVIEW: this could easily be generalized to take an arbitrary user object plus a "user data" value
            public InputAction action;
            public int bindingIndex;
        }

        ////TODO: optimize the lists away
        ////REVIEW: I think these can be organized smarter to make bookkeeping cheaper
        // Indices correspond with those in m_Devices.
        [NonSerialized] private List<StateChangeMonitorMemoryRegion>[] m_StateChangeMonitorMemoryRegions;
        [NonSerialized] private List<StateChangeMonitorListener>[] m_StateChangeMonitorListeners;
        [NonSerialized] private List<bool>[] m_StateChangeSignalled; ////TODO: make bitfield

        private struct ActionTimeout
        {
            public double time;
            public InputAction action;
            public int bindingIndex;
            public int modifierIndex;
        }

        [NonSerialized] private List<ActionTimeout> m_ActionTimeouts;

        ////TODO: move this out into a generic mechanism that produces change events
        ////TODO: support combining monitors for bitfields
        internal void AddStateChangeMonitor(InputControl control, InputAction action, int bindingIndex)
        {
            var device = control.device;
            Debug.Assert(device != null);

            var deviceIndex = device.m_DeviceIndex;

            // Allocate/reallocate monitor arrays, if necessary.
            if (m_StateChangeMonitorListeners == null)
            {
                var deviceCount = m_Devices.Length;
                m_StateChangeMonitorListeners = new List<StateChangeMonitorListener>[deviceCount];
                m_StateChangeMonitorMemoryRegions = new List<StateChangeMonitorMemoryRegion>[deviceCount];
                m_StateChangeSignalled = new List<bool>[deviceCount];
            }
            else if (m_StateChangeMonitorListeners.Length <= deviceIndex)
            {
                var deviceCount = m_Devices.Length;
                Array.Resize(ref m_StateChangeMonitorListeners, deviceCount);
                Array.Resize(ref m_StateChangeMonitorMemoryRegions, deviceCount);
                Array.Resize(ref m_StateChangeSignalled, deviceCount);
            }

            // Allocate lists, if necessary.
            var listeners = m_StateChangeMonitorListeners[deviceIndex];
            var memoryRegions = m_StateChangeMonitorMemoryRegions[deviceIndex];
            var signals = m_StateChangeSignalled[deviceIndex];
            if (listeners == null)
            {
                listeners = new List<StateChangeMonitorListener>();
                memoryRegions = new List<StateChangeMonitorMemoryRegion>();
                signals = new List<bool>();

                m_StateChangeMonitorListeners[deviceIndex] = listeners;
                m_StateChangeMonitorMemoryRegions[deviceIndex] = memoryRegions;
                m_StateChangeSignalled[deviceIndex] = signals;
            }

            // Add monitor.
            listeners.Add(new StateChangeMonitorListener {action = action, bindingIndex = bindingIndex, control = control});
            memoryRegions.Add(new StateChangeMonitorMemoryRegion
            {
                offsetRelativeToDevice = control.stateBlock.byteOffset - control.device.stateBlock.byteOffset,
                sizeInBits = control.stateBlock.sizeInBits,
                bitOffset = control.stateBlock.bitOffset
            });
            signals.Add(false);
        }

        private void RemoveStateChangeMonitors(InputDevice device)
        {
            if (m_StateChangeMonitorListeners == null)
                return;

            var deviceIndex = device.m_DeviceIndex;
            Debug.Assert(deviceIndex != InputDevice.kInvalidDeviceIndex);

            if (deviceIndex >= m_StateChangeMonitorListeners.Length)
                return;

            ArrayHelpers.EraseAt(ref m_StateChangeMonitorListeners, deviceIndex);
            ArrayHelpers.EraseAt(ref m_StateChangeMonitorMemoryRegions, deviceIndex);
            ArrayHelpers.EraseAt(ref m_StateChangeSignalled, deviceIndex);
        }

        ////REVIEW: better to to just pass device+action and remove all state change monitors for the pair?
        internal void RemoveStateChangeMonitor(InputControl control, InputAction action)
        {
            if (m_StateChangeMonitorListeners == null)
                return;

            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;

            // Ignore if device has already been removed.
            if (deviceIndex == InputDevice.kInvalidDeviceIndex)
                return;

            // Ignore if there are no state monitors set up for the device.
            if (deviceIndex >= m_StateChangeMonitorListeners.Length)
                return;

            var listeners = m_StateChangeMonitorListeners[deviceIndex];
            var regions = m_StateChangeMonitorMemoryRegions[deviceIndex];
            var signals = m_StateChangeSignalled[deviceIndex];

            for (var i = 0; i < listeners.Count; ++i)
            {
                if (listeners[i].action == action && listeners[i].control == control)
                {
                    ////TODO: use InlinedArrays for these and only null out entries; clean up array when traversing it during processing
                    listeners.RemoveAt(i);
                    regions.RemoveAt(i);
                    signals.RemoveAt(i);
                    break;
                }
            }
        }

        internal void AddActionTimeout(InputAction action, double time, int bindingIndex, int modifierIndex)
        {
            if (m_ActionTimeouts == null)
                m_ActionTimeouts = new List<ActionTimeout>();

            m_ActionTimeouts.Add(new ActionTimeout
            {
                time = time,
                action = action,
                bindingIndex = bindingIndex,
                modifierIndex = modifierIndex
            });
        }

        internal void RemoveActionTimeout(InputAction action, int bindingIndex, int modifierIndex)
        {
            if (m_ActionTimeouts == null)
                return;

            for (var i = 0; i < m_ActionTimeouts.Count; ++i)
            {
                if (m_ActionTimeouts[i].action == action
                    && m_ActionTimeouts[i].bindingIndex == bindingIndex
                    && m_ActionTimeouts[i].modifierIndex == modifierIndex)
                {
                    ////TODO: leave state empty and compact array lazily on traversal
                    m_ActionTimeouts.RemoveAt(i);
                    break;
                }
            }
        }

        ////REVIEW: Make it so that device names *always* have a number appended? (i.e. Gamepad1, Gamepad2, etc. instead of Gamepad, Gamepad1, etc)

        private void MakeDeviceNameUnique(InputDevice device)
        {
            if (m_Devices == null)
                return;

            var name = device.name;
            var nameLowerCase = device.m_Name.ToLower();
            var nameIsUnique = false;
            var namesTried = 1;

            // Find unique name.
            while (!nameIsUnique)
            {
                nameIsUnique = true;
                for (var i = 0; i < m_Devices.Length; ++i)
                {
                    if (m_Devices[i].name.ToLower() == nameLowerCase)
                    {
                        name = string.Format("{0}{1}", device.name, namesTried);
                        nameLowerCase = name.ToLower();
                        nameIsUnique = false;
                        ++namesTried;
                        break;
                    }
                }
            }

            // If we have changed the name of the device, nuke all path strings in the control
            // hiearchy so that they will get re-recreated when queried.
            if (namesTried >= 1)
                ResetControlPathsRecursive(device);

            // Assign name.
            device.m_Name = new InternedString(name);
        }

        private void ResetControlPathsRecursive(InputControl control)
        {
            control.m_Path = null;

            var children = control.children;
            var childCount = children.Count;

            for (var i = 0; i < childCount; ++i)
                ResetControlPathsRecursive(children[i]);
        }

        private void AssignUniqueDeviceId(InputDevice device)
        {
            // If the device already has an ID, make sure it's unique.
            if (device.id != InputDevice.kInvalidDeviceId)
            {
                // Safety check to make sure out IDs are really unique.
                // Given they are assigned by the native system they should be fine
                // but let's make sure.
                var existingDeviceWithId = TryGetDeviceById(device.id);
                if (existingDeviceWithId != null)
                    throw new Exception(
                        string.Format("Duplicate device ID {0} detected for devices '{1}' and '{2}'", device.id,
                            device.name, existingDeviceWithId.name));
            }
            else
            {
                device.m_Id = m_Runtime.AllocateDeviceId();
            }
        }

        // (Re)allocates state buffers and assigns each device that's been added
        // a segment of the buffer. Preserves the current state of devices.
        // NOTE: Installs the buffers globally.
        private void ReallocateStateBuffers(int[] oldDeviceIndices = null)
        {
            var devices = m_Devices;
            var oldBuffers = m_StateBuffers;

            // Allocate new buffers.
            var newBuffers = new InputStateBuffers();
            var newStateBlockOffsets = newBuffers.AllocateAll(m_UpdateMask, devices);

            // Migrate state.
            newBuffers.MigrateAll(devices, newStateBlockOffsets, oldBuffers, oldDeviceIndices);

            // Install the new buffers.
            oldBuffers.FreeAll();
            m_StateBuffers = newBuffers;
            InputStateBuffers.SwitchTo(m_StateBuffers,
                InputUpdate.lastUpdateType != 0 ? InputUpdate.lastUpdateType : InputUpdateType.Dynamic);

            ////TODO: need to update state change monitors
        }

        private void OnDeviceDiscovered(int deviceId, string deviceDescriptor)
        {
            // Parse description.
            var description = InputDeviceDescription.FromJson(deviceDescriptor);

            // Report it.
            ReportAvailableDevice(description, deviceId, isNative: true);
        }

        private void InstallBeforeUpdateHookIfNecessary()
        {
            if (m_NativeBeforeUpdateHooked || m_Runtime == null)
                return;

            m_Runtime.onBeforeUpdate = OnBeforeUpdate;
            m_NativeBeforeUpdateHooked = true;
        }

        private unsafe void OnBeforeUpdate(InputUpdateType updateType)
        {
            // For devices that have state callbacks, tell them we're carrying state over
            // into the next frame.
            if (m_HaveDevicesWithStateCallbackReceivers && updateType != InputUpdateType.BeforeRender) ////REVIEW: before-render handling is probably wrong
            {
                var stateBuffers = m_StateBuffers.GetDoubleBuffersFor(updateType);
                var isDynamicOrFixedUpdate =
                    updateType == InputUpdateType.Dynamic || updateType == InputUpdateType.Fixed;

                // For the sake of action state monitors, we need to be able to detect when
                // an OnCarryStateForward() method writes new values into a state buffer. To do
                // so, we create a temporary buffer, copy state blocks into that buffer, and then
                // run the normal action change logic on the temporary and the current state buffer.
                using (var tempBuffer = new NativeArray<byte>((int)m_StateBuffers.sizePerBuffer, Allocator.Temp))
                {
                    var tempBufferPtr = (byte*)tempBuffer.GetUnsafeReadOnlyPtr();
                    var time = Time.time;

                    for (var i = 0; i < m_Devices.Length; ++i)
                    {
                        var device = m_Devices[i];
                        if ((device.m_Flags & InputDevice.Flags.HasStateCallbacks) != InputDevice.Flags.HasStateCallbacks)
                            continue;

                        // Depending on update ordering, we are writing events into *upcoming* updates inside of
                        // OnUpdate(). E.g. we may receive an event in fixed update and write it concurrently into
                        // the fixed and dynamic update buffer for the device.
                        //
                        // This means that we have to be extra careful here not to overwrite state which has already
                        // been updated with events. To check for this, we simply determine whether the device's update
                        // count for the current update type already corresponds to the count of the upcoming update.
                        //
                        // NOTE: This is only relevant for non-editor updates.
                        if (isDynamicOrFixedUpdate)
                        {
                            if (updateType == InputUpdateType.Dynamic)
                            {
                                if (device.m_CurrentDynamicUpdateCount == InputUpdate.dynamicUpdateCount + 1)
                                    continue; // Device already received state for upcoming dynamic update.
                            }
                            else if (updateType == InputUpdateType.Fixed)
                            {
                                if (device.m_CurrentFixedUpdateCount == InputUpdate.fixedUpdateCount + 1)
                                    continue; // Device already received state for upcoming fixed update.
                            }
                        }

                        var deviceStateOffset = device.m_StateBlock.byteOffset;
                        var deviceStateSize = device.m_StateBlock.alignedSizeInBytes;

                        ////REVIEW: don't we need to flip here?

                        // Grab current front buffer.
                        var frontBuffer = stateBuffers.GetFrontBuffer(device.m_DeviceIndex);

                        // Copy to temporary buffer.
                        var statePtr = (byte*)frontBuffer.ToPointer() + deviceStateOffset;
                        var tempStatePtr = tempBufferPtr + deviceStateOffset;
                        UnsafeUtility.MemCpy(tempStatePtr, statePtr, deviceStateSize);

                        // Show to device.
                        if (((IInputStateCallbackReceiver)device).OnCarryStateForward(frontBuffer))
                        {
                            // Let listeners know the device's state has changed.
                            for (var n = 0; n < m_DeviceChangeListeners.Count; ++n)
                                m_DeviceChangeListeners[n](device, InputDeviceChange.StateChanged);

                            // Process action state change monitors.
                            if (ProcessStateChangeMonitors(i, new IntPtr(statePtr), new IntPtr(tempStatePtr),
                                    deviceStateSize, 0))
                            {
                                ////REVIEW: should this make the device current?
                                FireActionStateChangeNotifications(i, time);
                            }
                        }
                    }
                }
            }

            ////REVIEW: should we activate the buffers for the given update here?
            for (var i = 0; i < m_UpdateListeners.Count; ++i)
                m_UpdateListeners[i](updateType);
        }

        // NOTE: Update types do *NOT* say what the events we receive are for. The update type only indicates
        //       where in the Unity's application loop we got called from.
        internal unsafe void OnUpdate(InputUpdateType updateType, int eventCount, IntPtr eventData)
        {
            ////TODO: switch from Profiler to CustomSampler API
            // NOTE: This is *not* using try/finally as we've seen unreliability in the EndSample()
            //       execution (and we're not sure where it's coming from).
            Profiler.BeginSample("InputUpdate");

            // In the editor, we need to decide where to route state. Whenever the game is playing and
            // has focus, we route all input to play mode buffers. When the game is stopped or if any
            // of the other editor windows has focus, we route input to edit mode buffers.
            var gameIsPlayingAndHasFocus = true;
            var buffersToUseForUpdate = updateType;
#if UNITY_EDITOR
            gameIsPlayingAndHasFocus = InputConfiguration.LockInputToGame ||
                (UnityEditor.EditorApplication.isPlaying && Application.isFocused);

            if (updateType == InputUpdateType.Editor && gameIsPlayingAndHasFocus)
            {
                // For actions, it is important we have play mode buffers active when
                // fire change notifications.
                if (m_StateBuffers.m_DynamicUpdateBuffers.valid)
                    buffersToUseForUpdate = InputUpdateType.Dynamic;
                else
                    buffersToUseForUpdate = InputUpdateType.Fixed;
            }
#endif

            InputUpdate.lastUpdateType = updateType;
            InputStateBuffers.SwitchTo(m_StateBuffers, buffersToUseForUpdate);

            ////REVIEW: which set of buffers should we have active when processing timeouts?
            if (m_ActionTimeouts != null && gameIsPlayingAndHasFocus) ////REVIEW: for now, making actions exclusive to play mode
                ProcessActionTimeouts();

            var isBeforeRenderUpdate = false;
            if (updateType == InputUpdateType.Dynamic)
                ++InputUpdate.dynamicUpdateCount;
            else if (updateType == InputUpdateType.Fixed)
                ++InputUpdate.fixedUpdateCount;
            else if (updateType == InputUpdateType.BeforeRender)
                isBeforeRenderUpdate = true;

            // Early out if there's no events to process.
            if (eventCount <= 0)
            {
                if (buffersToUseForUpdate != updateType)
                    InputStateBuffers.SwitchTo(m_StateBuffers, updateType);
                #if ENABLE_PROFILER
                Profiler.EndSample();
                #endif
                return;
            }

            // Before render updates work in a special way. For them, we only want specific devices (and
            // sometimes even just specific controls on those devices) to be updated. What native will do is
            // it will *not* clear the event buffer after showing it to us. This means that in the next
            // normal update, we will see the same events again. This gives us a chance to only fish out
            // what we want.
            //
            // In before render updates, we will only access StateEvents and DeltaEvents (the latter should
            // be used to, for example, *only* update tracking on a device that also contains buttons -- which
            // should not get updated in berfore render).

            var currentEventPtr = (InputEvent*)eventData;
            var remainingEventCount = eventCount;

            // Handle events.
            while (remainingEventCount > 0)
            {
                InputDevice device = null;
                var doNotMakeDeviceCurrent = false;

                // Bump firstEvent up to the next unhandled event (in before-render updates
                // the event needs to be *both* unhandled *and* for a device with before
                // render updates enabled).
                while (remainingEventCount > 0)
                {
                    if (isBeforeRenderUpdate)
                    {
                        if (!currentEventPtr->handled)
                        {
                            device = TryGetDeviceById(currentEventPtr->deviceId);
                            if (device != null && device.updateBeforeRender)
                                break;
                        }
                    }
                    else if (!currentEventPtr->handled)
                        break;

                    currentEventPtr = InputEvent.GetNextInMemory(currentEventPtr);
                    --remainingEventCount;
                }
                if (remainingEventCount == 0)
                    break;

                // Give listeners a shot at the event.
                var listenerCount = m_EventListeners.Count;
                if (listenerCount > 0)
                {
                    for (var i = 0; i < listenerCount; ++i)
                        m_EventListeners[i](new InputEventPtr(currentEventPtr));
                    if (currentEventPtr->handled)
                        continue;
                }

                // Grab device for event. In before-render updates, we already had to
                // check the device.
                if (!isBeforeRenderUpdate)
                    device = TryGetDeviceById(currentEventPtr->deviceId);
                if (device == null)
                {
                    #if UNITY_EDITOR
                    if (m_Diagnostics != null)
                        m_Diagnostics.OnCannotFindDeviceForEvent(new InputEventPtr(currentEventPtr));
                    #endif

                    // No device found matching event. Consider it handled.
                    currentEventPtr->handled = true;
                    continue;
                }

                // Process.
                var currentEventType = currentEventPtr->type;
                var currentEventTime = currentEventPtr->time;
                switch (currentEventType)
                {
                    case StateEvent.Type:
                    case DeltaStateEvent.Type:

                        // Ignore state changes if device is disabled.
                        if (!device.enabled)
                        {
                            #if UNITY_EDITOR
                            if (m_Diagnostics != null)
                                m_Diagnostics.OnEventForDisabledDevice(new InputEventPtr(currentEventPtr), device);
                            #endif
                            doNotMakeDeviceCurrent = true;
                            break;
                        }

                        // Ignore the event if the last state update we received for the device was
                        // newer than this state event is.
                        if (currentEventTime < device.m_LastUpdateTime)
                        {
                            #if UNITY_EDITOR
                            if (m_Diagnostics != null)
                                m_Diagnostics.OnEventTimestampOutdated(new InputEventPtr(currentEventPtr), device);
                            #endif
                            doNotMakeDeviceCurrent = true;
                            break;
                        }

                        var deviceHasStateCallbacks = (device.m_Flags & InputDevice.Flags.HasStateCallbacks) ==
                            InputDevice.Flags.HasStateCallbacks;
                        IInputStateCallbackReceiver stateCallbacks = null;
                        var deviceIndex = device.m_DeviceIndex;
                        var stateBlockOfDevice = device.m_StateBlock;
                        var stateBlockSizeOfDevice = stateBlockOfDevice.alignedSizeInBytes;
                        var offsetInDeviceStateToCopyTo = 0u;
                        uint sizeOfStateToCopy;
                        uint receivedStateSize;
                        IntPtr ptrToReceivedState;
                        FourCC receivedStateFormat;
                        var needToCopyFromBackBuffer = false;

                        // Grab state data from event and decide where to copy to and how much to copy.
                        if (currentEventType == StateEvent.Type)
                        {
                            var stateEventPtr = (StateEvent*)currentEventPtr;
                            receivedStateFormat = stateEventPtr->stateFormat;
                            receivedStateSize = stateEventPtr->stateSizeInBytes;
                            ptrToReceivedState = stateEventPtr->state;

                            // Ignore extra state at end of event.
                            sizeOfStateToCopy = receivedStateSize;
                            if (sizeOfStateToCopy > stateBlockSizeOfDevice)
                                sizeOfStateToCopy = stateBlockSizeOfDevice;
                        }
                        else
                        {
                            var deltaEventPtr = (DeltaStateEvent*)currentEventPtr;
                            receivedStateFormat = deltaEventPtr->stateFormat;
                            receivedStateSize = deltaEventPtr->deltaStateSizeInBytes;
                            ptrToReceivedState = deltaEventPtr->deltaState;
                            offsetInDeviceStateToCopyTo = deltaEventPtr->stateOffset;

                            // Ignore extra state at end of event.
                            sizeOfStateToCopy = receivedStateSize;
                            if (offsetInDeviceStateToCopyTo + sizeOfStateToCopy > stateBlockSizeOfDevice)
                            {
                                if (offsetInDeviceStateToCopyTo >= stateBlockSizeOfDevice)
                                    break; // Entire delta state is out of range.

                                sizeOfStateToCopy = stateBlockSizeOfDevice - offsetInDeviceStateToCopyTo;
                            }
                        }

                        // If the state format doesn't match, see if the device knows what to do.
                        // If not, ignore the event.
                        if (stateBlockOfDevice.format != receivedStateFormat)
                        {
                            var canIncorporateUnrecognizedState = false;
                            if (deviceHasStateCallbacks)
                            {
                                if (stateCallbacks == null)
                                    stateCallbacks = (IInputStateCallbackReceiver)device;
                                canIncorporateUnrecognizedState =
                                    stateCallbacks.OnReceiveStateWithDifferentFormat(ptrToReceivedState, receivedStateFormat,
                                        receivedStateSize, ref offsetInDeviceStateToCopyTo);

                                // If the device tells us to put the state somewhere inside of it, we're potentially
                                // performing a partial state update, so bring the current state forward like for delta
                                // state events.
                                needToCopyFromBackBuffer = true;
                            }

                            if (!canIncorporateUnrecognizedState)
                            {
                                #if UNITY_EDITOR
                                if (m_Diagnostics != null)
                                    m_Diagnostics.OnEventFormatMismatch(new InputEventPtr(currentEventPtr), device);
                                #endif
                                doNotMakeDeviceCurrent = true;
                                break;
                            }
                        }

                        // If the device has state callbacks, give it a shot at running custom logic on
                        // the new state before we integrate it into the system.
                        if (deviceHasStateCallbacks)
                        {
                            if (stateCallbacks == null)
                                stateCallbacks = (IInputStateCallbackReceiver)device;

                            ////FIXME: this will read state from the current update, then combine it with the new state, and then write into all states
                            var currentState = InputStateBuffers.GetFrontBufferForDevice(deviceIndex);
                            var newState = new IntPtr((byte*)ptrToReceivedState.ToPointer() - stateBlockOfDevice.byteOffset);  // Account for device offset in buffers.

                            stateCallbacks.OnBeforeWriteNewState(currentState, newState);
                        }

                        // Before we update state, let change monitors compare the old and the new state.
                        // We do this instead of first updating the front buffer and then comparing to the
                        // back buffer as that would require a buffer flip for each state change in order
                        // for the monitors to work reliably. By comparing the *event* data to the current
                        // state, we can have multiple state events in the same frame yet still get reliable
                        // change notifications.
                        var haveSignalledMonitors =
                            gameIsPlayingAndHasFocus && ////REVIEW: for now making actions exclusive to player
                            ProcessStateChangeMonitors(deviceIndex, ptrToReceivedState,
                                new IntPtr(InputStateBuffers.GetFrontBufferForDevice(deviceIndex).ToInt64() + stateBlockOfDevice.byteOffset),
                                sizeOfStateToCopy, offsetInDeviceStateToCopyTo);

                        // Buffer flip.
                        if (FlipBuffersForDeviceIfNecessary(device, updateType, gameIsPlayingAndHasFocus))
                        {
                            // In case of a delta state event we need to carry forward all state we're
                            // not updating. Instead of optimizing the copy here, we're just bringing the
                            // entire state forward.
                            if (currentEventType == DeltaStateEvent.Type)
                                needToCopyFromBackBuffer = true;
                        }

                        // Now write the state.
                        var deviceStateOffset = device.m_StateBlock.byteOffset + offsetInDeviceStateToCopyTo;

#if UNITY_EDITOR
                        if (!gameIsPlayingAndHasFocus)
                        {
                            var buffer = m_StateBuffers.m_EditorUpdateBuffers.GetFrontBuffer(deviceIndex);
                            Debug.Assert(buffer != IntPtr.Zero);

                            if (needToCopyFromBackBuffer)
                                UnsafeUtility.MemCpy(
                                    (void*)(buffer.ToInt64() + (int)device.m_StateBlock.byteOffset),
                                    (void*)(m_StateBuffers.m_EditorUpdateBuffers.GetBackBuffer(deviceIndex).ToInt64() +
                                            (int)device.m_StateBlock.byteOffset),
                                    device.m_StateBlock.alignedSizeInBytes);

                            UnsafeUtility.MemCpy((void*)(buffer.ToInt64() + (int)deviceStateOffset), ptrToReceivedState.ToPointer(), sizeOfStateToCopy);
                        }
                        else
#endif
                        {
                            // For dynamic and fixed updates, we have to write into the front buffer
                            // of both updates as a state change event comes in only once and we have
                            // to reflect the most current state in both update types.
                            //
                            // If one or the other update is disabled, however, we will perform a single
                            // memcpy here.
                            if (m_StateBuffers.m_DynamicUpdateBuffers.valid)
                            {
                                var buffer = m_StateBuffers.m_DynamicUpdateBuffers.GetFrontBuffer(deviceIndex);
                                Debug.Assert(buffer != IntPtr.Zero);

                                if (needToCopyFromBackBuffer)
                                    UnsafeUtility.MemCpy(
                                        (void*)(buffer.ToInt64() + (int)device.m_StateBlock.byteOffset),
                                        (void*)(m_StateBuffers.m_DynamicUpdateBuffers.GetBackBuffer(deviceIndex).ToInt64() +
                                                (int)device.m_StateBlock.byteOffset),
                                        device.m_StateBlock.alignedSizeInBytes);

                                UnsafeUtility.MemCpy((void*)(buffer.ToInt64() + (int)deviceStateOffset), ptrToReceivedState.ToPointer(), sizeOfStateToCopy);
                            }
                            if (m_StateBuffers.m_FixedUpdateBuffers.valid)
                            {
                                var buffer = m_StateBuffers.m_FixedUpdateBuffers.GetFrontBuffer(deviceIndex);
                                Debug.Assert(buffer != IntPtr.Zero);

                                if (needToCopyFromBackBuffer)
                                    UnsafeUtility.MemCpy(
                                        (void*)(buffer.ToInt64() + (int)device.m_StateBlock.byteOffset),
                                        (void*)(m_StateBuffers.m_FixedUpdateBuffers.GetBackBuffer(deviceIndex).ToInt64() +
                                                (int)device.m_StateBlock.byteOffset),
                                        device.m_StateBlock.alignedSizeInBytes);

                                UnsafeUtility.MemCpy((void*)(buffer.ToInt64() + (int)deviceStateOffset), ptrToReceivedState.ToPointer(), sizeOfStateToCopy);
                            }
                        }

                        device.m_LastUpdateTime = currentEventTime;

                        // Notify listeners.
                        for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                            m_DeviceChangeListeners[i](device, InputDeviceChange.StateChanged);

                        // Now that we've committed the new state to memory, if any of the change
                        // monitors fired, let the associated actions know.
                        ////FIXME: this needs to happen with player buffers active
                        if (haveSignalledMonitors)
                            FireActionStateChangeNotifications(deviceIndex, currentEventTime);

                        break;

                    case TextEvent.Type:
                        var textEventPtr = (TextEvent*)currentEventPtr;
                        ////TODO: handle UTF-32 to UTF-16 conversion properly
                        device.OnTextInput((char)textEventPtr->character);
                        break;

                    case DeviceRemoveEvent.Type:
                        RemoveDevice(device);
                        doNotMakeDeviceCurrent = true;
                        break;

                    case DeviceConfigurationEvent.Type:
                        device.OnConfigurationChanged();
                        for (var i = 0; i < m_DeviceChangeListeners.Count; ++i)
                            m_DeviceChangeListeners[i](device, InputDeviceChange.ConfigurationChanged);
                        break;
                }

                // Mark as processed.
                currentEventPtr->handled = true;
                if (remainingEventCount >= 1)
                {
                    currentEventPtr = InputEvent.GetNextInMemory(currentEventPtr);
                    --remainingEventCount;
                }

                ////TODO: move this into the state event case; don't make device current for other types of events
                ////TODO: we need to filter out noisy devices; PS4 controller, for example, just spams constant reports and thus will always make itself current
                ////      (check for actual change and only make current if state changed?)
                // Device received event so make it current except if we got a
                // device removal event.
                if (!doNotMakeDeviceCurrent)
                    device.MakeCurrent();
            }

            ////TODO: fire event that allows code to update state *from* state we just updated

            if (buffersToUseForUpdate != updateType)
                InputStateBuffers.SwitchTo(m_StateBuffers, updateType);

            Profiler.EndSample();
        }

        // NOTE: 'newState' can be a subset of the full state stored at 'oldState'. In this case,
        //       'newStateOffset' must give the offset into the full state and 'newStateSize' must
        //       give the size of memory slice to be updated.
        private unsafe bool ProcessStateChangeMonitors(int deviceIndex, IntPtr newState, IntPtr oldState, uint newStateSize, uint newStateOffset)
        {
            if (m_StateChangeMonitorListeners == null)
                return false;

            // We resize the monitor arrays only when someone adds to them so they
            // may be out of sync with the size of m_Devices.
            if (deviceIndex >= m_StateChangeMonitorListeners.Length)
                return false;

            var changeMonitors = m_StateChangeMonitorMemoryRegions[deviceIndex];
            if (changeMonitors == null)
                return false; // No action cares about state changes on this device.

            var signals = m_StateChangeSignalled[deviceIndex];

            var numMonitors = changeMonitors.Count;
            var signalled = false;

            // Bake offsets into state pointers so that we don't have to adjust for
            // them repeatedly.
            if (newStateOffset != 0)
            {
                newState = new IntPtr(newState.ToInt64() - newStateOffset);
                oldState = new IntPtr(oldState.ToInt64() + newStateOffset);
            }

            for (var i = 0; i < numMonitors; ++i)
            {
                var memoryRegion = changeMonitors[i];
                var offset = (int)memoryRegion.offsetRelativeToDevice;
                var sizeInBits = memoryRegion.sizeInBits;
                var bitOffset = memoryRegion.bitOffset;

                // If we've updated only part of the state, see if the monitored region and the
                // updated region overlap. Ignore monitor if they don't.
                if (newStateOffset != 0 &&
                    !MemoryHelpers.MemoryOverlapsBitRegion((uint)offset, bitOffset, sizeInBits, newStateOffset, (uint)newStateSize))
                    continue;

                // See if we are comparing bits or bytes.
                if (sizeInBits % 8 != 0 || bitOffset != 0)
                {
                    // Not-so-simple path: compare bits.

                    if (sizeInBits > 1)
                        throw new NotImplementedException("state change detection on multi-bit fields");

                    // Check if bit offset is out of range of state we have.
                    if (MemoryHelpers.ComputeFollowingByteOffset((uint)offset + newStateOffset, bitOffset) > newStateSize)
                        continue;

                    if (MemoryHelpers.ReadSingleBit(new IntPtr(newState.ToInt64() + offset), bitOffset) ==
                        MemoryHelpers.ReadSingleBit(new IntPtr(oldState.ToInt64() + offset), bitOffset))
                        continue;
                }
                else
                {
                    // Simple path: compare whole bytes.

                    var sizeInBytes = sizeInBits / 8;
                    if (offset - newStateOffset + sizeInBytes > newStateSize)
                        continue;

                    if (UnsafeUtility.MemCmp((byte*)newState.ToPointer() + offset, (byte*)oldState.ToPointer() + offset, sizeInBytes) == 0)
                        continue;
                }

                signals[i] = true;
                signalled = true;
            }

            return signalled;
        }

        private void FireActionStateChangeNotifications(int deviceIndex, double time)
        {
            var signals = m_StateChangeSignalled[deviceIndex];
            var listeners = m_StateChangeMonitorListeners[deviceIndex];

            for (var i = 0; i < signals.Count; ++i)
            {
                if (signals[i])
                {
                    var listener = listeners[i];
                    listener.action.NotifyControlValueChanged(listener.control, listener.bindingIndex, time);
                    signals[i] = false;
                }
            }
        }

        private void ProcessActionTimeouts()
        {
            var time = Time.time;
            for (var i = 0; i < m_ActionTimeouts.Count; ++i)
                if (m_ActionTimeouts[i].time <= time)
                {
                    m_ActionTimeouts[i].action.NotifyTimerExpired(m_ActionTimeouts[i].bindingIndex, m_ActionTimeouts[i].modifierIndex, time);
                    ////TODO: use plain array and compact entries on traversal
                    m_ActionTimeouts.RemoveAt(i);
                }
        }

        // Flip front and back buffer for device, if necessary. May flip buffers for more than just
        // the given update type.
        // Returns true if there was a buffer flip.
        private bool FlipBuffersForDeviceIfNecessary(InputDevice device, InputUpdateType updateType, bool gameIsPlayingAndHasFocus)
        {
            if (updateType == InputUpdateType.BeforeRender)
            {
                ////REVIEW: I think this is wrong; if we haven't flipped in the current dynamic or fixed update, we should do so now
                // We never flip buffers for before render. Instead, we already write
                // into the front buffer.
                return false;
            }

#if UNITY_EDITOR
            // Updates go to the editor only if the game isn't playing or does not have focus.
            // Otherwise we fall through to the logic that flips for the *next* dynamic and
            // fixed updates.
            if (updateType == InputUpdateType.Editor && !gameIsPlayingAndHasFocus)
            {
                // The editor doesn't really have a concept of frame-to-frame operation the
                // same way the player does. So we simply flip buffers on a device whenever
                // a new state event for it comes in.
                m_StateBuffers.m_EditorUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                return true;
            }
#endif

            var flipped = false;

            // If it is *NOT* a fixed update, we need to flip for the *next* coming fixed
            // update if we haven't already.
            if (updateType != InputUpdateType.Fixed &&
                device.m_CurrentFixedUpdateCount != InputUpdate.fixedUpdateCount + 1)
            {
                m_StateBuffers.m_FixedUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentFixedUpdateCount = InputUpdate.fixedUpdateCount + 1;
                flipped = true;
            }

            // If it is *NOT* a dynamic update, we need to flip for the *next* coming
            // dynamic update if we haven't already.
            if (updateType != InputUpdateType.Dynamic &&
                device.m_CurrentDynamicUpdateCount != InputUpdate.dynamicUpdateCount + 1)
            {
                m_StateBuffers.m_DynamicUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentDynamicUpdateCount = InputUpdate.dynamicUpdateCount + 1;
                flipped = true;
            }

            // If it *is* a fixed update and we haven't flipped for the current update
            // yet, do it.
            if (updateType == InputUpdateType.Fixed &&
                device.m_CurrentFixedUpdateCount != InputUpdate.fixedUpdateCount)
            {
                m_StateBuffers.m_FixedUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentFixedUpdateCount = InputUpdate.fixedUpdateCount;
                flipped = true;
            }

            // If it *is* a dynamic update and we haven't flipped for the current update
            // yet, do it.
            if (updateType == InputUpdateType.Dynamic &&
                device.m_CurrentDynamicUpdateCount != InputUpdate.dynamicUpdateCount)
            {
                m_StateBuffers.m_DynamicUpdateBuffers.SwapBuffers(device.m_DeviceIndex);
                device.m_CurrentDynamicUpdateCount = InputUpdate.dynamicUpdateCount;
                flipped = true;
            }

            return flipped;
        }

        // Domain reload survival logic. Also used for pushing and popping input system
        // state for testing.

        // Stuff everything that we want to survive a domain reload into
        // a m_SerializedState.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Serializable]
        internal struct DeviceState
        {
            // Preserving InputDevices is somewhat tricky business. Serializing
            // them in full would involve pretty nasty work. We have the restriction,
            // however, that everything needs to be created from layouts (it partly
            // exists for the sake of reload survivability), so we should be able to
            // just go and recreate the device from the layout. This also has the
            // advantage that if the layout changes between reloads, the change
            // automatically takes effect.
            public string name;
            public string layout;
            public string variant;
            public string[] usages;
            public int deviceId;
            public uint stateOffset;
            public InputDevice.Flags flags;
            public InputDeviceDescription description;

            public void RestoreUsagesOnDevice(InputDevice device)
            {
                if (usages == null || usages.Length == 0)
                    return;
                var index = ArrayHelpers.Append(ref device.m_UsagesForEachControl, usages.Select(x => new InternedString(x)));
                device.m_UsagesReadOnly =
                    new ReadOnlyArray<InternedString>(device.m_UsagesForEachControl, index, usages.Length);
                device.UpdateUsageArraysOnControls();
            }
        }

        [Serializable]
        internal struct LayoutState
        {
            public string name;
            public string typeNameOrJson;
        }

        [Serializable]
        internal struct BaseLayoutState
        {
            public string baseLayout;
            public string derivedLayout;
        }

        [Serializable]
        internal struct LayoutBuilderState
        {
            public string name;
            public string typeName;
            public string methodName;
            public string instanceJson;
        }

        [Serializable]
        internal struct LayoutDeviceState
        {
            public string layoutName;
            public string matcherJson;
        }

        [Serializable]
        internal struct SerializedState
        {
            public int layoutRegistrationVersion;
            public LayoutState[] layoutTypes;
            public LayoutState[] layoutStrings;
            public LayoutBuilderState[] layoutFactories;
            public BaseLayoutState[] baseLayouts;
            public LayoutDeviceState[] layoutDeviceMatchers;
            public TypeTable.SavedState processors;
            public TypeTable.SavedState modifiers;
            public TypeTable.SavedState composites;
            public DeviceState[] devices;
            public AvailableDevice[] availableDevices;
            public InputStateBuffers buffers;
            public InputConfiguration.SerializedState configuration;
            public InputUpdate.SerializedState updateState;
            public InputUpdateType updateMask;

            // The rest is state that we want to preserve across Save() and Restore() but
            // not across domain reloads.

            [NonSerialized] public InlinedArray<DeviceChangeListener> deviceChangeListeners;
            [NonSerialized] public InlinedArray<DeviceFindControlLayoutCallback> deviceFindLayoutCallbacks;
            [NonSerialized] public InlinedArray<LayoutChangeListener> layoutChangeListeners;
            [NonSerialized] public InlinedArray<EventListener> eventListeners;

            [NonSerialized] public IInputRuntime runtime;

            #if UNITY_EDITOR
            [NonSerialized] public IInputDiagnostics diagnostics;
            #endif
        }

        internal SerializedState SaveState()
        {
            // Layout types.
            var layoutTypeCount = m_Layouts.layoutTypes.Count;
            var layoutTypeArray = new LayoutState[layoutTypeCount];

            var i = 0;
            foreach (var entry in m_Layouts.layoutTypes)
                layoutTypeArray[i++] = new LayoutState
                {
                    name = entry.Key,
                    typeNameOrJson = entry.Value.AssemblyQualifiedName
                };

            // Layout strings.
            var layoutStringCount = m_Layouts.layoutStrings.Count;
            var layoutStringArray = new LayoutState[layoutStringCount];

            i = 0;
            foreach (var entry in m_Layouts.layoutStrings)
                layoutStringArray[i++] = new LayoutState
                {
                    name = entry.Key,
                    typeNameOrJson = entry.Value
                };

            // Layout factories.
            var layoutBuilderCount = m_Layouts.layoutBuilders.Count;
            var layoutBuilderArray = new LayoutBuilderState[layoutBuilderCount];

            i = 0;
            foreach (var entry in m_Layouts.layoutBuilders)
                layoutBuilderArray[i++] = new LayoutBuilderState
                {
                    name = entry.Key,
                    typeName = entry.Value.method.DeclaringType.AssemblyQualifiedName,
                    methodName = entry.Value.method.Name,
                    instanceJson = entry.Value.instance != null ? JsonUtility.ToJson(entry.Value.instance) : null,
                };

            // Devices.
            var deviceCount = m_Devices != null ? m_Devices.Length : 0;
            var deviceArray = new DeviceState[deviceCount];
            for (i = 0; i < deviceCount; ++i)
            {
                var device = m_Devices[i];
                var deviceState = new DeviceState
                {
                    name = device.name,
                    layout = device.layout,
                    variant = device.variant,
                    deviceId = device.id,
                    usages = device.usages.Select(x => x.ToString()).ToArray(),
                    stateOffset = device.m_StateBlock.byteOffset,
                    description = device.m_Description,
                    flags = device.m_Flags
                };
                deviceArray[i] = deviceState;
            }

            return new SerializedState
            {
                layoutRegistrationVersion = m_LayoutRegistrationVersion,
                layoutTypes = layoutTypeArray,
                layoutStrings = layoutStringArray,
                layoutFactories = layoutBuilderArray,
                baseLayouts = m_Layouts.baseLayoutTable.Select(x => new BaseLayoutState { derivedLayout = x.Key, baseLayout = x.Value }).ToArray(),
                layoutDeviceMatchers = m_Layouts.layoutDeviceMatchers.Select(x => new LayoutDeviceState { matcherJson = x.Value.ToJson(), layoutName = x.Key }).ToArray(),
                processors = m_Processors.SaveState(),
                modifiers = m_Modifiers.SaveState(),
                composites = m_Composites.SaveState(),
                devices = deviceArray,
                availableDevices = m_AvailableDevices.ToArray(),
                buffers = m_StateBuffers,
                configuration = InputConfiguration.Save(),
                updateState = InputUpdate.Save(),
                deviceChangeListeners = m_DeviceChangeListeners.Clone(),
                deviceFindLayoutCallbacks = m_DeviceFindLayoutCallbacks.Clone(),
                layoutChangeListeners = m_LayoutChangeListeners.Clone(),
                eventListeners = m_EventListeners.Clone(),
                updateMask = m_UpdateMask,
                runtime = m_Runtime,

                #if UNITY_EDITOR
                diagnostics = m_Diagnostics
                #endif
            };

            // We don't bring monitors along. InputActions and related classes are equipped
            // with their own domain reload survival logic that will plug actions back into
            // the system after reloads -- *if* the user is serializing them as part of
            // MonoBehaviours/ScriptableObjects.
        }

        internal void RestoreState(SerializedState state)
        {
            m_Devices = null;
            m_HaveDevicesWithStateCallbackReceivers = false;

            InitializeData();
            if (state.runtime != null)
                InstallRuntime(state.runtime);
            InstallGlobals();

            m_StateBuffers = state.buffers;
            m_AvailableDevices = state.availableDevices.ToList();
            m_LayoutRegistrationVersion = state.layoutRegistrationVersion + 1;
            m_DeviceChangeListeners = state.deviceChangeListeners;
            m_DeviceFindLayoutCallbacks = state.deviceFindLayoutCallbacks;
            m_LayoutChangeListeners = state.layoutChangeListeners;
            m_EventListeners = state.eventListeners;
            m_UpdateMask = state.updateMask;

            #if UNITY_EDITOR
            m_Diagnostics = state.diagnostics;
            #endif

            // Configuration.
            InputConfiguration.Restore(state.configuration);

            // Update state.
            InputUpdate.Restore(state.updateState);

            // Layout types.
            foreach (var layout in state.layoutTypes)
            {
                var name = new InternedString(layout.name);
                if (m_Layouts.layoutTypes.ContainsKey(name))
                    continue; // Don't overwrite builtins as they have been updated.
                var type = Type.GetType(layout.typeNameOrJson, false);
                if (type != null)
                    m_Layouts.layoutTypes[name] = type;
                else
                    Debug.Log(string.Format("Input control layout '{0}' has been removed (type '{1}' cannot be found)",
                            layout.name, layout.typeNameOrJson));
            }

            // Layout strings.
            foreach (var layout in state.layoutStrings)
            {
                var name = new InternedString(layout.name);
                if (m_Layouts.layoutStrings.ContainsKey(name))
                    continue; // Don't overwrite builtins as they may have been updated.
                m_Layouts.layoutStrings[name] = layout.typeNameOrJson;
            }

            // Layout factories.
            foreach (var layout in state.layoutFactories)
            {
                var name = new InternedString(layout.name);
                // Don't need to check for builtin version. We don't have builtin layout
                // constructors.

                var type = Type.GetType(layout.typeName, false);
                if (type == null)
                {
                    Debug.Log(string.Format("Layout builder '{0}' has been removed (type '{1}' cannot be found)",
                            name, layout.typeName));
                    continue;
                }

                ////TODO: deal with overloaded methods

                var method = type.GetMethod(layout.methodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                m_Layouts.layoutBuilders[name] = new InputControlLayout.BuilderInfo
                {
                    method = method,
                    instance = layout.instanceJson != null ? JsonUtility.FromJson(layout.instanceJson, type) : null
                };
            }

            // Base layouts.
            if (state.baseLayouts != null)
                foreach (var entry in state.baseLayouts)
                {
                    var name = new InternedString(entry.derivedLayout);
                    if (!m_Layouts.baseLayoutTable.ContainsKey(name))
                        m_Layouts.baseLayoutTable[name] = new InternedString(entry.baseLayout);
                }

            // Layout device matchers.
            if (state.layoutDeviceMatchers != null)
                foreach (var entry in state.layoutDeviceMatchers)
                {
                    var name = new InternedString(entry.layoutName);
                    if (!m_Layouts.layoutDeviceMatchers.ContainsKey(name))
                        m_Layouts.layoutDeviceMatchers[name] = InputDeviceMatcher.FromJson(entry.matcherJson);
                }

            // Type registrations.
            m_Processors.RestoreState(state.processors, "Input processor");
            m_Modifiers.RestoreState(state.processors, "Input binding modifier");
            m_Composites.RestoreState(state.composites, "Input binding composite");

            // Re-create devices.
            var deviceCount = state.devices.Length;
            var devices = new InputDevice[deviceCount];
            var setup = new InputDeviceBuilder(m_Layouts);
            for (var i = 0; i < deviceCount; ++i)
            {
                var deviceState = state.devices[i];

                // See if we still have the layout that the device used. Might have
                // come from a type that was removed in the meantime. If so, just
                // don't re-add the device.
                var layout = new InternedString(deviceState.layout);
                if (!m_Layouts.HasLayout(layout))
                    continue;

                setup.Setup(layout, null, new InternedString(deviceState.variant));
                var device = setup.Finish();
                device.m_Name = new InternedString(deviceState.name);
                device.m_Id = deviceState.deviceId;
                device.m_DeviceIndex = i;
                device.m_Description = deviceState.description;
                if (!string.IsNullOrEmpty(device.m_Description.product))
                    device.m_DisplayName = device.m_Description.product;
                device.m_Flags = deviceState.flags;
                deviceState.RestoreUsagesOnDevice(device);

                device.BakeOffsetIntoStateBlockRecursive(deviceState.stateOffset);
                device.NotifyAdded();
                device.MakeCurrent();

                devices[i] = device;
                m_DevicesById[device.m_Id] = device;

                // Re-install update callback, if necessary.
                var beforeUpdateCallbackReceiver = device as IInputUpdateCallbackReceiver;
                if (beforeUpdateCallbackReceiver != null)
                {
                    // Can't use onUpdate here as that will install the hook. Can't do that
                    // during deserialization.
                    m_UpdateListeners.Append(beforeUpdateCallbackReceiver.OnUpdate);
                }

                m_HaveDevicesWithStateCallbackReceivers |= (device.m_Flags & InputDevice.Flags.HasStateCallbacks) ==
                    InputDevice.Flags.HasStateCallbacks;
            }
            m_Devices = devices;

            ////TODO: retry to make sense of available devices that we couldn't make sense of before; maybe we have a layout now

            // At the moment, there's no support for taking state across domain reloads
            // as we don't have support ATM for taking state across format changes.
            m_StateBuffers.FreeAll();

            ReallocateStateBuffers();
        }

        [SerializeField] private SerializedState m_SerializedState;

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
#if UNITY_EDITOR
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_SerializedState = SaveState();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            RestoreState(m_SerializedState);
            m_SerializedState = default(SerializedState);
        }

#endif
    }
}
