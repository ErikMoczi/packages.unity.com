using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.TestTools.Constraints;
using UnityEngine.TestTools.Utils;
using Is = UnityEngine.TestTools.Constraints.Is;
using Property = NUnit.Framework.PropertyAttribute;

#pragma warning disable CS0649
partial class CoreTests
{
    // This is one of the most central tests. If this one breaks, it most often
    // hints at the state layouting or state updating machinery being borked.
    [Test]
    [Category("Events")]
    public void Events_CanUpdateStateOfDeviceWithEvent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var newState = new GamepadState {leftStick = new Vector2(0.123f, 0.456f)};

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.123f));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.456f));
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdateStateOfDeviceWithEvent_SentFromUpdateCallback()
    {
        var device = InputSystem.AddDevice<CustomDeviceWithUpdate>();

        InputSystem.Update();

        Assert.That(device.onUpdateCallCount, Is.EqualTo(1));
        Assert.That(device.onUpdateType, Is.EqualTo(InputUpdateType.Dynamic));
        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.234).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdatePartialStateOfDeviceWithEvent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Full state update to make sure we won't be overwriting other
        // controls with state. Also, make sure we actually carry over
        // those values on buffer flips.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 0xffffffff,
                rightStick = Vector2.one,
                leftTrigger = 0.123f,
                rightTrigger = 0.456f
            });
        InputSystem.Update();

        // Update just left stick.
        InputSystem.QueueDeltaStateEvent(gamepad.leftStick, new Vector2(0.5f, 0.5f));
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(1).Within(0.000001));
    }

    #if UNITY_2018_3_OR_NEWER
    [Test]
    [Category("Events")]
    [Ignore("TODO")]
    public void TODO_Events_ProcessingStateEvent_DoesNotAllocateMemory()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        ////REVIEW: We do some analytics stuff on the first update that allocates. Probably there's
        ////        a better way to handle this.
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.one });

        ////FIXME: seeing odd allocations that seem be triggered by the noise filtering stuff
        Assert.That(() => InputSystem.Update(), Is.Not.AllocatingGCMemory());
    }

    #endif

    [Test]
    [Category("Events")]
    public void Events_TakeDeviceOffsetsIntoAccount()
    {
        InputSystem.AddDevice<Gamepad>();
        var secondGamepad = InputSystem.AddDevice<Gamepad>();

        // Full state updates to make sure we won't be overwriting other
        // controls with state. Also, make sure we actually carry over
        // those values on buffer flips.
        InputSystem.QueueStateEvent(secondGamepad,
            new GamepadState
            {
                buttons = 0xffffffff,
                rightStick = Vector2.one,
                leftTrigger = 0.123f,
                rightTrigger = 0.456f
            });
        InputSystem.Update();

        // Update just left stick.
        InputSystem.QueueDeltaStateEvent(secondGamepad.leftStick, new Vector2(0.5f, 0.5f));
        InputSystem.Update();

        Assert.That(secondGamepad.leftStick.x.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(secondGamepad.leftStick.y.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_UseCurrentTimeByDefault()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        runtime.currentTime = 1234;
        runtime.currentTimeOffsetToRealtimeSinceStartup = 1123;

        double? receivedTime = null;
        double? receivedInternalTime = null;
        InputSystem.onEvent +=
            eventPtr =>
        {
            receivedTime = eventPtr.time;
            receivedInternalTime = eventPtr.internalTime;
        };

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedTime.HasValue);
        Assert.That(receivedTime.Value, Is.EqualTo(111).Within(0.00001));
        Assert.That(receivedInternalTime.Value, Is.EqualTo(1234).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_AreProcessedInBothFixedAndDynamicUpdateByDefault()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        Assert.That(InputSystem.settings.updateMode, Is.EqualTo(InputSettings.UpdateMode.ProcessEventsInBothFixedAndDynamicUpdate));
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Fixed).valid, Is.True);
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Dynamic).valid, Is.True);
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Manual).valid, Is.False);

        // Push event through in fixed update.
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(mouse.leftButton.isPressed, Is.True);

        // Push event through in dynamic update.
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(mouse.leftButton.isPressed, Is.False);
        Assert.That(mouse.rightButton.isPressed, Is.True);
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_CanSwitchToFullyManualUpdates()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var receivedOnChange = true;
        InputSystem.onSettingsChange += () => receivedOnChange = true;

        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;

        Assert.That(InputSystem.settings.updateMode, Is.EqualTo(InputSettings.UpdateMode.ProcessEventsManually));
        Assert.That(receivedOnChange, Is.True);
        Assert.That(InputSystem.GetMetrics().currentStateSizeInBytes,
            Is.LessThanOrEqualTo(InputSystem.GetMetrics().maxStateSizeInBytes - mouse.stateBlock.alignedSizeInBytes));
        Assert.That(runtime.updateMask & InputUpdateType.Fixed, Is.EqualTo(InputUpdateType.None));
        Assert.That(runtime.updateMask & InputUpdateType.Dynamic, Is.EqualTo(InputUpdateType.None));
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Fixed).valid, Is.False);
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Dynamic).valid, Is.False);
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Manual).valid, Is.True);

        #if UNITY_EDITOR
        // Edit mode updates shouldn't have been disabled in editor.
        Assert.That(runtime.updateMask & InputUpdateType.Editor, Is.Not.Zero);
        #endif

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update(InputUpdateType.Manual);

        Assert.That(mouse.leftButton.isPressed, Is.True);
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_CanSwitchToProcessingInFixedUpdatesOnly()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var receivedOnChange = true;
        InputSystem.onSettingsChange += () => receivedOnChange = true;

        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdateOnly;

        Assert.That(InputSystem.settings.updateMode, Is.EqualTo(InputSettings.UpdateMode.ProcessEventsInFixedUpdateOnly));
        Assert.That(receivedOnChange, Is.True);
        Assert.That(InputSystem.GetMetrics().currentStateSizeInBytes,
            Is.LessThanOrEqualTo(InputSystem.GetMetrics().maxStateSizeInBytes - mouse.stateBlock.alignedSizeInBytes));
        Assert.That(runtime.updateMask & InputUpdateType.Fixed, Is.EqualTo(InputUpdateType.Fixed));
        Assert.That(runtime.updateMask & InputUpdateType.Dynamic, Is.EqualTo(InputUpdateType.None));
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Fixed).valid, Is.True);
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Dynamic).valid, Is.False);
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Manual).valid, Is.False);

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(mouse.leftButton.isPressed, Is.True);
    }

    [Test]
    [Category("Events")]
    public void Events_CanSwitchToProcessingInDynamicUpdatesOnly()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var receivedOnChange = true;
        InputSystem.onSettingsChange += () => receivedOnChange = true;

        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdateOnly;

        Assert.That(InputSystem.settings.updateMode, Is.EqualTo(InputSettings.UpdateMode.ProcessEventsInDynamicUpdateOnly));
        Assert.That(receivedOnChange, Is.True);
        Assert.That(InputSystem.GetMetrics().currentStateSizeInBytes,
            Is.LessThanOrEqualTo(InputSystem.GetMetrics().maxStateSizeInBytes - mouse.stateBlock.alignedSizeInBytes));
        Assert.That(runtime.updateMask & InputUpdateType.Fixed, Is.EqualTo(InputUpdateType.None));
        Assert.That(runtime.updateMask & InputUpdateType.Dynamic, Is.EqualTo(InputUpdateType.Dynamic));
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Fixed).valid, Is.False);
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Dynamic).valid, Is.True);
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Manual).valid, Is.False);

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(mouse.leftButton.isPressed, Is.True);
    }

    ////TODO: temporary; remove when the native changes have landed in publc 2019.1
    #if false
    //#if UNITY_2019_1_OR_NEWER
    [Test]
    [Category("Events")]
    public unsafe void Events_AreTimeslicedByDefault()
    {
        runtime.fixedUpdateIntervalInSeconds = 1.0 / 60; // 60 FPS.

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedEvents = new List<InputEvent>();
        InputSystem.onEvent +=
            eventPtr => receivedEvents.Add(*eventPtr.ToPointer());

        // First fixed update should just take everything.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.1234f }, 1);
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.2345f }, 2);
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.3456f }, 2.9);

        runtime.currentTime = 3;

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(3));
        Assert.That(receivedEvents[0].time, Is.EqualTo(1).Within(0.00001));
        Assert.That(receivedEvents[1].time, Is.EqualTo(2).Within(0.00001));
        Assert.That(receivedEvents[2].time, Is.EqualTo(2.9).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.3456).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.Zero);
        Assert.That(InputUpdate.s_LastFixedUpdateTime, Is.EqualTo(3).Within(0.0001));

        receivedEvents.Clear();

        // From now on, fixed updates should only take what falls in their slice.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.1234f }, 3 + 0.001);
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.2345f }, 3 + 0.002);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.3456f}, 3 + 1.0 / 60 + 0.001);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.4567f}, 3 + 2 * (1.0 / 60) + 0.001);

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(2));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 0.001).Within(0.00001));
        Assert.That(receivedEvents[1].time, Is.EqualTo(3 + 0.002).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.2345).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.EqualTo(2));
        Assert.That(InputUpdate.s_LastFixedUpdateTime, Is.EqualTo(3 + 1.0 / 60).Within(0.0001));

        receivedEvents.Clear();

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(1));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 1.0 / 60 + 0.001).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.3456).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.EqualTo(1));
        Assert.That(InputUpdate.s_LastFixedUpdateTime, Is.EqualTo(3 + 2 * (1.0 / 60)).Within(0.0001));

        receivedEvents.Clear();

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(1));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 2 * (1.0 / 60) + 0.001).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.4567).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.Zero);
        Assert.That(InputUpdate.s_LastFixedUpdateTime, Is.EqualTo(3 + 3 * (1.0 / 60)).Within(0.0001));

        receivedEvents.Clear();

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.Zero);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.4567).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.Zero);
        Assert.That(InputUpdate.s_LastFixedUpdateTime, Is.EqualTo(3 + 4 * (1.0 / 60)).Within(0.0001));
    }

    #endif

    [Test]
    [Category("Events")]
    public unsafe void Events_TimeslicingCanBeTurnedOff()
    {
        runtime.fixedUpdateIntervalInSeconds = 1.0 / 60; // 60 FPS.

        // Get first update out of the way with timeslicing on. First fixed update will consume all
        // input so we can't really tell the difference.
        InputSystem.Update(InputUpdateType.Fixed);

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedEvents = new List<InputEvent>();
        InputSystem.onEvent +=
            eventPtr => receivedEvents.Add(*eventPtr.ToPointer());

        bool? receivedOnSettingsChange = null;
        InputSystem.onSettingsChange += () => receivedOnSettingsChange = true;

        runtime.currentTime = 3;

        InputSystem.settings.timesliceEvents = false;

        Assert.That(receivedOnSettingsChange, Is.True);

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.1234f }, 3 + 0.001);
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.2345f }, 3 + 0.002);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.3456f}, 3 + 1.0 / 60 + 0.001);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.4567f}, 3 + 2 * (1.0 / 60) + 0.001);

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(4));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 0.001).Within(0.00001));
        Assert.That(receivedEvents[1].time, Is.EqualTo(3 + 0.002).Within(0.00001));
        Assert.That(receivedEvents[2].time, Is.EqualTo(3 + 1.0 / 60 + 0.001).Within(0.00001));
        Assert.That(receivedEvents[3].time, Is.EqualTo(3 + 2 * (1.0 / 60) + 0.001).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.4567).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.Zero);
    }

    [Test]
    [Category("Events")]
    public void Events_CanGetAverageEventLag()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        runtime.advanceTimeEachDynamicUpdate = 0;
        runtime.currentTime = 10;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A), 6);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.123f, 0.234f)}, 1);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A), 10);
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.234f, 0.345f)}, 3);
        InputSystem.Update();

        var metrics = InputSystem.GetMetrics();

        Assert.That(metrics.averageLagTimePerEvent, Is.EqualTo((9 + 7 + 4 + 0) / 4.0).Within(0.0001));
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanInitializeStateEventFromDevice()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.QueueStateEvent(mouse, new MouseState {delta = Vector2.one});
        InputSystem.Update();

        using (var buffer = StateEvent.From(mouse, out var eventPtr))
        {
            Assert.IsTrue(mouse.delta.x.ReadValueFromEvent(eventPtr, out var xVal));
            Assert.That(xVal, Is.EqualTo(1).Within(0.00001));

            Assert.IsTrue(mouse.delta.y.ReadValueFromEvent(eventPtr, out var yVal));
            Assert.That(yVal, Is.EqualTo(1).Within(0.00001));

            var stateEventPtr = StateEvent.From(eventPtr);
            Assert.That(stateEventPtr->baseEvent.deviceId, Is.EqualTo(mouse.id));
            Assert.That(stateEventPtr->baseEvent.time, Is.EqualTo(runtime.currentTime));
            Assert.That(stateEventPtr->baseEvent.sizeInBytes, Is.EqualTo(buffer.Length));
            Assert.That(stateEventPtr->baseEvent.sizeInBytes,
                Is.EqualTo(InputEvent.kBaseEventSize + sizeof(FourCC) + mouse.stateBlock.alignedSizeInBytes));
            Assert.That(stateEventPtr->stateSizeInBytes, Is.EqualTo(mouse.stateBlock.alignedSizeInBytes));
            Assert.That(stateEventPtr->stateFormat, Is.EqualTo(mouse.stateBlock.format));
        }
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_SendingStateToDeviceWithoutBeforeRenderEnabled_DoesNothingInBeforeRenderUpdate()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var newState = new GamepadState {leftStick = new Vector2(0.123f, 0.456f)};

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(default(Vector2)));
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_SendingStateToDeviceWithBeforeRenderEnabled_UpdatesDeviceInBeforeRender()
    {
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterLayout(deviceJson);

        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        var newState = new GamepadState {leftTrigger = 0.123f};

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123f).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanListenToEventStream()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputSystem.onEvent += inputEvent =>
        {
            ++receivedCalls;
            Assert.That(inputEvent.IsA<StateEvent>(), Is.True);
            Assert.That(inputEvent.deviceId, Is.EqualTo(device.id));
        };

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    // Should be possible to have a pointer to a state event and from it, return
    // the list of controls that have non-default values.
    // Probably makes sense to also be able to return from it a list of changed
    // controls by comparing it to a device's current state.
    [Test]
    [Category("Events")]
    [Ignore("TODO")]
    public void TODO_Events_CanFindActiveControlsFromStateEvent()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_AreProcessedInOrderTheyAreQueuedIn()
    {
        const double kFirstTime = 0.5;
        const double kSecondTime = 1.5;
        const double kThirdTime = 2.5;

        var receivedCalls = 0;
        var receivedFirstTime = 0.0;
        var receivedSecondTime = 0.0;
        var receivedThirdTime = 0.0;

        InputSystem.onEvent +=
            inputEvent =>
        {
            ++receivedCalls;
            if (receivedCalls == 1)
                receivedFirstTime = inputEvent.time;
            else if (receivedCalls == 2)
                receivedSecondTime = inputEvent.time;
            else
                receivedThirdTime = inputEvent.time;
        };

        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(device, new GamepadState(), kSecondTime);
        InputSystem.QueueStateEvent(device, new GamepadState(), kFirstTime);
        InputSystem.QueueStateEvent(device, new GamepadState(), kThirdTime);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(3));
        Assert.That(receivedFirstTime, Is.EqualTo(kSecondTime).Within(0.00001));
        Assert.That(receivedSecondTime, Is.EqualTo(kFirstTime).Within(0.00001));
        Assert.That(receivedThirdTime, Is.EqualTo(kThirdTime).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_CanQueueAndReceiveEventsAgainstNonExistingDevices()
    {
        // Device IDs are looked up only *after* the system shows the event to us.

        var receivedCalls = 0;
        var receivedDeviceId = InputDevice.kInvalidDeviceId;
        InputSystem.onEvent +=
            eventPtr =>
        {
            ++receivedCalls;
            receivedDeviceId = eventPtr.deviceId;
        };

        var inputEvent = DeviceConfigurationEvent.Create(4, 1.0);
        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDeviceId, Is.EqualTo(4));
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_HandledFlagIsResetWhenEventIsQueued()
    {
        var receivedCalls = 0;
        var wasHandled = true;

        InputSystem.onEvent +=
            eventPtr =>
        {
            ++receivedCalls;
            wasHandled = eventPtr.handled;
        };

        var inputEvent = DeviceConfigurationEvent.Create(4, 1.0);

        // This should go back to false when we inputEvent goes on the queue.
        // The way the behavior is implemented is a side-effect of how we store
        // the handled flag as a bit on the event ID -- which will get set by
        // native on an event when it is queued.
        inputEvent.baseEvent.handled = true;

        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(wasHandled, Is.False);
    }

    [Test]
    [Category("Events")]
    public void Events_CanPreventEventsFromBeingProcessed()
    {
        InputSystem.onEvent +=
            inputEvent =>
        {
            // If we mark the event handled, the system should skip it and not
            // let it go to the device.
            inputEvent.handled = true;
        };

        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(device, new GamepadState {rightTrigger = 0.45f});
        InputSystem.Update();

        Assert.That(device.rightTrigger.ReadValue(), Is.EqualTo(0.0).Within(0.00001));
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    struct StateWith2Bytes : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")]
        [FieldOffset(0)] public ushort value;
        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }

    [InputControlLayout(stateType = typeof(StateWith2Bytes))]
    class DeviceWith2ByteState : InputDevice
    {
    }

    // This test pertains mostly to how the input runtime handles events so it's of limited
    // use in our current test setup with InputTestRuntime. There's an equivalent native test
    // in the Unity runtime to ensure the constraint.
    //
    // Previously we used to actually modify event size to always be 4 byte aligned and thus potentially
    // added padding to events. This is a bad idea. The C# system can't tell between padding added to an
    // event and valid input data that's part of the state. This can cause the padding to actually overwrite
    // state of controls that happen to start at the end of an event. On top, we didn't clear out the
    // memory we added to an event and thus ended up with random garbage being written to unrelated controls.
    //
    // What we do now is to simply align event pointers to 4 byte boundaries as we read and write events.
    [Test]
    [Category("Events")]
    public void Events_CanHandleStateNotAlignedTo4ByteBoundary()
    {
        Debug.Assert(UnsafeUtility.SizeOf<StateWith2Bytes>() == 2);

        var device = InputSystem.AddDevice<DeviceWith2ByteState>();

        InputSystem.QueueStateEvent(device, new StateWith2Bytes());
        InputSystem.QueueStateEvent(device, new StateWith2Bytes());

        InputSystem.onEvent +=
            eventPtr =>
        {
            // Event addresses must be 4-byte aligned but sizeInBytes must not have been altered.
            Assert.That(eventPtr.data.ToInt64() % 4, Is.EqualTo(0));
            Assert.That(eventPtr.sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<StateWith2Bytes>()));
        };

        InputSystem.Update();
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public unsafe void Events_CanTraceEventsOfDevice()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        var noise = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace {deviceId = device.id})
        {
            trace.Enable();
            Assert.That(trace.enabled, Is.True);

            var firstState = new GamepadState {rightTrigger = 0.35f};
            var secondState = new GamepadState {leftTrigger = 0.75f};

            InputSystem.QueueStateEvent(device, firstState, 0.5);
            InputSystem.QueueStateEvent(device, secondState, 1.5);
            InputSystem.QueueStateEvent(noise, new GamepadState()); // This one just to make sure we don't get it.

            InputSystem.Update();

            trace.Disable();

            var events = trace.ToList();

            Assert.That(events, Has.Count.EqualTo(2));

            Assert.That(events[0].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[0].deviceId, Is.EqualTo(device.id));
            Assert.That(events[0].time, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(events[0].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref firstState),
                StateEvent.From(events[0])->state, UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);

            Assert.That(events[1].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[1].deviceId, Is.EqualTo(device.id));
            Assert.That(events[1].time, Is.EqualTo(1.5).Within(0.000001));
            Assert.That(events[1].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref secondState),
                StateEvent.From(events[1])->state, UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);
        }
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_WhenTraceIsFull_WillStartOverwritingOldEvents()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        using (var trace =
                   new InputEventTrace(StateEvent.GetEventSizeWithPayload<GamepadState>() * 2) {deviceId = device.id})
        {
            trace.Enable();

            var firstState = new GamepadState {rightTrigger = 0.35f};
            var secondState = new GamepadState {leftTrigger = 0.75f};
            var thirdState = new GamepadState {leftTrigger = 0.95f};

            InputSystem.QueueStateEvent(device, firstState, 0.5);
            InputSystem.QueueStateEvent(device, secondState, 1.5);
            InputSystem.QueueStateEvent(device, thirdState, 2.5);

            InputSystem.Update();

            trace.Disable();

            var events = trace.ToList();

            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events, Has.Exactly(1).With.Property("time").EqualTo(1.5).Within(0.000001));
            Assert.That(events, Has.Exactly(1).With.Property("time").EqualTo(2.5).Within(0.000001));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanClearEventTrace()
    {
        using (var trace = new InputEventTrace())
        {
            trace.Enable();

            var device = InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(device, new GamepadState());
            InputSystem.QueueStateEvent(device, new GamepadState());
            InputSystem.Update();

            Assert.That(trace.ToList(), Has.Count.EqualTo(2));

            trace.Clear();

            Assert.That(trace.ToList(), Has.Count.EqualTo(0));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_GetUniqueIds()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.QueueStateEvent(device, new GamepadState());

        var receivedCalls = 0;
        var firstId = InputEvent.kInvalidId;
        var secondId = InputEvent.kInvalidId;

        InputSystem.onEvent +=
            eventPtr =>
        {
            ++receivedCalls;
            if (receivedCalls == 1)
                firstId = eventPtr.id;
            else if (receivedCalls == 2)
                secondId = eventPtr.id;
        };

        InputSystem.Update();

        Assert.That(firstId, Is.Not.EqualTo(secondId));
    }

    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_IfOldStateEventIsSentToDevice_IsIgnored()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0.5f}, 2.0);
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0.75f}, 1.0);
        InputSystem.Update();

        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.5f).Within(0.000001));
    }

    // This is another case of IInputStateCallbackReceiver making everything more complicated by deviating from
    // the common, simple code path. Basically, what this test here is trying to ensure is that we can send
    // touch states to a Touchscreen and not have them rejected because of timestamps. It's easy to order the
    // events for a single touch correctly but ordering them for all touches would require backends to make
    // a sorting pass over all events before queueing them.
    [Test]
    [Category("Events")]
    [Property("TimesliceEvents", "Off")]
    public void Events_IfOldStateEventIsSentToDevice_IsIgnored_ExceptIfEventIsHandledByIInputStateCallbackReceiver()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        // Sanity check.
        Assert.That(device is IInputStateCallbackReceiver,
            "Test assumes that Touchscreen implements IInputStateCallbackReceiver");

        InputSystem.QueueStateEvent(device, new TouchState { position = new Vector2(0.123f, 0.234f) }, 2);
        InputSystem.QueueStateEvent(device, new TouchState { position = new Vector2(0.234f, 0.345f) }, 1);// Goes back in time.
        InputSystem.Update();

        Assert.That(device.lastUpdateTime, Is.EqualTo(2).Within(0.00001));
        Assert.That(device.position.ReadValue(), Is.EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));
    }

    private struct CustomNestedDeviceState : IInputStateTypeInfo
    {
        [InputControl(name = "button1", layout = "Button")]
        public int buttons;
        [InputControl(layout = "Axis")] public float axis2;

        public FourCC GetFormat()
        {
            return new FourCC('N', 'S', 'T', 'D');
        }
    }

    private struct CustomDeviceState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")] public float axis;

        public CustomNestedDeviceState nested;

        public FourCC GetFormat()
        {
            return new FourCC('C', 'U', 'S', 'T');
        }
    }

    [InputControlLayout(stateType = typeof(CustomDeviceState))]
    private class CustomDevice : InputDevice
    {
        public AxisControl axis { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            axis = builder.GetControl<AxisControl>(this, "axis");
            base.FinishSetup(builder);
        }
    }

    private class CustomDeviceWithUpdate : CustomDevice, IInputUpdateCallbackReceiver
    {
        public int onUpdateCallCount;
        public InputUpdateType onUpdateType;

        public void OnUpdate(InputUpdateType updateType)
        {
            ++onUpdateCallCount;
            onUpdateType = updateType;
            InputSystem.QueueStateEvent(this, new CustomDeviceState {axis = 0.234f});
        }
    }

    // We want devices to be able to "park" unused controls outside of the state
    // memory region that is being sent to the device in events.
    [Test]
    [Category("Events")]
    public void Events_CanSendSmallerStateToDeviceWithLargerState()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""extend"" : ""CustomDevice"",
                ""controls"" : [
                    { ""name"" : ""extra"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout<CustomDevice>();
        InputSystem.RegisterLayout(json);
        var device = (CustomDevice)InputSystem.AddDevice("TestLayout");

        InputSystem.QueueStateEvent(device, new CustomDeviceState {axis = 0.5f});
        InputSystem.Update();

        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
    }

    private struct ExtendedCustomDeviceState : IInputStateTypeInfo
    {
        public CustomDeviceState baseState;
        public int extra;

        public FourCC GetFormat()
        {
            return baseState.GetFormat();
        }
    }

    // HIDs rely on this behavior as we may only use a subset of a HID's set of
    // controls and thus get state events that are larger than the device state
    // that we store for the HID.
    [Test]
    [Category("Events")]
    public void Events_CanSendLargerStateToDeviceWithSmallerState()
    {
        var device = InputSystem.AddDevice<CustomDevice>();

        var state = new ExtendedCustomDeviceState {baseState = {axis = 0.5f}};
        InputSystem.QueueStateEvent(device, state);
        InputSystem.Update();

        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanDetectWhetherControlIsPartOfEvent()
    {
        // We use a mouse here as it has several controls that are "parked" outside MouseState.
        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.onEvent +=
            eventPtr =>
        {
            // For every control that isn't contained in a state event, GetStatePtrFromStateEvent() should
            // return IntPtr.Zero.
            if (eventPtr.IsA<StateEvent>())
            {
                Assert.That(mouse.position.GetStatePtrFromStateEvent(eventPtr) != null);
                Assert.That(mouse.tilt.GetStatePtrFromStateEvent(eventPtr) == null);
            }
            else if (eventPtr.IsA<DeltaStateEvent>())
            {
                Assert.That(mouse.position.GetStatePtrFromStateEvent(eventPtr) != null);
                Assert.That(mouse.leftButton.GetStatePtrFromStateEvent(eventPtr) == null);
            }
            else
            {
                Assert.Fail("Unexpected type of event");
            }
        };

        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.QueueDeltaStateEvent(mouse.position, new Vector2(0.5f, 0.5f));
        InputSystem.Update();
    }

    [Test]
    [Category("Events")]
    public void Events_CanListenForWhenAllEventsHaveBeenProcessed()
    {
        InputUpdateType? receivedUpdateType = null;
        Action<InputUpdateType> callback =
            type =>
        {
            Assert.That(receivedUpdateType, Is.Null);
            receivedUpdateType = type;
        };

        InputSystem.onAfterUpdate += callback;

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(receivedUpdateType, Is.EqualTo(InputUpdateType.Dynamic));

        receivedUpdateType = null;
        InputSystem.onAfterUpdate -= callback;

        InputSystem.Update();

        Assert.That(receivedUpdateType, Is.Null);
    }

    [Test]
    [Category("Events")]
    public void Events_EventBuffer_CanIterateEvents()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        unsafe
        {
            using (StateEvent.From(gamepad, out var eventPtr))
            using (var buffer = new InputEventBuffer(eventPtr, 1))
            {
                Assert.That(buffer.eventCount, Is.EqualTo(1));
                Assert.That(buffer.sizeInBytes, Is.EqualTo(InputEventBuffer.kBufferSizeUnknown));
                Assert.That(buffer.capacityInBytes, Is.Zero);
                Assert.That(buffer.bufferPtr, Is.EqualTo(eventPtr));

                var events = buffer.ToArray();
                Assert.That(events, Has.Length.EqualTo(1));
                Assert.That(events[0], Is.EqualTo(eventPtr));
            }
        }
    }

    [Test]
    [Category("Events")]
    public void Events_EventBuffer_CanAddEvents()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        unsafe
        {
            using (StateEvent.From(gamepad, out var eventPtr))
            using (var buffer = new InputEventBuffer())
            {
                // Write two events into buffer.
                gamepad.leftStick.WriteValueIntoEvent(Vector2.one, eventPtr);
                eventPtr.id = 111;
                eventPtr.time = 123;
                eventPtr.handled = false;
                buffer.AppendEvent(eventPtr);
                gamepad.leftStick.WriteValueIntoEvent(Vector2.zero, eventPtr);
                eventPtr.id = 222;
                eventPtr.time = 234;
                eventPtr.handled = true;
                buffer.AppendEvent(eventPtr);

                Assert.That(buffer.eventCount, Is.EqualTo(2));
                var events = buffer.ToArray();

                Assert.That(events, Has.Length.EqualTo(2));
                Assert.That(events[0].type, Is.EqualTo(new FourCC(StateEvent.Type)));
                Assert.That(events[1].type, Is.EqualTo(new FourCC(StateEvent.Type)));
                Assert.That(events[0].time, Is.EqualTo(123).Within(0.00001));
                Assert.That(events[1].time, Is.EqualTo(234).Within(0.00001));
                Assert.That(events[0].id, Is.EqualTo(111));
                Assert.That(events[1].id, Is.EqualTo(222));
                Assert.That(events[0].handled, Is.False);
                Assert.That(events[1].handled, Is.True);
                Assert.That(events[0].deviceId, Is.EqualTo(gamepad.id));
                Assert.That(events[1].deviceId, Is.EqualTo(gamepad.id));
                Assert.That(InputControlExtensions.ReadUnprocessedValueFromEvent(gamepad.leftStick, events[0]), Is.EqualTo(Vector2.one));
                Assert.That(InputControlExtensions.ReadUnprocessedValueFromEvent(gamepad.leftStick, events[1]), Is.EqualTo(Vector2.zero));
            }
        }
    }

    [Test]
    [Category("Events")]
    public void Events_EventBuffer_CanBeReset()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        unsafe
        {
            using (var buffer = new InputEventBuffer())
            {
                buffer.AppendEvent(DeviceConfigurationEvent.Create(gamepad.id, 123).ToEventPtr());
                buffer.AppendEvent(DeviceConfigurationEvent.Create(gamepad.id, 234).ToEventPtr());

                var events = buffer.ToArray();
                Assert.That(events, Has.Length.EqualTo(2));
                Assert.That(events[0].type, Is.EqualTo(new FourCC(DeviceConfigurationEvent.Type)));
                Assert.That(events[1].type, Is.EqualTo(new FourCC(DeviceConfigurationEvent.Type)));

                buffer.Reset();

                Assert.That(buffer.eventCount, Is.Zero);

                buffer.AppendEvent(DeviceRemoveEvent.Create(gamepad.id, 432).ToEventPtr());

                events = buffer.ToArray();

                Assert.That(events.Length, Is.EqualTo(1));
                Assert.That(events[0].type, Is.EqualTo(new FourCC(DeviceRemoveEvent.Type)));
            }
        }
    }

    [Test]
    [Category("Events")]
    public void Events_EventBuffer_CanAllocateEvent()
    {
        unsafe
        {
            using (var buffer = new InputEventBuffer())
            {
                var eventPtr = buffer.AllocateEvent(1024);

                Assert.That(buffer.bufferPtr, Is.EqualTo(new InputEventPtr(eventPtr)));
                Assert.That(buffer.eventCount, Is.EqualTo(1));
                Assert.That(eventPtr->sizeInBytes, Is.EqualTo(1024));
                Assert.That(eventPtr->type, Is.EqualTo(new FourCC()));
            }
        }
    }
}
