using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.InteractiveTutorials.Tests
{
    public class LayoutTests
    {
        string m_TempFolderPath;
        Tutorial m_Tutorial;

        [SetUp]
        public void SetUp()
        {
            var tempFolderGUID = AssetDatabase.CreateFolder("Assets", "Temp");
            m_TempFolderPath = AssetDatabase.GUIDToAssetPath(tempFolderGUID);

            m_Tutorial = ScriptableObject.CreateInstance<Tutorial>();
            var page = ScriptableObject.CreateInstance<TutorialPage>();
            m_Tutorial.m_Pages = new Tutorial.TutorialPageCollection(new[] { page });

            var serializedObject = new SerializedObject(m_Tutorial);

            // Ensure tutorial window is not open
            foreach (var window in Resources.FindObjectsOfTypeAll<TutorialWindow>())
            {
                window.Close();
            }

            // Set exit behavior to close window on exit
            var exitBehaviorProperty = serializedObject.FindProperty("m_ExitBehavior");
            exitBehaviorProperty.enumValueIndex = (int)Tutorial.ExitBehavior.CloseWindow;
            serializedObject.ApplyModifiedProperties();

            // TODO: Remove
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow>(), Is.Empty,"TestWindow is present");

            // Save current layout and use it as the tutorial layout
            var tutorialLayoutPath = m_TempFolderPath + "/TutorialLayout.dwlt";
            WindowLayout.SaveWindowLayout(tutorialLayoutPath);
            AssetDatabase.Refresh();
            var tutorialLayout = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tutorialLayoutPath);
            var windowLayoutProperty = serializedObject.FindProperty("m_WindowLayout");
            windowLayoutProperty.objectReferenceValue = tutorialLayout;
            serializedObject.ApplyModifiedProperties();

            // Open a test window
            EditorWindow.GetWindow<TestWindow>();

            // TODO: Remove
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow>(), Is.Not.Empty,"TestWindow is not present");
        }

        [TearDown]
        public void TearDown()
        {
            // Close any left over test windows
            foreach (var window in Resources.FindObjectsOfTypeAll<TestWindow>())
            {
                if (window != null)
                    window.Close();
            }

            AssetDatabase.DeleteAsset(m_TempFolderPath);
        }

        [Test]
        public void ReloadTutorial_WhenTutorialWindowIsNotOpen_SavesAndRestoresOriginalLayout()
        {
            m_Tutorial.ReloadTutorial();

            // TODO: Remove
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow>(), Is.Empty,"TestWindow is present");

            // Complete tutorial
            m_Tutorial.currentPage.ValidateCriteria();
            m_Tutorial.TryGoToNextPage();

            // Assert that original layout is restored (i.e. TestWindow should exist)
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow>(), Is.Not.Empty,"TestWindow is not present");
        }

        [Test]
        public void ReloadTutorial_WhenTutorialWindowIsOpen_DoesNotSaveAndRestoreOriginalLayout()
        {
            EditorWindow.GetWindow<TutorialWindow>();

            m_Tutorial.ReloadTutorial();

            // TODO: Remove
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow>(), Is.Empty,"TestWindow is present");

            // Complete tutorial
            m_Tutorial.currentPage.ValidateCriteria();
            m_Tutorial.TryGoToNextPage();

            // Assert that original layout is not restored (i.e. TestWindow should not exist)
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow>(), Is.Empty,"TestWindow is not present");
        }
    }
}
