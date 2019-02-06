
namespace Unity.Tiny
{
    internal interface ITinyComponent
    {
        TinyId ComponentId { get; }
        bool IsValid { get; }
    }

    internal interface ITinyStruct
    {
        TinyId StructId { get; }
    }
}
