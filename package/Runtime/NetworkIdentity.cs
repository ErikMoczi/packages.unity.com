#if ENABLE_UNET
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Networking.NetworkSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Networking
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/NetworkIdentity")]
    public sealed class NetworkIdentity : MonoBehaviour
    {
        // configuration
        [SerializeField] NetworkSceneId m_SceneId;
        [SerializeField] NetworkHash128 m_AssetId;
        [SerializeField] bool           m_ServerOnly;
        [SerializeField] bool           m_LocalPlayerAuthority;

        // runtime data
        bool                        m_IsClient;
        bool                        m_IsServer;
        bool                        m_HasAuthority;

        NetworkInstanceId           m_NetId;
        bool                        m_IsLocalPlayer;
        NetworkConnection           m_ConnectionToServer;
        NetworkConnection           m_ConnectionToClient;
        short                       m_PlayerId = -1;
        NetworkBehaviour[]          m_NetworkBehaviours;

        // there is a list AND a hashSet of connections, for fast verification of dupes, but the main operation is iteration over the list.
        HashSet<int>                m_ObserverConnections;
        List<NetworkConnection>     m_Observers;
        NetworkConnection           m_ClientAuthorityOwner;

        // member used to mark a identity for future reset
        // check MarkForReset for more information.
        bool                        m_Reset = false;
        // properties
        public bool isClient        { get { return m_IsClient; } }

        public bool isServer
        {
            get
            {
                // if server has stopped, should not still return true here
                return m_IsServer && NetworkServer.active;
            }
        }

        public bool hasAuthority    { get { return m_HasAuthority; } }

        public NetworkInstanceId netId { get { return m_NetId; } }
        public NetworkSceneId sceneId { get { return m_SceneId; } }
        public bool serverOnly { get { return m_ServerOnly; } set { m_ServerOnly = value; } }
        public bool localPlayerAuthority { get { return m_LocalPlayerAuthority; } set { m_LocalPlayerAuthority = value; } }
        public NetworkConnection clientAuthorityOwner { get { return m_ClientAuthorityOwner; }}

        public NetworkHash128 assetId
        {
            get
            {
#if UNITY_EDITOR
                // This is important because sometimes OnValidate does not run (like when adding view to prefab with no child links)
                if (!m_AssetId.IsValid())
                    SetupIDs();
#endif
                return m_AssetId;
            }
        }
        internal void SetDynamicAssetId(NetworkHash128 newAssetId)
        {
            if (!m_AssetId.IsValid() || m_AssetId.Equals(newAssetId))
            {
                m_AssetId = newAssetId;
            }
            else
            {
                if (LogFilter.logWarn) { Debug.LogWarning("SetDynamicAssetId object already has an assetId <" + m_AssetId + ">"); }
            }
        }

        // used when adding players
        internal void SetClientOwner(NetworkConnection conn)
        {
            if (m_ClientAuthorityOwner != null)
            {
                if (LogFilter.logError) { Debug.LogError("SetClientOwner m_ClientAuthorityOwner already set!"); }
            }
            m_ClientAuthorityOwner = conn;
            m_ClientAuthorityOwner.AddOwnedObject(this);
        }

        // used during dispose after disconnect
        internal void ClearClientOwner()
        {
            m_ClientAuthorityOwner = null;
        }

        internal void ForceAuthority(bool authority)
        {
            if (m_HasAuthority == authority)
            {
                return;
            }

            m_HasAuthority = authority;
            if (authority)
            {
                OnStartAuthority();
            }
            else
            {
                OnStopAuthority();
            }
        }

        public bool isLocalPlayer { get { return m_IsLocalPlayer; } }
        public short playerControllerId { get { return m_PlayerId; } }
        public NetworkConnection connectionToServer { get { return m_ConnectionToServer; } }
        public NetworkConnection connectionToClient { get { return m_ConnectionToClient; } }

        public ReadOnlyCollection<NetworkConnection> observers
        {
            get
            {
                if (m_Observers == null)
                    return null;

                return new ReadOnlyCollection<NetworkConnection>(m_Observers);
            }
        }

        static uint s_NextNetworkId = 1;
        static internal NetworkInstanceId GetNextNetworkId()
        {
            uint newId = s_NextNetworkId;
            s_NextNetworkId += 1;
            return new NetworkInstanceId(newId);
        }

        static NetworkWriter s_UpdateWriter = new NetworkWriter();

        void CacheBehaviours()
        {
            if (m_NetworkBehaviours == null)
            {
                m_NetworkBehaviours = GetComponents<NetworkBehaviour>();
            }
        }

        public delegate void ClientAuthorityCallback(NetworkConnection conn, NetworkIdentity uv, bool authorityState);
        public static ClientAuthorityCallback clientAuthorityCallback;

        static internal void AddNetworkId(uint id)
        {
            if (id >= s_NextNetworkId)
            {
                s_NextNetworkId = (uint)(id + 1);
            }
        }

        // only used during spawning on clients to set the identity.
        internal void SetNetworkInstanceId(NetworkInstanceId newNetId)
        {
            m_NetId = newNetId;
            if (newNetId.Value == 0)
            {
                m_IsServer = false;
            }
        }

        // only used when fixing duplicate scene IDs duing post-processing
        public void ForceSceneId(int newSceneId)
        {
            m_SceneId = new NetworkSceneId((uint)newSceneId);
        }

        // only used in SetLocalObject
        internal void UpdateClientServer(bool isClientFlag, bool isServerFlag)
        {
            m_IsClient |= isClientFlag;
            m_IsServer |= isServerFlag;
        }

        // used when the player object for a connection changes
        internal void SetNotLocalPlayer()
        {
            m_IsLocalPlayer = false;

            if (NetworkServer.active && NetworkServer.localClientActive)
            {
                // dont change authority for objects on the host
                return;
            }
            m_HasAuthority = false;
        }

        // this is used when a connection is destroyed, since the "observers" property is read-only
        internal void RemoveObserverInternal(NetworkConnection conn)
        {
            if (m_Observers != null)
            {
                m_Observers.Remove(conn);
                m_ObserverConnections.Remove(conn.connectionId);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (m_ServerOnly && m_LocalPlayerAuthority)
            {
                if (LogFilter.logWarn) { Debug.LogWarning("Disabling Local Player Authority for " + gameObject + " because it is server-only."); }
                m_LocalPlayerAuthority = false;
            }

            SetupIDs();
        }

        void AssignAssetID(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            m_AssetId = NetworkHash128.Parse(AssetDatabase.AssetPathToGUID(path));
        }

        bool ThisIsAPrefab()
        {
            return PrefabUtility.IsPartOfPrefabAsset(gameObject);
        }

        bool ThisIsASceneObjectWithThatReferencesPrefabAsset(out GameObject prefab)
        {
            prefab = null;
            if (!PrefabUtility.IsPartOfNonAssetPrefabInstance(gameObject))
                return false;
            prefab = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab == null)
            {
                if (LogFilter.logError) { Debug.LogError("Failed to find prefab parent for scene object [name:" + gameObject.name + "]"); }
                return false;
            }
            return true;
        }

        void SetupIDs()
        {
            GameObject prefab;
            if (ThisIsAPrefab())
            {
                ForceSceneId(0);
                AssignAssetID(gameObject);
            }
            else if (ThisIsASceneObjectWithThatReferencesPrefabAsset(out prefab))
            {
                AssignAssetID(prefab);
            }
            else
            {
                m_AssetId.Reset();
            }
        }

#endif
        void OnDestroy()
        {
            if (m_IsServer && NetworkServer.active)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        internal void OnStartServer(bool allowNonZeroNetId)
        {
            if (m_IsServer)
            {
                return;
            }
            m_IsServer = true;

            if (m_LocalPlayerAuthority)
            {
                // local player on server has NO authority
                m_HasAuthority = false;
            }
            else
            {
                // enemy on server has authority
                m_HasAuthority = true;
            }

            m_Observers = new List<NetworkConnection>();
            m_ObserverConnections = new HashSet<int>();
            CacheBehaviours();

            // If the instance/net ID is invalid here then this is an object instantiated from a prefab and the server should assign a valid ID
            if (netId.IsEmpty())
            {
                m_NetId = GetNextNetworkId();
            }
            else
            {
                if (allowNonZeroNetId)
                {
                    //allowed
                }
                else
                {
                    if (LogFilter.logError) { Debug.LogError("Object has non-zero netId " + netId + " for " + gameObject); }
                    return;
                }
            }

            if (LogFilter.logDev) { Debug.Log("OnStartServer " + gameObject + " GUID:" + netId); }
            NetworkServer.instance.SetLocalObjectOnServer(netId, gameObject);

            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                try
                {
                    comp.OnStartServer();
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception in OnStartServer:" + e.Message + " " + e.StackTrace);
                }
            }

            if (NetworkClient.active && NetworkServer.localClientActive)
            {
                // there will be no spawn message, so start the client here too
                ClientScene.SetLocalObject(netId, gameObject);
                OnStartClient();
            }

            if (m_HasAuthority)
            {
                OnStartAuthority();
            }
        }

        internal void OnStartClient()
        {
            if (!m_IsClient)
            {
                m_IsClient = true;
            }
            CacheBehaviours();

            if (LogFilter.logDev) { Debug.Log("OnStartClient " + gameObject + " GUID:" + netId + " localPlayerAuthority:" + localPlayerAuthority); }
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                try
                {
                    comp.PreStartClient(); // generated startup to resolve object references
                    comp.OnStartClient(); // user implemented startup
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception in OnStartClient:" + e.Message + " " + e.StackTrace);
                }
            }
        }

        internal void OnStartAuthority()
        {
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                try
                {
                    comp.OnStartAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception in OnStartAuthority:" + e.Message + " " + e.StackTrace);
                }
            }
        }

        internal void OnStopAuthority()
        {
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                try
                {
                    comp.OnStopAuthority();
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception in OnStopAuthority:" + e.Message + " " + e.StackTrace);
                }
            }
        }

        internal void OnSetLocalVisibility(bool vis)
        {
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                try
                {
                    comp.OnSetLocalVisibility(vis);
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception in OnSetLocalVisibility:" + e.Message + " " + e.StackTrace);
                }
            }
        }

        internal bool OnCheckObserver(NetworkConnection conn)
        {
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                try
                {
                    if (!comp.OnCheckObserver(conn))
                        return false;
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception in OnCheckObserver:" + e.Message + " " + e.StackTrace);
                }
            }
            return true;
        }

        // happens on server
        internal void UNetSerializeAllVars(NetworkWriter writer)
        {
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                comp.OnSerialize(writer, true);
            }
        }

        // happens on client
        internal void HandleClientAuthority(bool authority)
        {
            if (!localPlayerAuthority)
            {
                if (LogFilter.logError) { Debug.LogError("HandleClientAuthority " + gameObject + " does not have localPlayerAuthority"); }
                return;
            }

            ForceAuthority(authority);
        }

        // helper function for Handle** functions
        bool GetInvokeComponent(int cmdHash, Type invokeClass, out NetworkBehaviour invokeComponent)
        {
            // dont use GetComponent(), already have a list - avoids an allocation
            NetworkBehaviour foundComp = null;
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                var comp = m_NetworkBehaviours[i];
                if (comp.GetType() == invokeClass || comp.GetType().IsSubclassOf(invokeClass))
                {
                    // found matching class
                    foundComp = comp;
                    break;
                }
            }
            if (foundComp == null)
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logError) { Debug.LogError("Found no behaviour for incoming [" + errorCmdName + "] on " + gameObject + ",  the server and client should have the same NetworkBehaviour instances [netId=" + netId + "]."); }
                invokeComponent = null;
                return false;
            }
            invokeComponent = foundComp;
            return true;
        }

        // happens on client
        internal void HandleSyncEvent(int cmdHash, NetworkReader reader)
        {
            // this doesn't use NetworkBehaviour.InvokeSyncEvent function (anymore). this method of calling is faster.
            // The hash is only looked up once, insted of twice(!) per NetworkBehaviour on the object.

            if (gameObject == null)
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logWarn) { Debug.LogWarning("SyncEvent [" + errorCmdName + "] received for deleted object [netId=" + netId + "]"); }
                return;
            }

            // find the matching SyncEvent function and networkBehaviour class
            NetworkBehaviour.CmdDelegate invokeFunction;
            Type invokeClass;
            bool invokeFound = NetworkBehaviour.GetInvokerForHashSyncEvent(cmdHash, out invokeClass, out invokeFunction);
            if (!invokeFound)
            {
                // We don't get a valid lookup of the command name when it doesn't exist...
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logError) { Debug.LogError("Found no receiver for incoming [" + errorCmdName + "] on " + gameObject + ",  the server and client should have the same NetworkBehaviour instances [netId=" + netId + "]."); }
                return;
            }

            // find the right component to invoke the function on
            NetworkBehaviour invokeComponent;
            if (!GetInvokeComponent(cmdHash, invokeClass, out invokeComponent))
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logWarn) { Debug.LogWarning("SyncEvent [" + errorCmdName + "] handler not found [netId=" + netId + "]"); }
                return;
            }

            invokeFunction(invokeComponent, reader);

