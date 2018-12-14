namespace Unity.Tiny
{
    internal interface ITinyCustomEditor
    {
        TinyContext TinyContext { get; }
        bool Visit(ref UIVisitContext<TinyObject> context);
    }
}