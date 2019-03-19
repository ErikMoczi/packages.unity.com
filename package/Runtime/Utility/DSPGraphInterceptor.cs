#if UNITY_EDITOR
// Define this to enable live visualization of the DSP graph and its connection attenuations.
// #define ENABLE_DSPGRAPH_INTERCEPTOR
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Experimental.Audio;
using UnityEngine;
using UnityEngine.Experimental.Audio;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
#endif

namespace Unity.Audio.Megacity
{
    struct DSPCommandBlockInterceptor
    {
        public enum Group
        {
            Unassigned,
            SampleProvider,
            Root,
            Global,
            Events,
            Ambience,
            Music,
            MainVehicle,
            VehicleField,
            Flyby
        }

#if ENABLE_DSPGRAPH_INTERCEPTOR
        class Impl
        {
            public float headerHeight;

            public float nodeWidth;
            public float parameterHeight;
            public float nodeSpacingX;
            public float nodeSpacingY;

            public float headerFontSize = 14.0f;
            public float parameterFontSize = 12.0f;

            class Parameter
            {
                public TextElement element = new TextElement();
                public string name;
                public float value = 0.0f;
                public float flash = 1.0f;
            }

            class Node
            {
                public TextElement element = new TextElement();
                public TextElement fillDisplay;
                public string name;
                public int id;
                public DSPNode dspNode;
                public AudioSampleProvider sampleProvider;
                public Vector2 position = default;
                public Vector2 force = default;
                public List<Connection> incoming = new List<Connection>();
                public List<Connection> outgoing = new List<Connection>();
                public Dictionary<int, Parameter> parameters = new Dictionary<int, Parameter>();
                public Group group;
            }

            class Terminal
            {
                public Node node;
                public int port;
            }

            // This is a non-interactive control, so early out on all costly checks for whether the user hovers or drags the edge.
            // Note that it is not recommended to use EdgeControl for this, and that this is just a temporary workaround until UIElements support custom rendering.
            class NonInteractiveEdgeControl : EdgeControl
            {
                public override bool ContainsPoint(Vector2 localPoint) { return false; }
                public override bool Overlaps(Rect rect) { return false; }
            }

            class Connection
            {
                public NonInteractiveEdgeControl element = new NonInteractiveEdgeControl();
                public int id;
                public Terminal source;
                public Terminal target;
                public DSPConnection dspConnection;
                public float attenuationL = 1.0f;
                public float attenuationR = 1.0f;
            }

            Dictionary<AudioSampleProvider, Node> sampleProviders = new Dictionary<AudioSampleProvider, Node>();
            Dictionary<DSPNode, Node> nodes = new Dictionary<DSPNode, Node>();
            Dictionary<DSPConnection, Connection> connections = new Dictionary<DSPConnection, Connection>();
            Dictionary<Group, Color> groupColor = new Dictionary<Group, Color>();
            int nodeIdCounter;
            int connectionIdCounter;
            Node rootNode;

            public Impl()
            {
                groupColor.Add(Group.Unassigned, new Color(0.1f, 0.2f, 0.3f));
                groupColor.Add(Group.SampleProvider, new Color(0.5f, 0.2f, 0.0f));
                groupColor.Add(Group.Root, new Color(0.5f, 0.0f, 0.0f));
                groupColor.Add(Group.Global, new Color(0.5f, 0.4f, 0.0f));
                groupColor.Add(Group.Events, new Color(0.5f, 0.1f, 0.0f));
                groupColor.Add(Group.Ambience, new Color(0.1f, 0.5f, 0.0f));
                groupColor.Add(Group.Music, new Color(0.0f, 0.1f, 0.5f));
                groupColor.Add(Group.MainVehicle, new Color(0.0f, 0.5f, 0.4f));
                groupColor.Add(Group.VehicleField, new Color(0.2f, 0.4f, 0.4f));
                groupColor.Add(Group.Flyby, new Color(0.2f, 0.4f, 0.4f));

                graphView = new Workspace();

                graphView.Add(graphView.connectionLayer);
                graphView.Add(graphView.nodeLayer);

                graphView.SetupZoom(0.25f, 8.0f);
            }