#if UNITY_EDITOR
            Profiler.IncrementStatIncoming(MsgType.SyncEvent, NetworkBehaviour.GetCmdHashEventName(cmdHash));
#endif
        }

        // happens on client
        internal void HandleSyncList(int cmdHash, NetworkReader reader)
        {
            // this doesn't use NetworkBehaviour.InvokSyncList function (anymore). this method of calling is faster.
            // The hash is only looked up once, insted of twice(!) per NetworkBehaviour on the object.

            if (gameObject == null)
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logWarn) { Debug.LogWarning("SyncList [" + errorCmdName + "] received for deleted object [netId=" + netId + "]"); }
                return;
            }

            // find the matching SyncList function and networkBehaviour class
            NetworkBehaviour.CmdDelegate invokeFunction;
            Type invokeClass;
            bool invokeFound = NetworkBehaviour.GetInvokerForHashSyncList(cmdHash, out invokeClass, out invokeFunction);
            if (!invokeFound)
            {
                // We don't get a valid lookup of the command name when it doesn't exist...
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logError) { Debug.LogError("Found no receiver for incoming [" + errorCmdName + "] on " + gameObject + ",  the server and client should have the same NetworkBehaviour instances [netId=" + netId + "]."); }
                return;
            }

            // find the right component to invoke the function on
            NetworkBehaviour invokeComponent;
            if (!GetInvokeComponent(cmdHash, invokeClass, out invokeComponent))
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logWarn) { Debug.LogWarning("SyncList [" + errorCmdName + "] handler not found [netId=" + netId + "]"); }
                return;
            }

            invokeFunction(invokeComponent, reader);

