using System;
using UnityEngine.Events;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.UI;

////REVIEW: should we automatically pool/retain up to maxPlayerCount player instances?

////TODO: add support for reacting to players missing devices

namespace UnityEngine.Experimental.Input.Plugins.PlayerInput
{
    /// <summary>
    /// Manages joining and leaving of players.
    /// </summary>
    /// <remarks>
    /// This is a singleton component. Only one instance is meant to be active in a game
    /// at any one time. To retrieve the current instance, use <see cref="instance"/>.
    /// </remarks>
    public class PlayerInputManager : MonoBehaviour
    {
        public const string PlayerJoinedMessage = "OnPlayerJoined";
        public const string PlayerLeftMessage = "OnPlayerLeft";
        public const string PlayerJoinFailedMessage = "OnPlayerJoinFailed";
        public const string SplitScreenSetupChanged = "OnSplitScreenSetupChanged";

        /// <summary>
        /// If enabled, each player will automatically be assigned
        /// </summary>
        /// <remarks>
        /// For this to work, the <see cref="GameObject"/> associated with each <see cref="PlayerInput"/>
        /// component must have a <see cref="Camera"/> component either directly on the GameObject or
        /// on a child.
        ///
        /// Note that as player join, the screen may be increasingly subdivided and players may see their
        /// previous screen area getting resized.
        /// </remarks>
        public bool splitScreen
        {
            get => m_SplitScreen;
            set
            {
                if (m_SplitScreen == value)
                    return;

                m_SplitScreen = value;

                if (!m_SplitScreen)
                {
                    // Reset rects on all player cameras.
                    foreach (var player in PlayerInput.all)
                    {
                        var camera = player.camera;
                        if (camera != null)
                            camera.rect = new Rect(0, 0, 1, 1);
                    }
                }
                else
                {
                    UpdateSplitScreen();
                }
            }
        }

