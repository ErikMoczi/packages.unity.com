using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class AlertFactory : UxmlFactory<Alert>
    {
        protected override Alert DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new Alert(bag.GetPropertyString("text"));
        }
    }

    internal class Alert : VisualElement
    {
        private const string TemplatePath = PackageManagerWindow.ResourcesPath + "Templates/Alert.uxml";
        private readonly VisualElement root;
        private const float originalPositionRight = 5.0f;
        private const float positionRightWithScroll = 12.0f;

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
            AlertMessage.text = error.message ?? string.Format("An error occurred ({0})", error.errorCode.ToString());
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