#if UNITY_EDITOR
            Profiler.IncrementStatIncoming(MsgType.SyncList, NetworkBehaviour.GetCmdHashListName(cmdHash));
#endif
        }

        // happens on server
        internal void HandleCommand(int cmdHash, NetworkReader reader)
        {
            // this doesn't use NetworkBehaviour.InvokeCommand function (anymore). this method of calling is faster.
            // The hash is only looked up once, insted of twice(!) per NetworkBehaviour on the object.

            if (gameObject == null)
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logWarn) { Debug.LogWarning("Command [" + errorCmdName + "] received for deleted object [netId=" + netId + "]"); }
                return;
            }

            // find the matching Command function and networkBehaviour class
            NetworkBehaviour.CmdDelegate invokeFunction;
            Type invokeClass;
            bool invokeFound = NetworkBehaviour.GetInvokerForHashCommand(cmdHash, out invokeClass, out invokeFunction);
            if (!invokeFound)
            {
                // We don't get a valid lookup of the command name when it doesn't exist...
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logError) { Debug.LogError("Found no receiver for incoming [" + errorCmdName + "] on " + gameObject + ",  the server and client should have the same NetworkBehaviour instances [netId=" + netId + "]."); }
                return;
            }

            // find the right component to invoke the function on
            NetworkBehaviour invokeComponent;
            if (!GetInvokeComponent(cmdHash, invokeClass, out invokeComponent))
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logWarn) { Debug.LogWarning("Command [" + errorCmdName + "] handler not found [netId=" + netId + "]"); }
                return;
            }

            invokeFunction(invokeComponent, reader);

