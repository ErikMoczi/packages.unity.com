namespace EditorDiagnostics
{
    public class GraphDefinition
    {
        public IGraphLayer[] layers;
        public GraphDefinition(IGraphLayer[] l)
        {
            layers = l;
        }
    }
}