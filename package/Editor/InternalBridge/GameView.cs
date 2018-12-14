
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny.Bridge
{
    internal static class GameView
    {
        #region static
        private static readonly PropertyInfo s_SelectedSizeIndexProp;

        private static readonly bool s_ConfiguredProperly;

        private const string k_TinyName = "Tiny";
        private const string k_FreeAspect = "Free Aspect";

        public static GameViewSizeGroup StandaloneGroup => GameViewSizes.instance.GetGroup(GameViewSizeGroupType.Standalone);

        static GameView()
        {
            // Verify that we can find everything we are looking for and cache it.
            // If we didn't get everything, we disable the feature altogether.
            try
            {
                s_SelectedSizeIndexProp = typeof(UnityEditor.GameView).GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                s_ConfiguredProperly = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ShowErrorMessage();
            }
        }
        #endregion

        #region API
        
        /// <summary>
        /// Sets the size of all GameViews by creating a `Tiny (width:height)` size.
        /// </summary>
        /// <param name="width">The width of the GameView.</param>
        /// <param name="height">The height of the GameView.</param>
        public static void SetSize(int width, int height)
        {
            if (!s_ConfiguredProperly)
            {
                ShowErrorMessage();
                return;
            }

            RemoveTinySize();

            if (width <= 0)
            {
                width = 1;
            }

            if (height <= 0)
            {
                height = 1;
            }

            if (width < height && (width / (float) height) < 0.01)
            {
                width = (int) (height * 0.01);
            }
            else if (height < width && (height / (float) width) < 0.01)
            {
                height = (int) (width * 0.01);
            }
            
            AddCustomSize(width, height, k_TinyName);
            SetSize(k_TinyName);
        }

        /// <summary>
        /// Sets all GameViews to be in FreeAspect mode.
        /// </summary>
        public static void SetFreeAspect()
        {
            if (!s_ConfiguredProperly)
            {
                ShowErrorMessage();
                return;
            }
            
            RemoveTinySize();
            SetSize(k_FreeAspect);
        }

        public static void RepaintAll()
        {
            UnityEditor.GameView.RepaintAll();
        }
        #endregion

        #region Implementation
        
        private static bool IsWindowOpen()
        {
            return Resources.FindObjectsOfTypeAll<UnityEditor.GameView>().Length > 0;
        }

        private static void RemoveTinySize()
        {
            RemoveCustomSize(k_TinyName);
        }

        private static void SetSize(string name)
        {
            SetSize(FindSize(name));
        }
        
        private static bool SetSize(int index)
        {
            if (!IsWindowOpen())
            {
                return false;
            }
            
            var focusedWindow = EditorWindow.focusedWindow;
            var windows = Resources.FindObjectsOfTypeAll<UnityEditor.GameView>();
            var anyChanged = false;
            foreach (var window in windows)
            {
                if ((int)s_SelectedSizeIndexProp.GetValue(window, null) != index)
                {
                    s_SelectedSizeIndexProp.SetValue(window, index, null);
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                focusedWindow?.Focus();
                UnityEditor.GameView.RepaintAll();
            }

            return anyChanged;
        }

        private static void AddCustomSize(int width, int height, string text)
        {
            var newSize = new GameViewSize(GameViewSizeType.AspectRatio, width, height, text);
            StandaloneGroup.AddCustomSize(newSize);
        }

        private static void RemoveCustomSize(string text)
        {
            var index = FindSize(text);
            if (index == -1)
            {
                return;
            }

            StandaloneGroup.RemoveCustomSize(index);
        }

        private static int FindSize(string text)
        {
            var displayTexts = StandaloneGroup.GetDisplayTexts();
            for (var i = 0; i < displayTexts.Length; i++)
            {
                var display = displayTexts[i];
                var pren = display.IndexOf('(');
                if (pren != -1)
                    display = display.Substring(0, pren - 1);
                if (display == text)
                    return i;
            }
            return -1;
        }

        private static void ShowErrorMessage()
        {
            Debug.Log($"{k_TinyName}: GameViewUtility has not been configured properly. {k_TinyName} cannot resize the GameView");
        }
        #endregion
    }
}