#if UNITY_EDITOR
            Profiler.IncrementStatIncoming(MsgType.Command, NetworkBehaviour.GetCmdHashCmdName(cmdHash));
#endif
        }

        // happens on client
        internal void HandleRPC(int cmdHash, NetworkReader reader)
        {
            // this doesn't use NetworkBehaviour.InvokeClientRpc function (anymore). this method of calling is faster.
            // The hash is only looked up once, insted of twice(!) per NetworkBehaviour on the object.

            if (gameObject == null)
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logWarn) { Debug.LogWarning("ClientRpc [" + errorCmdName + "] received for deleted object [netId=" + netId + "]"); }
                return;
            }

            // find the matching ClientRpc function and networkBehaviour class
            NetworkBehaviour.CmdDelegate invokeFunction;
            Type invokeClass;
            bool invokeFound = NetworkBehaviour.GetInvokerForHashClientRpc(cmdHash, out invokeClass, out invokeFunction);
            if (!invokeFound)
            {
                // We don't get a valid lookup of the command name when it doesn't exist...
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logError) { Debug.LogError("Found no receiver for incoming [" + errorCmdName + "] on " + gameObject + ",  the server and client should have the same NetworkBehaviour instances [netId=" + netId + "]."); }
                return;
            }

            // find the right component to invoke the function on
            NetworkBehaviour invokeComponent;
            if (!GetInvokeComponent(cmdHash, invokeClass, out invokeComponent))
            {
                string errorCmdName = NetworkBehaviour.GetCmdHashHandlerName(cmdHash);
                if (LogFilter.logWarn) { Debug.LogWarning("ClientRpc [" + errorCmdName + "] handler not found [netId=" + netId + "]"); }
                return;
            }

            invokeFunction(invokeComponent, reader);

