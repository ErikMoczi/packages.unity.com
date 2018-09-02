using UnityEngine;
namespace EditorDiagnostics
{
    public class GraphLayerBase : IGraphLayer
    {
        protected string m_name;
        protected string m_description;
        protected int m_stream;
        protected Color m_color;

        public GraphLayerBase(int stream, string name, string desc, Color color)
        {
            m_stream = stream;
            m_name = name;
            m_description = desc;
            m_color = color;
        }

        public Color color { get { return m_color; } }

        public string name { get { return m_name; } }

        public string description { get { return m_description; } }

        public virtual void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded, Material mat, int maxValue)
        {
        }
    }
}