            public void SetNodeName(DSPNode dspNode, string name, Group group)
            {
                var node = GetOrCreateNode(dspNode);
                node.name = name;
                node.group = group;
            }

            float GetNodeHeight(Node node)
            {
                if (node.sampleProvider != null)
                    return headerHeight + parameterHeight;
                return node.parameters.Count * parameterHeight + headerHeight;
            }

            Node GetOrCreateNode(DSPNode dspNode)
            {
                if (nodes.ContainsKey(dspNode))
                    return nodes[dspNode];

                var node = new Node
                {
                    id = ++nodeIdCounter,
                    dspNode = dspNode,
                    name = "New"
                };

                graphView.nodeLayer.Add(node.element);
                nodes.Add(dspNode, node);
                return node;
            }

            Node CreateSampleProviderNode(AudioSampleProvider provider, string name)
            {
                if (sampleProviders.ContainsKey(provider))
                    return sampleProviders[provider];

                var node = new Node
                {
                    id = ++nodeIdCounter,
                    sampleProvider = provider,
                    name = name,
                    group = Group.SampleProvider
                };

                sampleProviders.Add(provider, node);
                graphView.nodeLayer.Add(node.element);
                node.fillDisplay = new TextElement();
                node.fillDisplay.style.position = Position.Absolute;
                node.fillDisplay.style.color = Color.black;
                node.fillDisplay.style.backgroundColor = Color.red;
                node.element.Add(node.fillDisplay);
                return node;
            }

            Connection CreateConnection(Node sourceNode, int sourcePort, Node targetNode, int targetPort)
            {
                var con = new Connection
                {
                    id = ++connectionIdCounter,
                    source = new Terminal { node = sourceNode, port = sourcePort },
                    target = new Terminal { node = targetNode, port = targetPort },
                };

                graphView.connectionLayer.Add(con.element);
                return con;
            }

            Connection GetOrCreateConnection(DSPConnection dspConnection, Node sourceNode, int sourcePort, Node targetNode, int targetPort)
            {
                if (connections.ContainsKey(dspConnection))
                    return connections[dspConnection];

                var con = CreateConnection(sourceNode, sourcePort, targetNode, targetPort);
                con.dspConnection = dspConnection;
                connections.Add(dspConnection, con);
                return con;
            }

            public void Connect(DSPNode sourceNode, int sourcePort, DSPNode targetNode, int targetPort, DSPConnection dspConnection)
            {
                GetOrCreateNode(sourceNode);
                GetOrCreateNode(targetNode);

                foreach (var c in nodes[sourceNode].outgoing)
                    if (c.target.node.dspNode.Equals(targetNode) && c.source.port == sourcePort && c.target.port == targetPort)
                        return;

                foreach (var c in nodes[targetNode].incoming)
                    if (c.source.node.dspNode.Equals(sourceNode) && c.target.port == targetPort && c.source.port == sourcePort)
                        return;

                var con = GetOrCreateConnection(dspConnection, nodes[sourceNode], sourcePort, nodes[targetNode], targetPort);

                nodes[sourceNode].outgoing.Add(con);
                nodes[targetNode].incoming.Add(con);
            }

            public void ReleaseDSPNode(DSPNode dspNode)
            {
                if (!nodes.ContainsKey(dspNode))
                    return;

                var node = nodes[dspNode];

                foreach (var c in node.incoming)
                {
                    if(connections.ContainsKey(c.dspConnection))
                    {
                        c.source.node.outgoing.Remove(c);
                        graphView.connectionLayer.Remove(c.element);
                        connections.Remove(c.dspConnection);
                    }
                }

                foreach (var c in node.outgoing)
                {
                    if (connections.ContainsKey(c.dspConnection))
                    {
                        c.target.node.incoming.Remove(c);
                        graphView.connectionLayer.Remove(c.element);
                        connections.Remove(c.dspConnection);
                    }
                }

                RemoveSampleProviderFromNode(node);

                graphView.nodeLayer.Remove(node.element);

                nodes.Remove(dspNode);
            }

