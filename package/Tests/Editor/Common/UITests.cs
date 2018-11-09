using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal abstract class UITests<TWindow> where TWindow : EditorWindow
    {
        protected TWindow Window { get; set; }
        protected VisualElement Container { get { return Window.rootVisualElement; } }
        protected MockOperationFactory Factory { get; private set; }

        [OneTimeSetUp]
        protected void OneTimeSetUp()
        {
            Factory = new MockOperationFactory();
            OperationFactory.Instance = Factory;

            Window = EditorWindow.GetWindow<TWindow>();
            Window.Show();
        }

        [OneTimeTearDown]
        protected void OneTimeTearDown()
        {
            OperationFactory.Reset();
        }

        protected static Error MakeError(ErrorCode code, string message)
        {
            var error = "{\"errorCode\" : " + (uint)code + ", \"message\" : \"" + message + "\"}";
            return JsonUtility.FromJson<Error>(error);
        }
    }
}
