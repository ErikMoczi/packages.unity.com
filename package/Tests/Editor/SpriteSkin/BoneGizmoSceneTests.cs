using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Unity.Collections;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;
using UnityEngine.TestTools;
using NSubstitute;
using UnityEditor.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.Animation.Test.BoneGizmo
{
    using BoneGizmo = UnityEditor.Experimental.U2D.Animation.BoneGizmo;
    public class BoneGizmoSceneTests
    {
        private static SceneView s_SceneView;
        private SpriteSkin m_SpriteSkin;
        private Sprite riggedSprite;

        private static void ValidateDirectory(string path)
        {
            var dirPath = Path.GetDirectoryName(path);

            if (Directory.Exists(dirPath) == false)
                Directory.CreateDirectory(dirPath);
        }

        // SceneView helpers
        private void ResetSceneViewCamera()
        {
            SceneView sceneView = s_SceneView;
            sceneView.camera.transform.position = new Vector3(0f, 0f, -10f);
            sceneView.camera.transform.forward = Vector3.forward;
            sceneView.camera.orthographic = true;
        }

        private Vector2 WorldToSceneViewPosition(Vector3 worldPosition)
        {
            var mousePosition = HandleUtility.WorldToGUIPoint(worldPosition);
            // Adjust for toolbar height
            mousePosition.y += 20 + 16;
            return mousePosition;
        }

        private void SceneViewMouseClick(Vector2 position)
        {
            EditorWindow window = s_SceneView;
            Event ev = new Event();
            ev.mousePosition = position;
            ev.type = EventType.MouseDown;
            window.SendEvent(ev);
            ev.type = EventType.MouseUp;
            window.SendEvent(ev);
        }

        [SetUp]
        public void Setup()
        {
            EditorWindow.GetWindow<SceneView>();
            s_SceneView = SceneView.sceneViews[0] as SceneView;
            s_SceneView.position = new Rect(60f, 90f, 640f, 480f);
            s_SceneView.in2DMode = true;
            s_SceneView.pivot = new Vector3(0f, 0f, -10f);
            s_SceneView.rotation = Quaternion.identity;
            s_SceneView.size = 6.0f;
            s_SceneView.orthographic = true;
            s_SceneView.Focus();

            var go = new GameObject("TestObject");
            riggedSprite = Resources.Load<Sprite>("bird");
            m_SpriteSkin = go.AddComponent<SpriteSkin>();
            m_SpriteSkin.spriteRenderer.sprite = riggedSprite;
            m_SpriteSkin.CreateBoneHierarchy();

            IBoneGizmoToggle boneGizmoToggle = Substitute.For<IBoneGizmoToggle>();
            boneGizmoToggle.enableGizmos.Returns(x => { return true; });

            BoneGizmo.instance.Initialize();
            BoneGizmo.instance.boneGizmoController.boneGizmoToggle = boneGizmoToggle;
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var transform in m_SpriteSkin.boneTransforms)
                transform.parent = null;

            foreach (var transform in m_SpriteSkin.boneTransforms)
                GameObject.DestroyImmediate(transform.gameObject);

            GameObject.DestroyImmediate(m_SpriteSkin.gameObject);
            s_SceneView.Close();
        }

        public class BoneGizmoGOPositionTestCase
        {
            public Vector3 position;

            public override String ToString()
            {
                return position.ToString();
            }
        }

        private static IEnumerable<BoneGizmoGOPositionTestCase> BoneGizmoGOPositionCases()
        {
            yield return new BoneGizmoGOPositionTestCase
            {
                position = new Vector3(3f, 0f, 0f),
            };
            yield return new BoneGizmoGOPositionTestCase
            {
                position = new Vector3(3f, -3f, 0f),
            };
            yield return new BoneGizmoGOPositionTestCase
            {
                position = new Vector3(0f, -3f, 0f),
            };
            yield return new BoneGizmoGOPositionTestCase
            {
                position = new Vector3(-3f, 3f, 0f),
            };
            yield return new BoneGizmoGOPositionTestCase
            {
                position = new Vector3(-3f, 3f, 3f),
            };
            yield return new BoneGizmoGOPositionTestCase
            {
                position = new Vector3(0f, 3f, -3f),
            };
        }

        [UnityTest]
        public IEnumerator BoneGizmo_MouseClick_SelectsClosestBoneGameObject([ValueSource("BoneGizmoGOPositionCases")] BoneGizmoGOPositionTestCase testCase)
        {
            m_SpriteSkin.transform.position = testCase.position;
            Selection.activeGameObject = m_SpriteSkin.gameObject;
            UnityEditor.Tools.current = Tool.None;

            // Get Mouse Position based on actual SceneView state by using SceneView delegate
            bool doingTest = false;
            bool testDone = false;
            Vector2 sceneViewMousePosition = Vector2.zero;
            SceneView.OnSceneFunc sceneViewDelegate = (sceneView) =>
                {
                    if (sceneView != s_SceneView)
                        return;

                    if (!doingTest)
                    {
                        doingTest = true;
                        ResetSceneViewCamera();
                        sceneViewMousePosition = WorldToSceneViewPosition(testCase.position);
                        testDone = true;
                    }
                };

            SceneView.onSceneGUIDelegate += sceneViewDelegate;
            while (!testDone)
            {
                yield return null;
            }
            SceneView.onSceneGUIDelegate -= sceneViewDelegate;

            SceneViewMouseClick(sceneViewMousePosition);

            var selectedGO = m_SpriteSkin.boneTransforms[1].gameObject;
            var selection = Selection.activeGameObject;

            Assert.AreNotEqual(m_SpriteSkin.gameObject, selection);
            Assert.AreEqual(selectedGO, selection);
            Assert.AreEqual(Tool.None, UnityEditor.Tools.current);
        }

        [UnityTest]
        public IEnumerator BoneGizmo_MouseClick_SelectsClosestBoneGameObject_BoneTransformsHaveNoParent([ValueSource("BoneGizmoGOPositionCases")] BoneGizmoGOPositionTestCase testCase)
        {
            m_SpriteSkin.transform.position = testCase.position;
            Selection.activeGameObject = m_SpriteSkin.gameObject;
            UnityEditor.Tools.current = Tool.None;

            foreach (var transform in m_SpriteSkin.boneTransforms)
                transform.parent = null;

            // Get Mouse Position based on actual SceneView state by using SceneView delegate
            bool doingTest = false;
            bool testDone = false;
            Vector2 sceneViewMousePosition = Vector2.zero;
            SceneView.OnSceneFunc sceneViewDelegate = (sceneView) =>
                {
                    if (sceneView != s_SceneView)
                        return;

                    if (!doingTest)
                    {
                        doingTest = true;
                        ResetSceneViewCamera();
                        sceneViewMousePosition = WorldToSceneViewPosition(testCase.position);
                        testDone = true;
                    }
                };

            SceneView.onSceneGUIDelegate += sceneViewDelegate;
            while (!testDone)
            {
                yield return null;
            }
            SceneView.onSceneGUIDelegate -= sceneViewDelegate;

            SceneViewMouseClick(sceneViewMousePosition);

            var selectedGO = m_SpriteSkin.boneTransforms[1].gameObject;
            var selection = Selection.activeGameObject;

            Assert.AreNotEqual(m_SpriteSkin.gameObject, selection);
            Assert.AreEqual(selectedGO, selection);
            Assert.AreEqual(Tool.None, UnityEditor.Tools.current);
        }
    }
}