        ////REVIEW: we probably need support for filling unused screen areas automatically
        /// <summary>
        /// If <see cref="splitScreen"/> is enabled, this property determines whether subdividing the screen is allowed to
        /// produce screen areas that have an aspect ratio different from the screen resolution.
        /// </summary>
        /// <remarks>
        /// By default, when <see cref="splitScreen"/> is enabled, the manager will add or remove screen subdivisions in
        /// steps of two. This means that when, for example, the second player is added, the screen will be subdivided into
        /// a left and a right screen area; the left one allocated to the first player and the right one allocated to the
        /// second player.
        ///
        /// This behavior makes optimal use of screen real estate but will result in screen areas that have aspect ratios
        /// different from the screen resolution. If this is not acceptable, this property can be set to true to enforce
        /// split-screen to only create screen areas that have the same aspect ratio of the screen.
        ///
        /// This results in the screen being subdivided more aggressively. When, for example, a second player is added,
        /// the screen will immediately be divided into a four-way split-screen setup with the lower two screen areas
        /// not being used.
        ///
        /// This property is irrelevant if <see cref="fixedNumberOfSplitScreens"/> is used.
        /// </remarks>
        public bool maintainAspectRatioInSplitScreen
        {
            get => m_MaintainAspectRatioInSplitScreen;
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// This property
        /// </summary>
        public int fixedNumberOfSplitScreens
        {
            get => m_FixedNumberOfSplitScreens;
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// If this is non-zero, split-screen areas will be
        /// </summary>
        public float splitScreenBorderWidth
        {
            get => m_SplitScreenBorderWidth;
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// The normalized screen rectangle available for allocating player split-screens into.
        /// </summary>
        /// <remarks>
        /// This is only used if <see cref="splitScreen"/> is true.
        ///
        /// By default it is set to <c>(0,0,1,1)</c>, i.e. the entire screen area will be used for player screens.
        /// If, for example, part of the screen should display a UI/information shared by all players, this
        /// property can be used to cut off the area and not have it used by PlayerInputManager.
        /// </remarks>
        public Rect splitScreenArea
        {
            get => m_SplitScreenRect;
            set { throw new NotImplementedException(); }
        }

        public int playerCount => PlayerInput.s_AllActivePlayersCount;

        /// <summary>
        /// Maximum number of players allowed concurrently in the game.
        /// </summary>
        /// <remarks>
        /// If this limit is reached, joining is turned off automatically.
        ///
        /// By default this is set to -1. Any negative value deactivates the player limit and allows
        /// arbitrary many players to join.
        /// </remarks>
        public int maxPlayerCount
        {
            get => m_MaxPlayerCount;
            set { throw new NotImplementedException(); }
        }

        public bool joiningEnabled
        {
            get => m_AllowJoining;
        }

        public PlayerJoinBehavior joinBehavior
        {
            get => m_JoinBehavior;
            set
            {
                if (m_JoinBehavior == value)
                    return;

                var joiningEnabled = m_AllowJoining;
                if (joiningEnabled)
                    DisableJoining();
                m_JoinBehavior = value;
                if (joiningEnabled)
                    EnableJoining();
            }
        }

        public InputActionProperty joinAction
        {
            get => m_JoinAction;
            set
            {
                if (m_JoinAction == value)
                    return;

                ////REVIEW: should we suppress notifications for temporary disables?

                var joinEnabled = m_AllowJoining && m_JoinBehavior == PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;
                if (joinEnabled)
                    DisableJoining();

                m_JoinAction = value;

                if (joinEnabled)
                    EnableJoining();
            }
        }

        public PlayerNotifications notificationBehavior
        {
            get => m_NotificationBehavior;
            set => m_NotificationBehavior = value;
        }

        public PlayerJoinedEvent playerJoinedEvent
        {
            get
            {
                if (m_PlayerJoinedEvent == null)
                    m_PlayerJoinedEvent = new PlayerJoinedEvent();
                return m_PlayerJoinedEvent;
            }
        }

        public PlayerLeftEvent playerLeftEvent
        {
            get
            {
                if (m_PlayerLeftEvent == null)
                    m_PlayerLeftEvent = new PlayerLeftEvent();
                return m_PlayerLeftEvent;
            }
        }

        public event Action<PlayerInput> onPlayerJoined
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_PlayerJoinedCallbacks.AppendWithCapacity(value, 4);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var index = m_PlayerJoinedCallbacks.IndexOf(value);
                if (index != -1)
                    m_PlayerJoinedCallbacks.RemoveAtWithCapacity(index);
            }
        }

        public event Action<PlayerInput> onPlayerLeft
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                m_PlayerLeftCallbacks.AppendWithCapacity(value, 4);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var index = m_PlayerLeftCallbacks.IndexOf(value);
                if (index != -1)
                    m_PlayerLeftCallbacks.RemoveAtWithCapacity(index);
            }
        }

        public GameObject playerPrefab
        {
            get => m_PlayerPrefab;
            set => m_PlayerPrefab = value;
        }

        /// <summary>
        /// Optional delegate that creates players.
        /// </summary>
        /// <remarks>
        /// This can be used in place of <see cref="playerPrefab"/> to take control over how
        /// player objects are created. If this property is not null, <see cref="playerPrefab"/>
        /// will be ignored (and can be left at null) and the delegate will be invoked to create
        /// new players.
        /// </remarks>
        public Func<PlayerInput> onCreatePlayer
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public Func<PlayerInput> onDestroyPlayer
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public static PlayerInputManager instance { get; private set; }

        public void EnableJoining()
        {
            switch (m_JoinBehavior)
            {
                case PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed:
                    if (!m_UnpairedDeviceUsedDelegateHooked)
                    {
                        if (m_UnpairedDeviceUsedDelegate == null)
                            m_UnpairedDeviceUsedDelegate = OnUnpairedDeviceUsed;
                        InputUser.onUnpairedDeviceUsed += m_UnpairedDeviceUsedDelegate;
                        m_UnpairedDeviceUsedDelegateHooked = true;
                        ++InputUser.listenForUnpairedDeviceActivity;
                    }
                    break;

                case PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered:
                    // Hook into join action if we have one.
                    if (m_JoinAction.action != null)
                    {
                        if (!m_JoinActionDelegateHooked)
                        {
                            if (m_JoinActionDelegate == null)
                                m_JoinActionDelegate = JoinPlayerFromActionIfNotAlreadyJoined;
                            m_JoinAction.action.performed += m_JoinActionDelegate;
                            m_JoinActionDelegateHooked = true;
                        }
                        m_JoinAction.action.Enable();
                    }
                    else
                    {
                        Debug.LogError(
                            "No join action configured on PlayerInputManager but join behavior is set to JoinPlayersWhenActionIsTriggered",
                            this);
                    }
                    break;
            }

            m_AllowJoining = true;
        }

