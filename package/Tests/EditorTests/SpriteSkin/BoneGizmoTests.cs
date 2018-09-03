using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.U2D;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.Animation.Test.BoneGizmoTests
{
    public class BoneGizmoTests
    {
        private static SceneView s_SceneView;
        private GameObject go;
        private Sprite riggedSprite;

        private static string kTestAssetsFolder = "Packages/com.unity.2d.animation/Tests/EditorTests/SpriteSkin/Assets/";
        private static string kTestTempFolder = "Assets/Temp/";

        [OneTimeTearDown]
        public void FullTeardown()
        {
            // Delete cloned sprites
            AssetDatabase.DeleteAsset(Path.GetDirectoryName(kTestTempFolder));
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            CloneSpriteForTest("bird.png");
        }

        private static void CloneSpriteForTest(string filename)
        {
            ValidateDirectory(kTestTempFolder);

            File.Copy(kTestAssetsFolder + filename, kTestTempFolder + filename);
            File.Copy(kTestAssetsFolder + filename + ".meta", kTestTempFolder + filename + ".meta");

            AssetDatabase.Refresh();
        }

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
        }

        private Vector2 WorldToSceneViewPosition(Vector3 worldPosition)
        {
            SceneView sceneView = s_SceneView;
            var screenPosition = EditorGUIUtility.PixelsToPoints(sceneView.camera.WorldToScreenPoint(worldPosition));
            // Adjust for toolbar height
            var mousePosition = new Vector2(screenPosition.x, screenPosition.y + 20 + 22);
            return mousePosition;
        }

        private void SceneViewMouseClick(Vector2 position)
        {
            EditorWindow window = s_SceneView;
            window.Focus();

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
            s_SceneView.position = new Rect(0, 0, 640, 480);

            go = new GameObject("TestObject", typeof(SpriteRenderer), typeof(UnityEngine.Experimental.U2D.Animation.SpriteSkin));
            go.transform.position = Vector3.zero;

            riggedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Temp/bird.png");
            go.GetComponent<SpriteRenderer>().sprite = riggedSprite;

            var bones = riggedSprite.GetBones();
            var rootBoneGO = SpriteBoneUtility.CreateSkeleton(bones, go, null);
            go.GetComponent<UnityEngine.Experimental.U2D.Animation.SpriteSkin>().rootBone = rootBoneGO.transform;
        }

        [TearDown]
        public void Teardown()
        {
            GameObject.DestroyImmediate(go);
            s_SceneView.Close();
        }

        [Test]
        public void BoneGizmo_MouseClick_SelectsClosestBoneGameObject()
        {
            Selection.activeGameObject = go;
            Tools.current = Tool.None;

            ResetSceneViewCamera();
            SceneViewMouseClick(WorldToSceneViewPosition(Vector3.zero));

            var selectedGO = go.transform.GetChild(0).GetChild(0).gameObject;
            var selection = Selection.activeGameObject;

            Assert.AreNotEqual(go, selection);
            Assert.AreEqual(selectedGO, selection);
            Assert.AreEqual(Tool.Transform, Tools.current);
        }
    }
}