            public void SetAttenuation(DSPConnection dspConnection, float l, float r)
            {
                if (connections.ContainsKey(dspConnection))
                {
                    connections[dspConnection].attenuationL = l;
                    connections[dspConnection].attenuationR = r;
                }
            }

            public void SetFloat<TParams>(DSPNode dspNode, TParams parameter, float value)
                where TParams : struct, IConvertible
            {
                var node = GetOrCreateNode(dspNode);

                int index = UnsafeUtility.EnumToInt<TParams>(parameter);

                if (!node.parameters.ContainsKey(index))
                {
                    node.parameters[index] = new Parameter();
                    node.parameters[index].name = parameter.ToString();
                    node.element.Add(node.parameters[index].element);
                }

                node.parameters[index].value = value;
                node.parameters[index].flash = 1.0f;
            }

            void RemoveSampleProviderFromNode(Node node)
            {
                AudioSampleProvider provider = node.sampleProvider;
                if (provider == null)
                    return;

                if (sampleProviders.ContainsKey(provider))
                {
                    var sampleProviderNode = sampleProviders[provider];
                    sampleProviderNode.element.Remove(sampleProviderNode.fillDisplay);
                    node.sampleProvider = null;
                    graphView.nodeLayer.Remove(sampleProviders[provider].element);
                    graphView.connectionLayer.Remove(sampleProviders[provider].outgoing[0].element);
                    sampleProviders.Remove(provider);
                }
            }

            public void SetSampleProvider(AudioSampleProvider provider, DSPNode dspNode, int index)
            {
                if (provider == null)
                {
                    if (nodes.ContainsKey(dspNode))
                        RemoveSampleProviderFromNode(nodes[dspNode]);

                    return;
                }

                var targetNode = GetOrCreateNode(dspNode);

                targetNode.sampleProvider = provider;
                targetNode.name = "SP " + provider.id + " (" + provider.channelCount + " ch, " + (provider.sampleRate * 0.001f) + " kHz)";
                targetNode.group = Group.SampleProvider;

                var sourceNode = CreateSampleProviderNode(provider, targetNode.name);

                var con = CreateConnection(sourceNode, -1, targetNode, -1);

                sourceNode.outgoing.Add(con);
                targetNode.incoming.Add(con);
            }

            void LayoutX(Node node, int x)
            {
                node.position.x = Mathf.Min(node.position.x, x);
                foreach (var con in node.incoming)
                    LayoutX(con.source.node, (int)(x - nodeWidth - nodeSpacingX));
            }

            float LayoutY(Node node, float y)
            {
                float y0 = y;

                node.position.y = y;

                int count = 0;
                foreach (var con in node.incoming)
                {
                    y = LayoutY(con.source.node, y) + nodeSpacingY;
                    ++count;
                }

                if (count > 0)
                {
                    y += nodeSpacingY;

                    float minY = 1000000.0f;
                    float maxY = -1000000.0f;

                    foreach (var con in node.incoming)
                    {
                        minY = Mathf.Min(minY, con.source.node.position.y);
                        maxY = Mathf.Max(maxY, con.source.node.position.y);
                    }

                    node.position.y = (minY + maxY) * 0.5f;
                    y = Mathf.Max(y, maxY);
                }

                return Mathf.Max(y, y0 + GetNodeHeight(node));
            }

            float Layout(Node node, float baseY)
            {
                foreach (var n in sampleProviders)
                    n.Value.position.x = 1000000;

                foreach (var n in nodes)
                    n.Value.position.x = 1000000;

                for (int pass = 0; pass < 2; pass++)
                {
                    foreach (var n in nodes)
                        if (n.Value.outgoing.Count == 0)
                            LayoutX(n.Value, 0);
                }

                float y = 0;

                foreach (var n in nodes)
                    if (n.Value.outgoing.Count == 0)
                        y = LayoutY(n.Value, y);

                var minPos = new Vector2(100000, 100000);
                var maxPos = new Vector2(-100000, -100000);

                float maxNodeHeight = 0.0f;

                foreach (var n in nodes)
                {
                    minPos = Vector2.Min(minPos, n.Value.position);
                    maxPos = Vector2.Max(maxPos, n.Value.position);
                    maxNodeHeight = Mathf.Max(maxNodeHeight, GetNodeHeight(n.Value));
                }

                foreach (var n in sampleProviders)
                {
                    minPos = Vector2.Min(minPos, n.Value.position);
                    maxPos = Vector2.Max(maxPos, n.Value.position);
                    maxNodeHeight = Mathf.Max(maxNodeHeight, GetNodeHeight(n.Value));
                }

                var offset = new Vector2(0.5f * nodeWidth + 10.0f, 0.5f * maxNodeHeight + 30.0f + baseY) - minPos;

                foreach (var n in nodes)
                    n.Value.position += offset;

                foreach (var n in sampleProviders)
                    n.Value.position += offset;

                return baseY + maxPos.y - minPos.y;
            }

#if UNITY_EDITOR