        public void DisableJoining()
        {
            switch (m_JoinBehavior)
            {
                case PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed:
                    if (m_UnpairedDeviceUsedDelegateHooked)
                    {
                        InputUser.onUnpairedDeviceUsed -= m_UnpairedDeviceUsedDelegate;
                        m_UnpairedDeviceUsedDelegateHooked = false;
                        --InputUser.listenForUnpairedDeviceActivity;
                    }
                    break;

                case PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered:
                    if (m_JoinActionDelegateHooked)
                    {
                        var joinAction = m_JoinAction.action;
                        if (joinAction != null)
                            m_JoinAction.action.performed -= m_JoinActionDelegate;
                        m_JoinActionDelegateHooked = false;
                    }
                    m_JoinAction.action?.Disable();
                    break;
            }

            m_AllowJoining = false;
        }

        /// <summary>
        /// Join a new player based on input on a UI element.
        /// </summary>
        /// <remarks>
        /// This should be called directly from a UI callback such as <see cref="Button.onClick"/>. The device
        /// that the player joins with is taken from the device that was used to interact with the UI element.
        /// </remarks>
        public void JoinPlayerFromUI()
        {
            if (!CheckIfPlayerCanJoin())
                return;

            //find used device

            throw new NotImplementedException();
        }

        /// <summary>
        /// Join a new player based on input received through an <see cref="InputAction"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <remarks>
        /// </remarks>
        public void JoinPlayerFromAction(InputAction.CallbackContext context)
        {
            if (!CheckIfPlayerCanJoin())
                return;

            var device = context.control.device;
            JoinPlayer(pairWithDevice: device);
        }

        public void JoinPlayerFromActionIfNotAlreadyJoined(InputAction.CallbackContext context)
        {
            if (!CheckIfPlayerCanJoin())
                return;

            var device = context.control.device;
            if (PlayerInput.FindFirstPairedToDevice(device) != null)
                return;

            JoinPlayer(pairWithDevice: device);
        }

        public void JoinPlayer(int playerIndex = -1, int splitScreenIndex = -1, string controlScheme = null, InputDevice pairWithDevice = null)
        {
            if (!CheckIfPlayerCanJoin(playerIndex))
                return;

            PlayerInput.Instantiate(m_PlayerPrefab, playerIndex: playerIndex, splitScreenIndex: splitScreenIndex,
                controlScheme: controlScheme, pairWithDevice: pairWithDevice);
        }

        public void JoinPlayer(int playerIndex = -1, int splitScreenIndex = -1, string controlScheme = null, params InputDevice[] pairWithDevices)
        {
            if (!CheckIfPlayerCanJoin(playerIndex))
                return;

            PlayerInput.Instantiate(m_PlayerPrefab, playerIndex: playerIndex, splitScreenIndex: splitScreenIndex,
                controlScheme: controlScheme, pairWithDevices: pairWithDevices);
        }

        [SerializeField] internal PlayerNotifications m_NotificationBehavior;
        [SerializeField] internal int m_MaxPlayerCount = -1;
        [SerializeField] internal bool m_AllowJoining = true;
        [SerializeField] internal bool m_JoinPlayersWithMissingDevices;
        [SerializeField] internal PlayerJoinBehavior m_JoinBehavior;
        [SerializeField] internal PlayerJoinedEvent m_PlayerJoinedEvent;
        [SerializeField] internal PlayerLeftEvent m_PlayerLeftEvent;
        [SerializeField] internal InputActionProperty m_JoinAction;
        [SerializeField] internal GameObject m_PlayerPrefab;
        [SerializeField] internal bool m_SplitScreen;
        [SerializeField] internal bool m_MaintainAspectRatioInSplitScreen;
        [SerializeField] internal int m_FixedNumberOfSplitScreens = -1;
        [SerializeField] internal float m_SplitScreenBorderWidth;
        [SerializeField] internal Rect m_SplitScreenRect = new Rect(0, 0, 1, 1);

