using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class AlertFactory : UxmlFactory<Alert>
    {
        protected override Alert DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new Alert(bag.GetPropertyString("text"));
        }
    }
#endif

    internal class Alert : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<Alert, UxmlTraits> { }

        internal new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_Text;

            public UxmlTraits()
            {
                m_Text = new UxmlStringAttributeDescription { name="text" };
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Alert alert = (Alert) ve;
                alert.AlertMessage.text = m_Text.GetValueFromBag(bag);
            }
        }
#endif

        private const string TemplatePath = PackageManagerWindow.ResourcesPath + "Templates/Alert.uxml";
        private readonly VisualElement root;
        private const float originalPositionRight = 5.0f;
        private const float positionRightWithScroll = 12.0f;

        public Alert() : this(string.Empty)
        {
        }

        public Alert(string text)
        {
            root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath).CloneTree(null);
            Add(root);
            root.StretchToParentSize();

            AlertMessage.text = text;
            CloseButton.clickable.clicked += ClearError;
        }

        public void SetError(Error error)
        {
            var message = "An error occured.";
            if (error != null)
                message = error.message ?? string.Format("An error occurred ({0})", error.errorCode.ToString());

            AlertMessage.text = message;
            RemoveFromClassList("display-none");
        }

        public void ClearError()
        {
            AddToClassList("display-none");
            AdjustSize(false);
            AlertMessage.text = "";
        }

        public void AdjustSize(bool verticalScrollerVisible)
        {
            if (verticalScrollerVisible)
                style.positionRight = originalPositionRight + positionRightWithScroll;
            else
                style.positionRight = originalPositionRight;
        }

        private Label AlertMessage { get { return root.Q<Label>("alertMessage"); } }
        private Button CloseButton { get { return root.Q<Button>("close"); } }
    }
}
