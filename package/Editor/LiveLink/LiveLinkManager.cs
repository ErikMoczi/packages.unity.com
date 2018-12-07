using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using Unity.Tiny.Runtime.EditorExtensions;

namespace Unity.Tiny
{
    internal interface ILiveLinkManager : IContextManager
    {
        WorldState WorldState { get; }
    }

    internal class WorldState
    {
        public WorldState(IRegistry registry, DateTime time, string data)
        {
            Time = time;
            Data = data;
            using (registry.DontTrackChanges())
            {
                EntityGroup = registry.CreateEntityGroup(TinyId.New(), "World");
                FrameWidth = TinyEditorApplication.Project.Settings.CanvasWidth;
                FrameHeight = TinyEditorApplication.Project.Settings.CanvasHeight;
                IsValid = WorldStateReader.Deserialize(this);
            }
        }

        public bool IsValid { get; }
        public DateTime Time { get; }
        public string Data { get; }
        public TinyEntityGroup EntityGroup { get; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
    }

    internal class ClientHistory
    {
        public ClientHistory(WebSocketServer.Client client)
        {
            Client = client;
        }

        public WebSocketServer.Client Client { get; }
        public List<WorldState> WorldStates { get; } = new List<WorldState>();
        public WorldState GetClosest(DateTime time) => WorldStates.Count > 1 ?
            WorldStates.Aggregate((a, b) => (a.Time - time).Duration() < (b.Time - time).Duration() ? a : b) :
            WorldStates.FirstOrDefault();
    }

    [UsedImplicitly]
    [ContextManager(ContextUsage.LiveLink)]
    internal class LiveLinkManager : ContextManager, ILiveLinkManager
    {
        private IEntityGroupManagerInternal EntityGroupManager { get; set; }
        private Dictionary<WebSocketServer.Client, ClientHistory> History { get; } = new Dictionary<WebSocketServer.Client, ClientHistory>();
        private bool OriginalCanvasAutoResize { get; set; }
        private int OriginalCanvasWidth { get; set; }
        private int OriginalCanvasHeight { get; set; }
        public WorldState WorldState { get; private set; }

        [TinyInitializeOnLoad]
        private static void Initialize()
        {
            TinyEditorApplication.OnLoadProject += (project, context) =>
            {
                EditorApplication.pauseStateChanged += HandlePauseStateChanged;
                WebSocketServer.Instance.OnClientDisconnected += OnClientDisconnected;
                WebSocketServer.Instance.OnActiveClientChanged += OnActiveClientChanged;

                var liveLinkManager = context.GetManager<LiveLinkManager>();
                if  (liveLinkManager != null)
                {
                    liveLinkManager.OriginalCanvasAutoResize = project.Settings.CanvasAutoResize;
                    liveLinkManager.OriginalCanvasWidth = project.Settings.CanvasWidth;
                    liveLinkManager.OriginalCanvasHeight = project.Settings.CanvasHeight;
                }
            };
            TinyEditorApplication.OnCloseProject += (project, context) =>
            {
                WebSocketServer.Instance.OnActiveClientChanged -= OnActiveClientChanged;
                WebSocketServer.Instance.OnClientDisconnected -= OnClientDisconnected;
                EditorApplication.pauseStateChanged -= HandlePauseStateChanged;
            };
        }

        public LiveLinkManager(TinyContext context) : base(context)
        {
        }

        public override void Load()
        {
            base.Load();
            EntityGroupManager = Context.GetManager<IEntityGroupManagerInternal>();
        }

        private static void HandlePauseStateChanged(PauseState state)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var server = WebSocketServer.Instance;
            if (server == null || !server.Listening)
            {
                return;
            }

            var client = server.ActiveClient;
            if (client == null)
            {
                return;
            }

            if (state == PauseState.Paused)
            {
                var liveLinkManager = TinyEditorApplication.EditorContext.Context.GetManager<LiveLinkManager>();
                if (liveLinkManager == null)
                {
                    return;
                }

                server.SendPause(client, (message) =>
                {
                    if (message.Data[0] == 1)
                    {
                        liveLinkManager.SyncWorldState(client);
                    }
                });
            }
            else
            {
                server.SendResume(client);
            }
        }

