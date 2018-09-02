using System;
using UnityEngine;

namespace EditorDiagnostics
{
    public class GraphDefinition
    {
        int m_maxValueStream;
        public IGraphLayer[] layers;
        public GraphDefinition(int maxValueStream, IGraphLayer[] l)
        {
            layers = l;
            m_maxValueStream = maxValueStream;
        }

        internal int GetMaxValue(EventDataCollection.PlayerSession.DataSet e)
        {
            var stream = e.GetStream(m_maxValueStream);
            return stream == null ? 1 : Mathf.Max(10, (stream.maxValue / 10 + 1) * 10);
        }
    }
}