            class Workspace : GraphView
            {
                public VisualElement nodeLayer = new VisualElement();
                public VisualElement connectionLayer = new VisualElement();
                public ContentDragger m_Dragger;

                public Workspace()
                {
                    m_Dragger = new ContentDragger();
                    this.AddManipulator(m_Dragger);
                }
            }

            VisualElement rootElement;
            Workspace graphView;

            public void OnEnable(EditorWindow window)
            {
                // Each editor window contains a root VisualContainer object
                rootElement = window.rootVisualElement;

                rootElement.Add(graphView);
            }

            public void OnDisable(EditorWindow window)
            {
            }

            Vector2 m_ScrollPosition = default;

            int updateCounter;

            float gainScale = 10.0f;

            public void Update(EditorWindow window)
            {
                float windowWidth = window.position.width;
                float windowHeight = window.position.height;

                ++updateCounter;

                headerHeight = 20.0f;
                nodeWidth = 160.0f;
                parameterHeight = 15.0f;
                nodeSpacingX = 100.0f;
                nodeSpacingY = 5.0f;

                float y = 0;
                foreach (var node in nodes)
                    if (node.Value.outgoing.Count == 0)
                        y = Layout(node.Value, y) + 10;

                ApplyLayout();
            }

            void ApplyLayout()
            {
                float scale = graphView.scale;
                int edgeWidth = (int)(2.0f * scale) + 1;
                var offset = new Vector2 (graphView.contentViewContainer.transform.position.x, graphView.contentViewContainer.transform.position.y);
                var d = new Vector2 (nodeWidth * 0.5f * scale - 10, 0.0f);

                foreach (var c in connections)
                {
                    var con = c.Value;
                    var e = con.element;
                    e.from = con.source.node.position * scale + d + offset;
                    e.to = con.target.node.position * scale - d + offset;
                    e.edgeWidth = edgeWidth;
                    e.inputColor = new Color(con.attenuationL * gainScale, con.attenuationR * gainScale, 0.2f);
                    e.outputColor = e.inputColor;
                    e.UpdateLayout();
                }

                foreach (var s in sampleProviders)
                {
                    foreach (var con in s.Value.outgoing)
                    {
                        var e = con.element;
                        e.from = con.source.node.position * scale + d + offset;
                        e.to = con.target.node.position * scale - d + offset;
                        e.edgeWidth = edgeWidth;
                        e.inputColor = new Color(con.attenuationL * gainScale, con.attenuationR * gainScale, 0.2f);
                        e.outputColor = e.inputColor;
                        e.UpdateLayout();
                    }
                }

                foreach (var s in sampleProviders)
                {
                    var node = s.Value;

                    var content = new GUIContent(node.name);

                    var center = node.position;

                    float h0 = GetNodeHeight(node);
                    float h1 = headerHeight;
                    var p = new Vector2(center.x - nodeWidth * 0.5f, center.y - h0 * 0.5f) * scale + offset;

                    node.element.text = node.name;
                    node.element.style.position = Position.Absolute;
                    node.element.style.left = p.x;
                    node.element.style.top = p.y;
                    node.element.style.width = nodeWidth * scale;
                    node.element.style.height = h1 * scale;
                    node.element.style.backgroundColor = groupColor[node.group];
                    node.element.style.fontSize = (int)(scale * headerFontSize);

                    if (s.Key.valid)
                    {
                        uint availableSamples = s.Key.availableSampleFrameCount;
                        uint maxSamples = s.Key.maxSampleFrameCount;
                        float fillAmount = (float)availableSamples / (float)maxSamples;
                        if (fillAmount >= 0.0f)
                        {
                            node.fillDisplay.text = String.Format("{0}/{1} samples", availableSamples, maxSamples);
                            node.fillDisplay.style.position = Position.Absolute;
                            node.fillDisplay.style.left = 0.0f;
                            node.fillDisplay.style.top = h1 * scale;
                            node.fillDisplay.style.width = nodeWidth * scale * fillAmount;
                            node.fillDisplay.style.height = (h0 - h1) * scale;
                        }
                    }
                }

                foreach (var n in nodes)
                {
                    var node = n.Value;

                    var center = node.position;

                    int numParams = node.parameters.Count;

                    float h = GetNodeHeight(node);
                    var p = new Vector2(center.x - nodeWidth * 0.5f, center.y - h * 0.5f) * scale + offset;

                    var color = groupColor[node.group];

                    node.element.text = node.name;
                    node.element.style.position = Position.Absolute;
                    node.element.style.left = p.x;
                    node.element.style.top = p.y;
                    node.element.style.width = nodeWidth * scale;
                    node.element.style.height = h * scale;
                    node.element.style.backgroundColor = color;
                    node.element.style.fontSize = (int)(scale * headerFontSize);

                    if (numParams > 0)
                    {
                        p.x = 0.0f;
                        p.y = headerHeight * scale;

                        foreach (var i in node.parameters)
                        {
                            var parameter = i.Value;
                            parameter.element.style.position = Position.Absolute;
                            parameter.element.style.left = p.x;
                            parameter.element.style.top = p.y;
                            parameter.element.style.width = nodeWidth * scale;
                            parameter.element.style.height = parameterHeight * scale;
                            parameter.element.style.backgroundColor = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 1.0f);
                            parameter.element.style.fontSize = (int)(scale * parameterFontSize);
                            parameter.element.style.color = new Color(1.0f, 1.0f, 1.0f, 0.5f * parameter.flash + 0.5f);
                            parameter.element.text = parameter.name + ": " + parameter.value;
                            parameter.flash *= 0.99f;
                            p.y += parameterHeight * scale;
                        }
                    }
                }