        [NonSerialized] private bool m_JoinActionDelegateHooked;
        [NonSerialized] private bool m_UnpairedDeviceUsedDelegateHooked;
        [NonSerialized] private Action<InputAction.CallbackContext> m_JoinActionDelegate;
        [NonSerialized] private Action<InputControl> m_UnpairedDeviceUsedDelegate;
        [NonSerialized] private InlinedArray<Action<PlayerInput>> m_PlayerJoinedCallbacks;
        [NonSerialized] private InlinedArray<Action<PlayerInput>> m_PlayerLeftCallbacks;

        internal static string[] messages => new[]
        {
            PlayerJoinedMessage,
            PlayerLeftMessage,
        };

        private bool CheckIfPlayerCanJoin(int playerIndex = -1)
        {
            if (m_PlayerPrefab == null)
            {
                Debug.LogError("playerPrefab must be set in order to be able to join new players", this);
                return false;
            }

            if (m_MaxPlayerCount >= 0 && playerCount >= m_MaxPlayerCount)
            {
                Debug.LogError("Have reached maximum player count of " + maxPlayerCount, this);
                return false;
            }

            // If we have a player index, make sure it's unique.
            if (playerIndex != -1)
            {
                for (var i = 0; i < PlayerInput.s_AllActivePlayersCount; ++i)
                    if (PlayerInput.s_AllActivePlayers[i].playerIndex == playerIndex)
                    {
                        Debug.LogError(
                            $"Player index #{playerIndex} is already taken by player {PlayerInput.s_AllActivePlayers[i]}",
                            PlayerInput.s_AllActivePlayers[i]);
                        return false;
                    }
            }

            return true;
        }

        private void OnUnpairedDeviceUsed(InputControl control)
        {
            if (!m_AllowJoining)
                return;

            if (m_JoinBehavior == PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed)
            {
                // Make sure it's a button that was actuated.
                if (!(control is ButtonControl))
                    return;

                // Make sure it's a device that is usable by the player's actions. We don't want
                // to join a player who's then stranded and has no way to actually interact with the game.
                if (!IsDeviceUsableWithPlayerActions(control.device))
                    return;

                JoinPlayer(pairWithDevice: control.device);
            }
        }

