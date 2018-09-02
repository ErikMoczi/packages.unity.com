namespace UnityEditor.PackageManager.UI
{
    internal class UpmOperationFactory : IOperationFactory
    {
        public IListOperation CreateListOperation()
        {
            return new UpmListOperation();
        }

        public ISearchOperation CreateSearchOperation()
        {
            return new UpmSearchOperation();
        }

        public IAddOperation CreateAddOperation()
        {
            return new UpmAddOperation();
        }

        public IRemoveOperation CreateRemoveOperation()
        {
            return new UpmRemoveOperation();
        }
    }
}
