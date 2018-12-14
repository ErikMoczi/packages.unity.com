

namespace Unity.Tiny
{
    internal interface IIdentified<out T>
    {
        T Id { get; }
    }
}