        private void OnEnable()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple PlayerInputManagers in the game. There should only be one PlayerInputManager", this);
                return;
            }

            // Join all players already in the game.
            for (var i = 0; i < PlayerInput.s_AllActivePlayersCount; ++i)
                NotifyPlayerJoined(PlayerInput.s_AllActivePlayers[i]);

            if (m_AllowJoining)
                EnableJoining();
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;

            if (m_AllowJoining)
                DisableJoining();
        }

        /// <summary>
        /// If split-screen is enabled, then for each player in the game, adjust the player's <see cref="Camera.rect"/>
        /// to fit the player's split screen area according to the number of players currently in the game and the
        /// current split-screen configuration.
        /// </summary>
        private void UpdateSplitScreen()
        {
            // Nothing to do if split-screen is not enabled.
            if (!m_SplitScreen)
                return;

            // Determine number of split-screens to create based on highest player index we have.
            var minSplitScreenCount = 0;
            foreach (var player in PlayerInput.all)
            {
                if (player.playerIndex >= minSplitScreenCount)
                    minSplitScreenCount = player.playerIndex + 1;
            }

            // Adjust to fixed number if we have it.
            if (m_FixedNumberOfSplitScreens > 0)
            {
                if (m_FixedNumberOfSplitScreens < minSplitScreenCount)
                    Debug.LogWarning(
                        $"Highest playerIndex of {minSplitScreenCount} exceeds fixed number of split-screens of {m_FixedNumberOfSplitScreens}",
                        this);

                minSplitScreenCount = m_FixedNumberOfSplitScreens;
            }

            // Determine divisions along X and Y. Usually, we have a square grid of split-screens so all we need to
            // do is make it large enough to fit all players.
            var numDivisionsX = Mathf.CeilToInt(Mathf.Sqrt(minSplitScreenCount));
            var numDivisionsY = numDivisionsX;
            if (!m_MaintainAspectRatioInSplitScreen && numDivisionsX * (numDivisionsX - 1) >= minSplitScreenCount)
            {
                // We're allowed to produce split-screens with aspect ratios different from the screen meaning
                // that we always add one more column before finally adding an entirely new row.
                numDivisionsY -= 1;
            }

            // Assign split-screen area to each player.
            foreach (var player in PlayerInput.all)
            {
                // Make sure the player's splitScreenIndex isn't out of range.
                var splitScreenIndex = player.splitScreenIndex;
                if (splitScreenIndex >= numDivisionsX * numDivisionsY)
                {
                    Debug.LogError(
                        $"Split-screen index of {splitScreenIndex} on player is out of range (have {numDivisionsX * numDivisionsY} screens); resetting to playerIndex",
                        player);
                    player.m_SplitScreenIndex = player.playerIndex;
                }

                // Make sure we have a camera.
                var camera = player.camera;
                if (camera == null)
                {
                    Debug.LogError(
                        "Player has no camera associated with it. Cannot set up split-screen. Point PlayerInput.camera to camera for player.",
                        player);
                    continue;
                }

                // Assign split-screen area based on m_SplitScreenRect.
                var column = splitScreenIndex % numDivisionsX;
                var row = splitScreenIndex / numDivisionsX;
                var rect = new Rect
                {
                    width = m_SplitScreenRect.width / numDivisionsX,
                    height = m_SplitScreenRect.height / numDivisionsY
                };
                rect.x = m_SplitScreenRect.x + column * rect.width;
                // Y is bottom-to-top but we fill from top down.
                rect.y = m_SplitScreenRect.y + m_SplitScreenRect.height - (row + 1) * rect.height;
                camera.rect = rect;
            }
        }

        private bool IsDeviceUsableWithPlayerActions(InputDevice device)
        {
            Debug.Assert(device != null);

            if (m_PlayerPrefab == null)
                return true;

            var playerInput = m_PlayerPrefab.GetComponentInChildren<PlayerInput>();
            if (playerInput == null)
                return true;

            var actions = playerInput.actions;
            if (actions == null)
                return true;

            foreach (var actionMap in actions.actionMaps)
                if (actionMap.IsUsableWithDevice(device))
                    return true;

            return false;
        }

        /// <summary>
        /// Called by <see cref="PlayerInput"/> when it is enabled.
        /// </summary>
        /// <param name="player"></param>
        internal void NotifyPlayerJoined(PlayerInput player)
        {
            Debug.Assert(player != null);

            UpdateSplitScreen();

            switch (m_NotificationBehavior)
            {
                case PlayerNotifications.SendMessages:
                    SendMessage(PlayerJoinedMessage, player, SendMessageOptions.DontRequireReceiver);
                    break;

                case PlayerNotifications.BroadcastMessages:
                    BroadcastMessage(PlayerJoinedMessage, player, SendMessageOptions.DontRequireReceiver);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_PlayerJoinedEvent?.Invoke(player);
                    break;

                case PlayerNotifications.InvokeCSharpEvents:
                    DelegateHelpers.InvokeCallbacksSafe(ref m_PlayerJoinedCallbacks, player, "onPlayerJoined");
                    break;
            }
        }

        /// <summary>
        /// Called by <see cref="PlayerInput"/> when it is disabled.
        /// </summary>
        /// <param name="player"></param>
        internal void NotifyPlayerLeft(PlayerInput player)
        {
            Debug.Assert(player != null);

            UpdateSplitScreen();

            switch (m_NotificationBehavior)
            {
                case PlayerNotifications.SendMessages:
                    SendMessage(PlayerLeftMessage, player, SendMessageOptions.DontRequireReceiver);
                    break;

                case PlayerNotifications.BroadcastMessages:
                    BroadcastMessage(PlayerLeftMessage, player, SendMessageOptions.DontRequireReceiver);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_PlayerLeftEvent?.Invoke(player);
                    break;

                case PlayerNotifications.InvokeCSharpEvents:
                    DelegateHelpers.InvokeCallbacksSafe(ref m_PlayerLeftCallbacks, player, "onPlayerLeft");
                    break;
            }
        }

        [Serializable]
        public class PlayerJoinedEvent : UnityEvent<PlayerInput>
        {
        }

        [Serializable]
        public class PlayerLeftEvent : UnityEvent<PlayerInput>
        {
        }
    }
}
