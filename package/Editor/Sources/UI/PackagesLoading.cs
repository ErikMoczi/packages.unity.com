using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class PackagesLoadingFactory : UxmlFactory<PackagesLoading>
    {
        protected override PackagesLoading DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackagesLoading();
        }
    }
#endif
    
    public class PackagesLoading : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<PackagesLoading> { }
#endif

        private readonly VisualElement root;

        public PackagesLoading()
        {
            root = Resources.GetTemplate("PackagesLoading.uxml");
            Add(root);

            PackageCollection.Instance.OnFilterChanged += OnFilterChanged;
            OnFilterChanged(PackageCollection.Instance.Filter);
        }

        private void OnFilterChanged(PackageFilter packageFilter)
        {
            CancelPreviousBindings();

            if (packageFilter == PackageFilter.Local)
                UpdateLoading(PackageCollection.Instance.listOperation);
            else
                UpdateLoading(PackageCollection.Instance.searchOperation);
        }

        private void UpdateLoading(IBaseOperation operation)
        {
            if (operation == null || operation.IsCompleted)
                SetLoading(false);
            else
            {
                operation.OnOperationFinalized += OnOperationFinalized;
                SetLoading(true);
            }
        }

        private void CancelPreviousBindings()
        {
            if (PackageCollection.Instance.listOperation != null)
                PackageCollection.Instance.listOperation.OnOperationFinalized -= OnOperationFinalized;
            if (PackageCollection.Instance.searchOperation != null)
                PackageCollection.Instance.searchOperation.OnOperationFinalized -= OnOperationFinalized;            
        }

        private void OnOperationFinalized()
        {
            SetLoading(false);
        }

        private void SetLoading(bool state)
        {
            LoadingContainer.visible = state;

            if (state)
                LoadingSpinner.Start();                
            else
                LoadingSpinner.Stop();
        }

        private VisualElement LoadingContainer { get { return root.Q<VisualElement>("loadingContainer");  }}
        private LoadingSpinner LoadingSpinner { get { return root.Q<LoadingSpinner>("packageSpinner");  }}
    }
}