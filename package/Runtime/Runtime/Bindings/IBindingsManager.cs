namespace Unity.Tiny
{
    using EntityRef = TinyEntity.Reference;

    internal interface IBindingsManager : IContextManager
    {
        void SetConfigurationDirty(TinyEntity entity);
        void SetTemporaryDependency(EntityRef from, EntityRef to);
        void RemoveTemporaryDependency(EntityRef from, EntityRef to);
        void Transfer(TinyEntity entity);
        void SetAllDirty();
        void TransferAll();
    }
}