                if (rootElement != null)
                {
                    graphView.style.position = Position.Absolute;
                    graphView.style.left = 0;
                    graphView.style.top = 0;
                    graphView.style.right = 0;
                    graphView.style.bottom = 0;
                }
            }
#endif
        }

        static Impl impl = new Impl();
#endif

        DSPCommandBlock m_Block;

        public static DSPCommandBlockInterceptor CreateCommandBlock(DSPGraph graph)
        {
            return new DSPCommandBlockInterceptor
            {
                m_Block = graph.CreateCommandBlock()
            };
        }

        public DSPNode CreateDSPNode<TParams, TProvs, TAudioJob>()
            where TParams : struct, IConvertible
            where TProvs : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            var node = m_Block.CreateDSPNode<TParams, TProvs, TAudioJob>();
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.SetNodeName(node, "Unknown", Group.Unassigned);
#endif
            return node;
        }

        public void SetFloat<TParams, TProvs, TAudioJob>(DSPNode node, TParams parameter, float value, uint interpolationLength = 0)
            where TParams : struct, IConvertible
            where TProvs : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            m_Block.SetFloat<TParams, TProvs, TAudioJob>(node, parameter, value, interpolationLength);
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.SetFloat(node, parameter, value);
#endif
        }

        public void SetSampleProvider<TParams, TProvs, TAudioJob>(AudioSampleProvider provider, DSPNode node, TProvs item, int index = 0)
            where TParams : struct, IConvertible
            where TProvs : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            m_Block.SetSampleProvider<TParams, TProvs, TAudioJob>(provider, node, item, index);
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.SetSampleProvider(provider, node, index);
#endif
        }

        public DSPNodeUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob> CreateUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob>(TAudioJobUpdate updateJob, DSPNode node, Action<DSPNodeUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob>> callback)
            where TAudioJobUpdate : struct, IAudioJobUpdate<TParams, TProvs, TAudioJob>
            where TParams : struct, IConvertible
            where TProvs : struct, IConvertible
            where TAudioJob : struct, IAudioJob<TParams, TProvs>
        {
            return m_Block.CreateUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob>(updateJob, node, callback);
        }

        public void AddInletPort(DSPNode node, int channelCount, SoundFormat format)
        {
            m_Block.AddInletPort(node, channelCount, format);
        }

        public void AddOutletPort(DSPNode node, int channelCount, SoundFormat format)
        {
            m_Block.AddOutletPort(node, channelCount, format);
        }

        public void Complete()
        {
            m_Block.Complete();
        }

        public static void SetNodeName(DSPNode node, string name, Group group)
        {
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.SetNodeName(node, name, group);
#endif
        }

        public DSPConnection Connect(DSPNode sourceNode, int sourcePort, DSPNode targetNode, int targetPort)
        {
            var dspConnection = m_Block.Connect(sourceNode, sourcePort, targetNode, targetPort);

#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.Connect(sourceNode, sourcePort, targetNode, targetPort, dspConnection);
#endif

            return dspConnection;
        }

        public void ReleaseDSPNode(DSPNode node)
        {
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.ReleaseDSPNode(node);
#endif

            m_Block.ReleaseDSPNode(node);
        }

        public void SetAttenuation(DSPConnection dspConnection, float l, float r, uint lerpSamples)
        {
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.SetAttenuation(dspConnection, l, r);
#endif

            m_Block.SetAttenuation(dspConnection, l, r, lerpSamples);
        }

        public void SetAttenuation(DSPConnection dspConnection, float v, uint lerpSamples)
        {
            SetAttenuation(dspConnection, v, v, lerpSamples);
        }

        public void SetAttenuation(DSPConnection dspConnection, float v)
        {
            SetAttenuation(dspConnection, v, v, 0);
        }

