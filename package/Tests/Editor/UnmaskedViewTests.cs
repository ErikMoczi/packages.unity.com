using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace Unity.InteractiveTutorials.Tests
{
    public class UnmaskedViewTests
    {
        [Test]
        [Ignore("Annoyingly closes Test Runner window and clears test results of other tests")]
        public void TestGetViewsAndRects_ThrowsArgumentException_WhenTryingToGetRectsFromTwoEditorWindowsInTheSameDockArea()
        {
            EditorUtility.LoadWindowLayout("Packages/com.unity.learn.iet-framework/Tests/Editor/UnmaskedViewTestLayout.dwlt");

            // these two windows are docked together in the test layout
            var unmaskedViews = new[] {
                UnmaskedView.CreateInstanceForEditorWindow<SceneView>(),
                UnmaskedView.CreateInstanceForEditorWindow<GameView>(),
            };

            Assert.Throws<ArgumentException>(
                () => UnmaskedView.GetViewsAndRects(unmaskedViews),
                "Did not throw ArgumentException when getting rects for two EditorWindows in the same DockArea"
                );
        }

        [Test]
        public void TestGetViewsAndRects_ForNamedControlsInToolbar()
        {
            var unmaskedViews = new[] {
                UnmaskedView.CreateInstanceForGUIView<Toolbar>(
                    new[] {
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarPersistentToolsPan" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarPersistentToolsTranslate" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarPersistentToolsRotate" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarPersistentToolsScale" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarPersistentToolsRect" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarToolPivotPositionButton" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarToolPivotOrientationButton" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarPlayModePlayButton" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarPlayModePauseButton" },
                    new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.NamedControl, controlName = "ToolbarPlayModeStepButton" },
                }
                    )
            };

            var viewsAndRects = UnmaskedView.GetViewsAndRects(unmaskedViews).m_MaskData;
            Assert.AreEqual(1, viewsAndRects.Count, "Did not find one view for the Toolbar");
            var rects = viewsAndRects.Values.First().rects;
            Assert.AreEqual(10, rects.Count, "Did not find all of the expected named controls in the Toolbar");
        }

        [UnityTest]
        public IEnumerator TestGetViewsAndRects_ForSerializedPropertyInInspector()
        {
            var testObject = new GameObject("TestGetViewsAndRects_ForSerializedPropertiesInInspector");
            Selection.activeObject = testObject;
            try
            {
                EditorWindow.GetWindow<InspectorWindow>();
                yield return null;

                var unmaskedViews = new[] {
                    UnmaskedView.CreateInstanceForEditorWindow<InspectorWindow>(
                        new[] {
                        new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.Property, targetType = typeof(Transform), propertyPath = "m_LocalPosition" }
                    }
                        )
                };
                var viewsAndRects = UnmaskedView.GetViewsAndRects(unmaskedViews).m_MaskData;
                Assert.AreEqual(1, viewsAndRects.Count, "Did not find one view for the Inspector");
                var rects = viewsAndRects.Values.First().rects;
                Assert.AreEqual(1, rects.Count, "Did not find exactly one control for the SerializedProperty m_LocalPosition for a Transform");
            }
            finally
            {
                GameObject.DestroyImmediate(testObject);
            }
        }

        [UnityTest]
        public IEnumerator TestGetViewsAndRects_ForSerializedPropertyInInspector_WhenSamePathExistsOnMultipleComponents()
        {
            var testObject = new GameObject("TestGetViewsAndRects_ForSerializedPropertiesInInspector", typeof(Light), typeof(SpriteRenderer));

            Selection.activeObject = testObject;
            try
            {
                Assert.IsNotNull(new SerializedObject(testObject.GetComponent<Light>()).FindProperty("m_Color"));
                Assert.IsNotNull(new SerializedObject(testObject.GetComponent<SpriteRenderer>()).FindProperty("m_Color"));

                EditorWindow.GetWindow<InspectorWindow>();
                yield return null;

                var unmaskedViews = new[] {
                    UnmaskedView.CreateInstanceForEditorWindow<InspectorWindow>(
                        new[] {
                        new GUIControlSelector() { selectorMode = GUIControlSelector.Mode.Property, targetType = typeof(SpriteRenderer), propertyPath = "m_Color" }
                    }
                        )
                };
                var viewsAndRects = UnmaskedView.GetViewsAndRects(unmaskedViews).m_MaskData;
                Assert.AreEqual(1, viewsAndRects.Count, "Did not find one view for the Inspector");
                var rects = viewsAndRects.Values.First().rects;
                Assert.AreEqual(1, rects.Count, "Did not find exactly one control for the SerializedProperty m_Color for a SpriteRenderer");
            }
            finally
            {
                GameObject.DestroyImmediate(testObject);
            }
        }
    }
}