#if UNITY_EDITOR
            Profiler.IncrementStatIncoming(MsgType.Rpc, NetworkBehaviour.GetCmdHashRpcName(cmdHash));
#endif
        }

        // invoked by unity runtime immediately after the regular "Update()" function.
        public void UNetUpdate()
        {
            // check if any behaviours are ready to send
            uint dirtyChannelBits = 0;
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                int channelId = comp.GetDirtyChannel();
                if (channelId != -1)
                {
                    dirtyChannelBits |= (uint)(1 << channelId);
                }
            }
            if (dirtyChannelBits == 0)
                return;

            for (int channelId = 0; channelId < NetworkServer.numChannels; channelId++)
            {
                if ((dirtyChannelBits & (uint)(1 << channelId)) != 0)
                {
                    s_UpdateWriter.StartMessage(MsgType.UpdateVars);
                    s_UpdateWriter.Write(netId);

                    bool wroteData = false;
                    short oldPos;
                    for (int i = 0; i < m_NetworkBehaviours.Length; i++)
                    {
                        oldPos = s_UpdateWriter.Position;
                        NetworkBehaviour comp = m_NetworkBehaviours[i];
                        if (comp.GetDirtyChannel() != channelId)
                        {
                            // component could write more than one dirty-bits, so call the serialize func
                            comp.OnSerialize(s_UpdateWriter, false);
                            continue;
                        }

                        if (comp.OnSerialize(s_UpdateWriter, false))
                        {
                            comp.ClearAllDirtyBits();

#if UNITY_EDITOR
                            Profiler.IncrementStatOutgoing(MsgType.UpdateVars, comp.GetType().Name);
#endif

                            wroteData = true;
                        }
                        if (s_UpdateWriter.Position - oldPos > NetworkServer.maxPacketSize)
                        {
                            if (LogFilter.logWarn) { Debug.LogWarning("Large state update of " + (s_UpdateWriter.Position - oldPos) + " bytes for netId:" + netId + " from script:" + comp); }
                        }
                    }

                    if (!wroteData)
                    {
                        // nothing to send.. this could be a script with no OnSerialize function setting dirty bits
                        continue;
                    }

                    s_UpdateWriter.FinishMessage();
                    NetworkServer.SendWriterToReady(gameObject, s_UpdateWriter, channelId);
                }
            }
        }

        internal void OnUpdateVars(NetworkReader reader, bool initialState)
        {
            if (initialState && m_NetworkBehaviours == null)
            {
                m_NetworkBehaviours = GetComponents<NetworkBehaviour>();
            }
            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];