#if UNITY_EDITOR
        public static void OnEnable(EditorWindow window)
        {
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.OnEnable(window);
#endif
        }

        public static void OnDisable(EditorWindow window)
        {
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.OnDisable(window);
#endif
        }

        public static void Update(EditorWindow window)
        {
#if ENABLE_DSPGRAPH_INTERCEPTOR
            impl.Update(window);
#endif
        }
#endif
    }
}

#if UNITY_EDITOR
class DSPGraphWindow : EditorWindow
{
    [MenuItem("Window/DSP Graph")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(DSPGraphWindow));
    }

#if ENABLE_DSPGRAPH_INTERCEPTOR
    internal class TickTimerHelper
    {
        double m_NextTick;
        double m_Interval;

        public TickTimerHelper(double intervalBetweenTicksInSeconds)
        {
            m_Interval = intervalBetweenTicksInSeconds;
        }

        public bool DoTick()
        {
            if (EditorApplication.timeSinceStartup > m_NextTick)
            {
                m_NextTick = EditorApplication.timeSinceStartup + m_Interval;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            m_NextTick = 0;
        }
    }

    readonly TickTimerHelper m_Ticker = new TickTimerHelper(0.05f);
#endif

    void OnEnable()
    {
#if ENABLE_DSPGRAPH_INTERCEPTOR
        Unity.Audio.DSPCommandBlockInterceptor.OnEnable(this);
#endif
    }

    void OnDisable()
    {
#if ENABLE_DSPGRAPH_INTERCEPTOR
        Unity.Audio.DSPCommandBlockInterceptor.OnDisable(this);
#endif
    }

    void Update()
    {
#if ENABLE_DSPGRAPH_INTERCEPTOR
        if (!m_Ticker.DoTick() || !EditorApplication.isPlaying)
            return;

        Unity.Audio.DSPCommandBlockInterceptor.Update(this);
#endif
    }

    void OnGUI()
    {
#if !ENABLE_DSPGRAPH_INTERCEPTOR
        GUILayout.Label("Define ENABLE_DSPGRAPH_INTERCEPTOR in DSPGraphInterceptor.cs to enable DSP graph visualization.");
#endif
    }
}
#endif
