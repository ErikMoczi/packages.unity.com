

namespace Unity.Tiny
{
    internal enum ComponentCallbackType
    {
        OnAddComponent = 0,
        OnRemoveComponent = 1,
        OnValidate = 2
    }

    internal interface IComponentCallback
    {
        TinyType.Reference TypeRef { get; }

        void Run(ComponentCallbackType callbackType, TinyEntity entity, TinyObject component);
    }
}