#if UNITY_EDITOR
                var oldReadPos = reader.Position;
#endif
                comp.OnDeserialize(reader, initialState);
#if UNITY_EDITOR
                if (reader.Position - oldReadPos > 1)
                {
                    Profiler.IncrementStatIncoming(MsgType.UpdateVars, comp.GetType().Name);
                }
#endif
            }
        }

        internal void SetLocalPlayer(short localPlayerControllerId)
        {
            m_IsLocalPlayer = true;
            m_PlayerId = localPlayerControllerId;

            // there is an ordering issue here that originAuthority solves. OnStartAuthority should only be called if m_HasAuthority was false when this function began,
            // or it will be called twice for this object. But that state is lost by the time OnStartAuthority is called below, so the original value is cached
            // here to be checked below.
            bool originAuthority = m_HasAuthority;
            if (localPlayerAuthority)
            {
                m_HasAuthority = true;
            }

            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                comp.OnStartLocalPlayer();

                if (localPlayerAuthority && !originAuthority)
                {
                    comp.OnStartAuthority();
                }
            }
        }

        internal void SetConnectionToServer(NetworkConnection conn)
        {
            m_ConnectionToServer = conn;
        }

        internal void SetConnectionToClient(NetworkConnection conn, short newPlayerControllerId)
        {
            m_PlayerId = newPlayerControllerId;
            m_ConnectionToClient = conn;
        }

        internal void OnNetworkDestroy()
        {
            for (int i = 0;
                 m_NetworkBehaviours != null && i < m_NetworkBehaviours.Length;
                 i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                comp.OnNetworkDestroy();
            }
            m_IsServer = false;
        }

        internal void ClearObservers()
        {
            if (m_Observers != null)
            {
                int count = m_Observers.Count;
                for (int i = 0; i < count; i++)
                {
                    var c = m_Observers[i];
                    c.RemoveFromVisList(this, true);
                }
                m_Observers.Clear();
                m_ObserverConnections.Clear();
            }
        }

        internal void AddObserver(NetworkConnection conn)
        {
            if (m_Observers == null)
            {
                if (LogFilter.logError) { Debug.LogError("AddObserver for " + gameObject + " observer list is null"); }
                return;
            }

            // uses hashset for better-than-list-iteration lookup performance.
            if (m_ObserverConnections.Contains(conn.connectionId))
            {
                if (LogFilter.logDebug) { Debug.Log("Duplicate observer " + conn.address + " added for " + gameObject); }
                return;
            }

            if (LogFilter.logDev) { Debug.Log("Added observer " + conn.address + " added for " + gameObject); }

            m_Observers.Add(conn);
            m_ObserverConnections.Add(conn.connectionId);
            conn.AddToVisList(this);
        }

        internal void RemoveObserver(NetworkConnection conn)
        {
            if (m_Observers == null)
                return;

            // NOTE this is linear performance now..
            m_Observers.Remove(conn);
            m_ObserverConnections.Remove(conn.connectionId);
            conn.RemoveFromVisList(this, false);
        }

        public void RebuildObservers(bool initialize)
        {
            if (m_Observers == null)
                return;

            bool changed = false;
            bool result = false;
            HashSet<NetworkConnection> newObservers = new HashSet<NetworkConnection>();
            HashSet<NetworkConnection> oldObservers = new HashSet<NetworkConnection>(m_Observers);

            for (int i = 0; i < m_NetworkBehaviours.Length; i++)
            {
                NetworkBehaviour comp = m_NetworkBehaviours[i];
                result |= comp.OnRebuildObservers(newObservers, initialize);
            }
            if (!result)
            {
                // none of the behaviours rebuilt our observers, use built-in rebuild method
                if (initialize)
                {
                    for (int i = 0; i < NetworkServer.connections.Count; i++)
                    {
                        var conn = NetworkServer.connections[i];
                        if (conn == null) continue;
                        if (conn.isReady)
                            AddObserver(conn);
                    }

                    for (int i = 0; i < NetworkServer.localConnections.Count; i++)
                    {
                        var conn = NetworkServer.localConnections[i];
                        if (conn == null) continue;
                        if (conn.isReady)
                            AddObserver(conn);
                    }
                }
                return;
            }

            // apply changes from rebuild
            foreach (var conn in newObservers)
            {
                if (conn == null)
                {
                    continue;
                }

                if (!conn.isReady)
                {
                    if (LogFilter.logWarn) { Debug.LogWarning("Observer is not ready for " + gameObject + " " + conn); }
                    continue;
                }

                if (initialize || !oldObservers.Contains(conn))
                {
                    // new observer
                    conn.AddToVisList(this);
                    if (LogFilter.logDebug) { Debug.Log("New Observer for " + gameObject + " " + conn); }
                    changed = true;
                }
            }

            foreach (var conn in oldObservers)
            {
                if (!newObservers.Contains(conn))
                {
                    // removed observer
                    conn.RemoveFromVisList(this, false);
                    if (LogFilter.logDebug) { Debug.Log("Removed Observer for " + gameObject + " " + conn); }
                    changed = true;
                }
            }

            // special case for local client.
            if (initialize)
            {
                for (int i = 0; i < NetworkServer.localConnections.Count; i++)
                {
                    if (!newObservers.Contains(NetworkServer.localConnections[i]))
                    {
                        OnSetLocalVisibility(false);
                    }
                }
            }

            if (!changed)
                return;

            m_Observers = new List<NetworkConnection>(newObservers);

            // rebuild hashset once we have the final set of new observers
            m_ObserverConnections.Clear();
            for (int i = 0; i < m_Observers.Count; i++)
            {
                m_ObserverConnections.Add(m_Observers[i].connectionId);
            }
        }

        public bool RemoveClientAuthority(NetworkConnection conn)
        {
            if (!isServer)
            {
                if (LogFilter.logError) { Debug.LogError("RemoveClientAuthority can only be call on the server for spawned objects."); }
                return false;
            }

            if (connectionToClient != null)
            {
                if (LogFilter.logError) { Debug.LogError("RemoveClientAuthority cannot remove authority for a player object"); }
                return false;
            }

            if (m_ClientAuthorityOwner == null)
            {
                if (LogFilter.logError) { Debug.LogError("RemoveClientAuthority for " + gameObject + " has no clientAuthority owner."); }
                return false;
            }

            if (m_ClientAuthorityOwner != conn)
            {
                if (LogFilter.logError) { Debug.LogError("RemoveClientAuthority for " + gameObject + " has different owner."); }
                return false;
            }

            m_ClientAuthorityOwner.RemoveOwnedObject(this);
            m_ClientAuthorityOwner = null;

            // server now has authority (this is only called on server)
            ForceAuthority(true);

            // send msg to that client
            var msg = new ClientAuthorityMessage();
            msg.netId = netId;
            msg.authority = false;
            conn.Send(MsgType.LocalClientAuthority, msg);

            if (clientAuthorityCallback != null)
            {
                clientAuthorityCallback(conn, this, false);
            }
            return true;
        }

        public bool AssignClientAuthority(NetworkConnection conn)
        {
            if (!isServer)
            {
                if (LogFilter.logError) { Debug.LogError("AssignClientAuthority can only be call on the server for spawned objects."); }
                return false;
            }
            if (!localPlayerAuthority)
            {
                if (LogFilter.logError) { Debug.LogError("AssignClientAuthority can only be used for NetworkIdentity component with LocalPlayerAuthority set."); }
                return false;
            }

            if (m_ClientAuthorityOwner != null && conn != m_ClientAuthorityOwner)
            {
                if (LogFilter.logError) { Debug.LogError("AssignClientAuthority for " + gameObject + " already has an owner. Use RemoveClientAuthority() first."); }
                return false;
            }

            if (conn == null)
            {
                if (LogFilter.logError) { Debug.LogError("AssignClientAuthority for " + gameObject + " owner cannot be null. Use RemoveClientAuthority() instead."); }
                return false;
            }

            m_ClientAuthorityOwner = conn;
            m_ClientAuthorityOwner.AddOwnedObject(this);

            // server no longer has authority (this is called on server). Note that local client could re-acquire authority below
            ForceAuthority(false);

            // send msg to that client
            var msg = new ClientAuthorityMessage();
            msg.netId = netId;
            msg.authority = true;
            conn.Send(MsgType.LocalClientAuthority, msg);

            if (clientAuthorityCallback != null)
            {
                clientAuthorityCallback(conn, this, true);
            }
            return true;
        }

        // marks the identity for future reset, this is because we cant reset the identity during destroy
        // as people might want to be able to read the members inside OnDestroy(), and we have no way
        // of invoking reset after OnDestroy is called.
        internal void MarkForReset()
        {
            m_Reset = true;
        }

        // if we have marked an identity for reset we do the actual reset.
        internal void Reset()
        {
            if (!m_Reset)
                return;

            m_Reset = false;
            m_IsServer = false;
            m_IsClient = false;
            m_HasAuthority = false;

            m_NetId = NetworkInstanceId.Zero;
            m_IsLocalPlayer = false;
            m_ConnectionToServer = null;
            m_ConnectionToClient = null;
            m_PlayerId = -1;
            m_NetworkBehaviours = null;

            ClearObservers();
            m_ClientAuthorityOwner = null;
        }


#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void OnInitializeOnLoad()
        {
            // The transport layer has state in C++, so when the C# state is lost (on domain reload), the C++ transport layer must be shutown as well.
            NetworkManager.OnDomainReload();
        }
#endif

        [RuntimeInitializeOnLoadMethod]
        static void OnRuntimeInitializeOnLoad()
        {
            var go = new GameObject("UNETCallbacks");
            go.AddComponent(typeof(NetworkCallbacks));
            go.hideFlags = go.hideFlags | HideFlags.HideAndDontSave;
        }

        static internal void UNetStaticUpdate()
        {
            NetworkServer.Update();
            NetworkClient.UpdateClients();
            NetworkManager.UpdateScene();

#if UNITY_EDITOR
            Profiler.NewProfilerTick();
#endif
        }
    };
}
#endif //ENABLE_UNET
