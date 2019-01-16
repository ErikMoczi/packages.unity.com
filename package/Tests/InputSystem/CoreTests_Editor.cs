#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Composites;
using UnityEngine.Experimental.Input.Editor;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.TestTools;
using Object = System.Object;

partial class CoreTests
{
    [Test]
    [Category("Editor")]
    public void Editor_CanSaveAndRestoreState()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterControlLayout(json);
        InputSystem.AddDevice("MyDevice");
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            product = "Product",
            manufacturer = "Manufacturer",
            interfaceName = "Test"
        }.ToJson());
        InputSystem.Update();

        InputSystem.Save();
        InputSystem.Reset();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(0));

        InputSystem.Restore();

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("layout").EqualTo("MyDevice").And.TypeOf<Gamepad>());

        var unsupportedDevices = new List<InputDeviceDescription>();
        InputSystem.GetUnsupportedDevices(unsupportedDevices);

        Assert.That(unsupportedDevices.Count, Is.EqualTo(1));
        Assert.That(unsupportedDevices[0].product, Is.EqualTo("Product"));
        Assert.That(unsupportedDevices[0].manufacturer, Is.EqualTo("Manufacturer"));
        Assert.That(unsupportedDevices[0].interfaceName, Is.EqualTo("Test"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringDeviceFromSave_RestoresRelevantDynamicConfiguration()
    {
        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.SetUsage(device, CommonUsages.LeftHand);
        ////TODO: set variants

        InputSystem.Save();
        InputSystem.Reset();
        InputSystem.Restore();

        var newDevice = InputSystem.devices.First(x => x is Gamepad);

        Assert.That(newDevice.layout, Is.EqualTo("Gamepad"));
        Assert.That(newDevice.usages, Has.Count.EqualTo(1));
        Assert.That(newDevice.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(Gamepad.current, Is.SameAs(newDevice));
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringStateWillCleanUpEventHooks()
    {
        InputSystem.Save();

        var receivedOnEvent = 0;
        var receivedOnDeviceChange = 0;

        InputSystem.onEvent += _ => ++ receivedOnEvent;
        InputSystem.onDeviceChange += (c, d) => ++ receivedOnDeviceChange;

        InputSystem.Restore();

        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedOnEvent, Is.Zero);
        Assert.That(receivedOnDeviceChange, Is.Zero);
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringStateWillRestoreObjectsOfLayoutBuilder()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Gamepad"};
        InputSystem.RegisterControlLayoutBuilder(() => builder.DoIt(), "TestLayout");

        InputSystem.Save();
        InputSystem.Reset();
        InputSystem.Restore();

        var device = InputSystem.AddDevice("TestLayout");

        Assert.That(device, Is.TypeOf<Gamepad>());
    }

    // Editor updates are confusing in that they denote just another point in the
    // application loop where we push out events. They do not mean that the events
    // we send necessarily go to the editor state buffers.
    [Test]
    [Category("Editor")]
    public void Editor_WhenPlaying_EditorUpdatesWriteEventIntoPlayerState()
    {
        InputConfiguration.LockInputToGame = true;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.25f});
        InputSystem.Update(InputUpdateType.Dynamic);

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.75f});
        InputSystem.Update(InputUpdateType.Editor);

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadPreviousValue(), Is.EqualTo(0.25).Within(0.000001));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionMapThroughSerialization()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var obj = new SerializedObject(asset);

        InputActionSerializationHelpers.AddActionMap(obj);
        InputActionSerializationHelpers.AddActionMap(obj);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps[0].name, Is.Not.Null.Or.Empty);
        Assert.That(asset.actionMaps[1].name, Is.Not.Null.Or.Empty);
        Assert.That(asset.actionMaps[0].name, Is.Not.EqualTo(asset.actionMaps[1].name));

        var actionMap2Name = asset.actionMaps[1].name;

        InputActionSerializationHelpers.DeleteActionMap(obj, 0);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps, Has.Count.EqualTo(1));
        Assert.That(asset.actionMaps[0].name, Is.EqualTo(actionMap2Name));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddActionMapFromObject()
    {
        var map = new InputActionMap("set");
        var binding = new InputBinding();
        binding.path = "some path";
        var action = map.AddAction("action");
        action.AppendBinding(binding);

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var obj = new SerializedObject(asset);

        var parameters = new Dictionary<string, string>();
        parameters.Add("m_Name", "set");

        Assert.That(asset.actionMaps, Has.Count.EqualTo(0));

        InputActionSerializationHelpers.AddActionMapFromObject(obj, parameters);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps, Has.Count.EqualTo(1));
        Assert.That(asset.actionMaps[0].name, Is.EqualTo("set"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionThroughSerialization()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action", binding: "/gamepad/leftStick");
        map.AddAction(name: "action1", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AddAction(mapProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps[0].actions, Has.Count.EqualTo(3));
        Assert.That(asset.actionMaps[0].actions[2].name, Is.EqualTo("action2"));
        Assert.That(asset.actionMaps[0].actions[2].bindings, Has.Count.Zero);

        InputActionSerializationHelpers.DeleteAction(mapProperty, 2);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionMaps[0].actions, Has.Count.EqualTo(2));
        Assert.That(asset.actionMaps[0].actions[0].name, Is.EqualTo("action"));
        Assert.That(asset.actionMaps[0].actions[1].name, Is.EqualTo("action1"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveBindingThroughSerialization()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action1", binding: "/gamepad/leftStick");
        map.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AppendBinding(action1Property, mapProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        // Maps and actions aren't UnityEngine.Objects so the modifications will not
        // be in-place. Look up the actions after each apply.
        var action1 = asset.actionMaps[0].TryGetAction("action1");
        var action2 = asset.actionMaps[0].TryGetAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(2));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action1.bindings[1].path, Is.EqualTo(""));
        Assert.That(action1.bindings[1].interactions, Is.EqualTo(""));
        Assert.That(action1.bindings[1].groups, Is.EqualTo(""));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));

        InputActionSerializationHelpers.RemoveBinding(action1Property, 1, mapProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        action1 = asset.actionMaps[0].TryGetAction("action1");
        action2 = asset.actionMaps[0].TryGetAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddBindingFromObject()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action1");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        var pathName = "/gamepad/leftStick";
        var name = "some name";
        var interactionsName = "someinteractions";
        var sourceActionName = "some action";
        var groupName = "group";
        var flags = 10;

        var parameters = new Dictionary<string, string>();
        parameters.Add("path", pathName);
        parameters.Add("name", name);
        parameters.Add("groups", groupName);
        parameters.Add("interactions", interactionsName);
        parameters.Add("flags", "" + flags);
        parameters.Add("action", sourceActionName);

        InputActionSerializationHelpers.AppendBindingFromObject(parameters, action1Property, mapProperty);

        obj.ApplyModifiedPropertiesWithoutUndo();

        var action1 = asset.actionMaps[0].TryGetAction("action1");
        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo(pathName));
        Assert.That(action1.bindings[0].action, Is.EqualTo("action1"));
        Assert.That(action1.bindings[0].groups, Is.EqualTo(groupName));
        Assert.That(action1.bindings[0].interactions, Is.EqualTo(interactionsName));
        Assert.That(action1.bindings[0].name, Is.EqualTo(name));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAppendCompositeBinding()
    {
        var map = new InputActionMap("set");
        map.AddAction(name: "action1");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AppendCompositeBinding(action1Property, mapProperty, "Axis", typeof(AxisComposite));
        obj.ApplyModifiedPropertiesWithoutUndo();

        var action1 = asset.actionMaps[0].TryGetAction("action1");
        Assert.That(action1.bindings, Has.Count.EqualTo(3));
        Assert.That(action1.bindings[0].path, Is.EqualTo("Axis"));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) => x.name == "positive"));
        Assert.That(action1.bindings, Has.Exactly(1).Matches((InputBinding x) => x.name == "negative"));
        Assert.That(action1.bindings[0].isComposite, Is.True);
        Assert.That(action1.bindings[0].isPartOfComposite, Is.False);
        Assert.That(action1.bindings[1].isComposite, Is.False);
        Assert.That(action1.bindings[1].isPartOfComposite, Is.True);
        Assert.That(action1.bindings[2].isComposite, Is.False);
        Assert.That(action1.bindings[2].isPartOfComposite, Is.True);
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGenerateCodeWrapperForInputAsset()
    {
        var set1 = new InputActionMap("set1");
        set1.AddAction(name: "action1", binding: "/gamepad/leftStick");
        set1.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var set2 = new InputActionMap("set2");
        set2.AddAction(name: "action1", binding: "/gamepad/buttonSouth");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(set1);
        asset.AddActionMap(set2);
        asset.name = "MyControls";

        var code = InputActionCodeGenerator.GenerateWrapperCode(asset,
            new InputActionCodeGenerator.Options {namespaceName = "MyNamespace", sourceAssetPath = "test"});

        // Our version of Mono doesn't implement the CodeDom stuff so all we can do here
        // is just perform some textual verification. Once we have the newest Mono, this should
        // use CSharpCodeProvider and at least parse if not compile and run the generated wrapper.

        Assert.That(code, Contains.Substring("namespace MyNamespace"));
        Assert.That(code, Contains.Substring("public class MyControls"));
        Assert.That(code, Contains.Substring("public UnityEngine.Experimental.Input.InputActionMap Clone()"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGenerateCodeWrapperForInputAsset_WhenAssetNameContainsSpacesAndSymbols()
    {
        var set1 = new InputActionMap("set1");
        set1.AddAction(name: "action ^&", binding: "/gamepad/leftStick");
        set1.AddAction(name: "1thing", binding: "/gamepad/leftStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(set1);
        asset.name = "New Controls (4)";

        var code = InputActionCodeGenerator.GenerateWrapperCode(asset,
            new InputActionCodeGenerator.Options {sourceAssetPath = "test"});

        Assert.That(code, Contains.Substring("class NewControls_4_"));
        Assert.That(code, Contains.Substring("public UnityEngine.Experimental.Input.InputAction @action__"));
        Assert.That(code, Contains.Substring("public UnityEngine.Experimental.Input.InputAction @_1thing"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanRenameAction()
    {
        var set1 = new InputActionMap("set1");
        set1.AddAction(name: "action", binding: "/gamepad/leftStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(set1);

        var obj = new SerializedObject(asset);
        var mapProperty = obj.FindProperty("m_ActionMaps").GetArrayElementAtIndex(0);
        var action1Property = mapProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.RenameAction(action1Property, mapProperty, "newAction");
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(set1.actions[0].name, Is.EqualTo("newAction"));
        Assert.That(set1.actions[0].bindings, Has.Count.EqualTo(1));
        Assert.That(set1.actions[0].bindings[0].action, Is.EqualTo("newAction"));
    }

    private class TestEditorWindow : EditorWindow
    {
        public Vector2 mousePosition;

        public void OnGUI()
        {
            mousePosition = Mouse.current.position.ReadValue();
        }
    }

    [Test]
    [Category("Editor")]
    public void TODO_Editor_PointerCoordinatesInEditorWindowOnGUI_AreInEditorWindowSpace()
    {
        Assert.Fail();
    }

    ////TODO: the following tests have to be edit mode tests but it looks like putting them into
    ////      Assembly-CSharp-Editor is the only way to mark them as such

    ////REVIEW: support actions in the editor at all?
    [UnityTest]
    [Category("Editor")]
    public IEnumerator TODO_Editor_ActionSetUpInEditor_DoesNotTriggerInPlayMode()
    {
        throw new NotImplementedException();
    }

    [UnityTest]
    [Category("Editor")]
    public IEnumerator TODO_Editor_PlayerActionDoesNotTriggerWhenGameViewIsNotFocused()
    {
        throw new NotImplementedException();
    }

    ////TODO: tests for InputAssetImporter; for this we need C# mocks to be able to cut us off from the actual asset DB
}
#endif // UNITY_EDITOR