        private static void OnClientDisconnected(WebSocketServer.Client client)
        {
            if (client == null)
            {
                return;
            }

            var liveLinkManager = TinyEditorApplication.EditorContext.Context.GetManager<LiveLinkManager>();
            if (liveLinkManager == null)
            {
                return;
            }

            liveLinkManager.History.Remove(client);
        }

        private static void OnActiveClientChanged(WebSocketServer.Client client)
        {
            var liveLinkManager = TinyEditorApplication.EditorContext.Context.GetManager<LiveLinkManager>();
            if (liveLinkManager == null)
            {
                return;
            }

            var history = liveLinkManager.GetOrAddClientHistory(client);
            var worldState = history?.GetClosest(DateTime.Now) ?? null;
            liveLinkManager.LoadWorldState(worldState);

            if (client == null)
            {
                return;
            }

            var server = WebSocketServer.Instance;
            if (server == null || !server.Listening)
            {
                return;
            }

            server.SendIsPaused(client, (message) =>
            {
                EditorApplication.isPaused = message.Data[0] == 1;
            });
        }

        private void SyncWorldState(WebSocketServer.Client client)
        {
            if (client == null)
            {
                return;
            }

            var server = WebSocketServer.Instance;
            if (server == null || !server.Listening)
            {
                return;
            }

            server.SendGetWorldState(client, (message) =>
            {
                var history = GetOrAddClientHistory(client);
                var worldState = new WorldState(Context.Registry, DateTime.Now, Encoding.ASCII.GetString(message.Data));
                if (worldState.IsValid)
                {
                    history.WorldStates.Add(worldState);
                    LoadWorldState(worldState);
                }
                else
                {
                    server.SendResume(client);
                    EditorApplication.isPaused = false;
                }
            });
        }

        private ClientHistory GetOrAddClientHistory(WebSocketServer.Client client)
        {
            if (client == null)
            {
                return null;
            }

            if (History.TryGetValue(client, out var history))
            {
                return history;
            }

            history = new ClientHistory(client);
            History.Add(client, history);
            return history;
        }

        private void LoadWorldState(WorldState worldState)
        {
            using (Context.Registry.DontTrackChanges())
            {
                var settings = TinyEditorApplication.Project.Settings;
                EntityGroupManager.UnloadAllEntityGroups();
                if (worldState != null)
                {
                    // Load world state entity group
                    EntityGroupManager.LoadEntityGroup(worldState.EntityGroup.Ref);

                    // Move asset entities in folders
                    var graph = EntityGroupManager.GetSceneGraph(worldState.EntityGroup.Ref);
                    var roots = graph.Roots.ToArray();
                    foreach (var entityNode in roots.OfType<EntityNode>())
                    {
                        var entity = entityNode.EntityRef.Dereference(Context.Registry);
                        if (entity.Components.FirstOrDefault(c => TinyAssetReference.IsAssetReference(c.Type)) != null)
                        {
                            FolderNode assetFolder = null;
                            if (string.IsNullOrEmpty(entity.Name) || !entity.Name.Contains('/'))
                            {
                                assetFolder = FolderNode.GetOrCreateFolderHierarchy(graph, "assets");
                            }
                            else
                            {
                                var separatorIndex = entity.Name.LastIndexOf('/');
                                var path = entity.Name.Substring(0, separatorIndex);
                                assetFolder = FolderNode.GetOrCreateFolderHierarchy(graph, path);
                                entity.Name = entity.Name.Substring(separatorIndex + 1);
                            }
                            entityNode.SetParent(assetFolder);
                        }
                    }

                    // Override project settings
                    settings.CanvasAutoResize = false;
                    settings.CanvasWidth = worldState.FrameWidth;
                    settings.CanvasHeight = worldState.FrameHeight;
                }
                else
                {
                    // Unpause editor
                    EditorApplication.isPaused = false;

                    // Restore original settings
                    settings.CanvasAutoResize = OriginalCanvasAutoResize;
                    settings.CanvasWidth = OriginalCanvasWidth;
                    settings.CanvasHeight = OriginalCanvasHeight;
                }
                WorldState = worldState;
            }
            TinyEditorBridge.RepaintGameViews();
        }
    }
}
