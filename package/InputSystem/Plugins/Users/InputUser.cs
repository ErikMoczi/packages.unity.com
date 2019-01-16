using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Profiling;

////REVIEW: do we need to handle the case where devices are added to a user that are each associated with a different user account

////TODO: option to bind to *all* devices instead of just the paired ones (bindToAllDevices)

////TODO: the account selection stuff needs cleanup; the current flow is too convoluted

namespace UnityEngine.Experimental.Input.Plugins.Users
{
    /// <summary>
    /// Represents a specific user/player interacting with one or more devices and input actions.
    /// </summary>
    /// <remarks>
    /// Principally, an InputUser represents a human interacting with the application. Moreover, at any point
    /// each InputUser represents a human actor distinct from all other InputUsers in the system.
    ///
    /// Each user has one or more paired devices. In general, these devices are unique to each user. However,
    /// it is permitted to use <see cref="PerformPairingWithDevice"/> to pair the same device to multiple users.
    /// This can be useful in setups such as split-keyboard (e.g. one user using left side of keyboard and the
    /// other the right one) use or hotseat-style gameplay (e.g. two players taking turns on the same game controller).
    ///
    /// A user may be associated with a platform user account (<see cref="platformUserAccountHandle"/>), if supported by the
    /// platform and the devices used. Support for this is commonly found on consoles. Note that the account
    /// associated with an InputUser may change if the player uses the system's facilities to switch to a different
    /// account (<see cref="InputUserChange.AccountChanged"/>). On Xbox and Switch, this may also be initiated from
    /// the application by passing <see cref="InputUserPairingOptions.ForcePlatformUserAccountSelection"/> to
    /// <see cref="PerformPairingWithDevice"/>.
    ///
    /// Platforms that support user account association are <see cref="RuntimePlatform.XboxOne"/>,
    /// <see cref="RuntimePlatform.PS4"/>, <see cref="RuntimePlatform.Switch"/>, <see cref="RuntimePlatform.WSAPlayerX86"/>,
    /// <see cref="RuntimePlatform.WSAPlayerX64"/>, and <see cref="RuntimePlatform.WSAPlayerARM"/>. Note that
    /// for WSA/UWP apps, the "User Account Information" capability must be enabled for the app in order for
    /// user information to come through on input devices.
    /// </remarks>
    /// <seealso cref="InputUserChange"/>
    public struct InputUser : IEquatable<InputUser>
    {
        public const uint kInvalidId = 0;

        /// <summary>
        /// Whether this is a currently active user record in <see cref="all"/>.
        /// </summary>
        /// <remarks>
        /// Users that are removed (<see cref="UnpairDevicesAndRemoveUser"/>) will become invalid.
        /// </remarks>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.Removed"/>
        public bool valid
        {
            get
            {
                if (m_Id == kInvalidId || s_AllUserCount == 0)
                    return false;

                // See if there's a currently active user with the given ID.
                for (var i = 0; i < s_AllUserCount; ++i)
                    if (s_AllUsers[i].m_Id == m_Id)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// The sequence number of the user.
        /// </summary>
        /// <remarks>
        /// It can be useful to establish a sorting of players locally such that it is
        /// known who is the first player, who is the second, and so on. This property
        /// gives the positioning of the user within <see cref="all"/>.
        ///
        /// Note that the index of a user may change as users are added and removed.
        /// </remarks>
        /// <seealso cref="all"/>
        public int index
        {
            get
            {
                if (m_Id == kInvalidId)
                    throw new InvalidOperationException("Invalid user");

                var userIndex = TryFindUserIndex(m_Id);
                if (userIndex == -1)
                    throw new InvalidOperationException(string.Format("User with ID {0} is no longer valid", m_Id));

                return userIndex;
            }
        }

        /// <summary>
        /// The unique numeric ID of the user.
        /// </summary>
        /// <remarks>
        /// The ID of a user is internally assigned and cannot be changed over its lifetime. No two users, even
        /// if not concurrently active, will receive the same ID.
        ///
        /// Note that this is not the same as the platform's internal user ID (if relevant on the current
        /// platform). To get the ID that the platform uses to identify the user, use <see cref="platformUserAccountHandle"/>.
        ///
        /// The ID stays valid and unique even if the user is removed and no longer <see cref="valid"/>.
        /// </remarks>
        /// <seealso cref="platformUserAccountHandle"/>
        public uint id
        {
            get { return m_Id; }
        }

        /// <summary>
        /// If the user is is associated with a user account at the platform level, this is the handle used by the
        /// underlying platform API for the account.
        /// </summary>
        /// <remarks>
        /// Users may be associated with user accounts defined by the platform we are running on. Consoles, for example,
        /// have user account management built into the OS and marketplaces like Steam also have APIs for user management.
        ///
        /// If this property is not <c>null</c>, it is the handle associated with the user at the platform level. This can
        /// be used, for example, to call platform-specific APIs to fetch additional information about the user (such as
        /// user profile images).
        ///
        /// Be aware that there may be multiple InputUsers that have the same platformUserAccountHandle in case the platform
        /// allows different players to log in on the same user account.
        /// </remarks>
        /// <seealso cref="platformUserAccountName"/>
        /// <seealso cref="platformUserAccountId"/>
        /// <seealso cref="InputUserChange.AccountChanged"/>
        public InputUserAccountHandle? platformUserAccountHandle
        {
            get { return s_AllUserData[index].platformUserAccountHandle; }
        }

        /// <summary>
        /// Human-readable name assigned to the user account at the platform level.
        /// </summary>
        /// <remarks>
        /// This property will be <c>null</c> on platforms that do not have user account management. In that case,
        /// <see cref="platformUserAccountHandle"/> will be <c>null</c> as well.
        ///
        /// On platforms such as Xbox, PS4, and Switch, the user name will be the name of the user as logged in on the platform.
        /// </remarks>
        /// <seealso cref="platformUserAccountHandle"/>
        /// <seealso cref="platformUserAccountId"/>
        /// <seealso cref="InputUserChange.AccountChanged"/>
        /// <seealso cref="InputUserChange.AccountNameChanged"/>
        public string platformUserAccountName
        {
            get { return s_AllUserData[index].platformUserAccountName; }
        }

        /// <summary>
        /// Platform-specific user ID that is valid across sessions even if the <see cref="platformUserAccountName"/> of
        /// the user changes.
        /// </summary>
        /// <remarks>
        /// This is only valid if <see cref="platformUserAccountHandle"/> is not null.
        ///
        /// Use this to, for example, associate application settings with the user. For display in UIs, use
        /// <see cref="platformUserAccountName"/> instead.
        /// </remarks>
        /// <seealso cref="platformUserAccountHandle"/>
        /// <seealso cref="platformUserAccountName"/>
        /// <seealso cref="InputUserChange.AccountChanged"/>
        public string platformUserAccountId
        {
            get { return s_AllUserData[index].platformUserAccountId; }
        }

        ////REVIEW: Does it make sense to track used devices separately from paired devices?
        /// <summary>
        /// Devices assigned/paired/linked to the user.
        /// </summary>
        /// <remarks>
        /// It is generally valid for a device to be assigned to multiple users. For example, two users could
        /// both use the local keyboard in a split-keyboard or hot seat setup. However, a platform may restrict this
        /// and mandate that a device never belong to more than one user. This is the case on Xbox and PS4, for
        /// example.
        ///
        /// To associate devices with users, use <see cref="PerformPairingWithDevice"/>. To remove devices, use
        /// <see cref="UnpairDevice"/> or <see cref="UnpairDevicesAndRemoveUser"/>.
        ///
        /// The array will be empty for a user who is currently not paired to any devices.
        ///
        /// If <see cref="actions"/> is set (<see cref="AssociateActionsWithUser(IInputActionCollection)"/>), then
        /// <see cref="IInputActionCollection.devices"/> will be kept synchronized with the devices paired to the user.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="UnpairDevice"/>
        /// <seealso cref="UnpairDevices"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DevicePaired"/>
        /// <seealso cref="InputUserChange.DeviceUnpaired"/>
        public ReadOnlyArray<InputDevice> pairedDevices
        {
            get
            {
                var userIndex = index;
                return new ReadOnlyArray<InputDevice>(s_AllPairedDevices, s_AllUserData[userIndex].deviceStartIndex,
                    s_AllUserData[userIndex].deviceCount);
            }
        }

        /// <summary>
        /// Devices that were removed while they were still paired to the user.
        /// </summary>
        /// <remarks>
        ///
        /// This list is cleared once the user has either regained lost devices or has regained other devices
        /// such that the <see cref="controlScheme"/> is satisfied.
        /// </remarks>
        /// <seealso cref="InputUserChange.DeviceRegained"/>
        /// <seealso cref="InputUserChange.DeviceLost"/>
        public ReadOnlyArray<InputDevice> lostDevices
        {
            get
            {
                var userIndex = index;
                return new ReadOnlyArray<InputDevice>(s_AllLostDevices, s_AllUserData[userIndex].lostDeviceStartIndex,
                    s_AllUserData[userIndex].lostDeviceCount);
            }
        }

        /// <summary>
        /// By default, bindings on the user's <see cref="actions"/> are restricted to the devices paired
        /// to the user (<see cref="pairedDevices"/>). If this property is set to true, then actions are not
        /// restricted to <see cref="pairedDevices"/> but will instead bind to whatever devices in <see cref="InputSystem.devices"/>
        /// match their bindings.
        /// </summary>
        /// <remarks>
        /// This defaults to false.
        ///
        /// Enabling this behavior is useful for implementing automatic control scheme switching that does not
        /// rely on <see cref="onUnpairedDeviceUsed"/>. Instead of listening for unpaired device activity (which
        /// can be costly), one can instead hook into <see cref="InputSystem.onActionChange"/> and use
        /// <see cref="InputActionChange.ActionTriggered"/> to detect when an action was triggered from a device
        /// not currently paired to the user, in which case <see cref="ActivateControlScheme(InputControlScheme)"/>
        /// can be used to automatically switch to a different control scheme.
        /// </remarks>
        public bool bindToAllDevices
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Actions associated with the user.
        /// </summary>
        /// <remarks>
        /// Associating actions with a user will synchronize the actions with the devices paired to the
        /// user. Also, it makes it possible to use support for control scheme activation (<see
        /// cref="ActivateControlScheme(InputControlScheme)"/> and related APIs like <see cref="controlScheme"/>
        /// and <see cref="controlSchemeMatch"/>).
        ///
        /// Note that is generally does not make sense for users to share actions. Instead, each user should
        /// receive a set of actions private to the user. To duplicate an existing set of actions, call
        /// <see cref="InputActionAssetReference.MakePrivateCopyOfActions"/> or <see cref="ScriptableObject.Instantiate(Object)"/>
        /// to duplicate an <see cref="InputActionAsset"/>.
        ///
        /// If <see cref="settings"/> are applied with customized bindings (<see cref="InputUserSettings.customBindings"/>),
        /// these are applied automatically to the actions.
        /// </remarks>
        /// <seealso cref="AssociateActionsWithUser(IInputActionCollection)"/>
        /// <seealso cref="AssociateActionsWithUser(InputActionAssetReference)"/>
        /// <seealso cref="InputActionMap"/>
        /// <seealso cref="InputActionAsset"/>
        /// <seealso cref="InputUserChange.BindingsChanged"/>
        public IInputActionCollection actions
        {
            get { return s_AllUserData[index].actions; }
        }

        /// <summary>
        /// The control scheme currently employed by the user.
        /// </summary>
        /// <remarks>
        /// This is null by default.
        ///
        /// Any time the value of this property changes (whether by <see cref="SetControlScheme"/>
        /// or by automatic switching), a notification is sent on <see cref="onChange"/> with
        /// <see cref="InputUserChange.ControlSchemeChanged"/>.
        ///
        /// Be aware that using control schemes with InputUsers requires <see cref="actions"/> to
        /// be set, i.e. input actions to be associated with the user (<see
        /// cref="AssociateActionsWithUser(IInputActionCollection)"/>).
        /// </remarks>
        /// <seealso cref="ActivateControlScheme(string)"/>
        /// <seealso cref="ActivateControlScheme(InputControlScheme)"/>
        /// <seealso cref="InputUserChange.ControlSchemeChanged"/>
        public InputControlScheme? controlScheme
        {
            get { return s_AllUserData[index].controlScheme; }
        }

        /// <summary>
        /// The result of matching the device requirements given by <see cref="controlScheme"/> against
        /// the devices paired to the user (<see cref="pairedDevices"/>).
        /// </summary>
        /// <remarks>
        /// When devices are paired to or unpaired from a user, as well as when a new control scheme is
        /// activated on a user, this property is updated automatically.
        /// </remarks>
        /// <seealso cref="InputControlScheme.deviceRequirements"/>
        /// <seealso cref="InputControlScheme.PickDevicesFrom{TDevices}"/>
        public InputControlScheme.MatchResult controlSchemeMatch
        {
            get { return s_AllUserData[index].controlSchemeMatch; }
        }

        /// <summary>
        /// Whether the user is missing devices required by the <see cref="controlScheme"/> activated
        /// on the user.
        /// </summary>
        /// <remarks>
        /// This will only take required devices into account. Device requirements marked optional (<see
        /// cref="InputControlScheme.DeviceRequirement.isOptional"/>) will not be considered missing
        /// devices if they cannot be satisfied based on the devices paired to the user.
        /// </remarks>
        /// <seealso cref="InputControlScheme.deviceRequirements"/>
        public bool hasMissingDevices
        {
            get { return s_AllUserData[index].controlSchemeMatch.hasMissingRequiredDevices; }
        }

        /// <summary>
        /// Profile settings associated with the user or <c>null</c> if the user has no associated
        /// profile settings.
        /// </summary>
        public InputUserSettings settings
        {
            //on get, refresh profile (such as custom bindings); keep dirty flag?
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// List of all current users.
        /// </summary>
        /// <remarks>
        /// Use <see cref="PerformPairingWithDevice"/> to add new users and <see cref="UnpairDevicesAndRemoveUser"/> to
        /// remove users.
        ///
        /// Note that this array does not necessarily correspond to the list of users present at the platform level
        /// (e.g. Xbox and PS4). There can be users present at the platform level that are not present in this array
        /// (e.g. because they are not joined to the game) and users can even be present more than once (e.g. if
        /// playing on the user account but as two different players in the game). Also, there can be users in the array
        /// that are not present at the platform level.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        public static ReadOnlyArray<InputUser> all
        {
            get { return new ReadOnlyArray<InputUser>(s_AllUsers, 0, s_AllUserCount); }
        }

        /// <summary>
        /// Event that is triggered when the <see cref="InputUser">user</see> setup in the system
        /// changes.
        /// </summary>
        /// <remarks>
        /// Each notification receives the user that was affected by the change and, in the form of <see cref="InputUserChange"/>,
        /// a description of what has changed about the user. The third parameter may be null but if the change will be related
        /// to an input device, will reference the device involved in the change.
        /// </remarks>
        public static event Action<InputUser, InputUserChange, InputDevice> onChange
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                s_OnChange.AppendWithCapacity(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                ////FIXME: probably don't want to move tail
                s_OnChange.RemoveByMovingTailWithCapacity(value);
            }
        }

        /// <summary>
        /// Event that is triggered when a device is used that is not currently paired to any user.
        /// </summary>
        /// <remarks>
        /// A device is considered "used" when it has magnitude (<see cref="InputControl.EvaluateMagnitude()"/>) greater than zero
        /// on a control that is not noisy (<see cref="InputControl.noisy"/>) and not synthetic (i.e. not a control that is
        /// "made up" like <see cref="Keyboard.anyKey"/>; <see cref="InputControl.synthetic"/>).
        ///
        /// Detecting the use of unpaired devices has a non-zero cost. While multiple levels of tests are applied to try to
        /// cheaply ignore devices that have events sent to them that do not contain user activity, finding out whether
        /// a device had real user activity will eventually require going through the device control by control.
        ///
        /// To enable detection of the use of unpaired devices, set <see cref="listenForUnpairedDeviceActivity"/> to true.
        /// It is disabled by default.
        ///
        /// The control passed to the callback is the first control that activity was detected on. The device can be accessed
        /// through <see cref="InputControl.device"/>. If multiple controls on a device have been actuated, this will simply
        /// be the first control in <see cref="InputDevice.allControls"/> that is actuated. The system does not spend time
        /// trying to find the control that has been actuated the strongest. If desired, this can be done manually in the callback.
        ///
        /// Note that whether the use of unpaired devices leads to them getting paired is under the control of the application.
        /// If the device should be paired, invoke <see cref="PerformPairingWithDevice"/> from the callback. This can also be used
        /// to further filter the activity, e.g. to only join users when pressing a button.
        ///
        /// <example>
        /// <code>
        /// // Activate support for listening to device activity.
        /// InputUser.listenForUnpairedDeviceActivity = true;
        ///
        /// // When a button on an unpaired device is pressed, pair the device to a new
        /// // or existing user.
        /// InputUser.onUnpairedDeviceUsed +=
        ///     usedControl =>
        ///     {
        ///         // Only react to button presses on unpaired devices.
        ///         if (!(usedControl is ButtonControl))
        ///             return;
        ///
        ///         // Pair the device to a user.
        ///         InputUser.PerformPairingWithDevice(usedControl.device);
        ///     };
        ///
        /// InputUser.onChange +=
        ///     (user, change) =>
        ///     {
        ///         ////TODO
        ///     };
        /// </code>
        /// </example>
        ///
        /// Another possible use of the callback is for implementing automatic control scheme switching for a user such that
        /// the user can, for example, switch from keyboard&amp;mouse to gamepad seamlessly by simply picking up the gamepad
        /// and starting to play.
        /// </remarks>
        public static event Action<InputControl> onUnpairedDeviceUsed
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                s_OnUnpairedDeviceUsed.AppendWithCapacity(value);
                HookIntoDeviceChange();
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                ////FIXME: probably don't want to move tail
                s_OnUnpairedDeviceUsed.RemoveByMovingTailWithCapacity(value);
                if (s_OnUnpairedDeviceUsed.length == 0 && !s_ListenForUnpairedDeviceActivity)
                    UnhookFromDeviceChange();
            }
        }

        /// <summary>
        /// Whether to listen for user activity on currently unpaired devices and invoke <see cref="onUnpairedDeviceUsed"/>
        /// if such activity is detected.
        /// </summary>
        /// <remarks>
        /// This is off by default.
        ///
        /// Note that enabling this has a non-zero cost. Whenever the state changes of a device that is not currently paired
        /// to a user, the system has to spend time figuring out whether there was a meaningful change or whether it's just
        /// noise on the device.
        /// </remarks>
        /// <seealso cref="onUnpairedDeviceUsed"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="PerformPairingWithDevice"/>
        public static bool listenForUnpairedDeviceActivity
        {
            get { return s_ListenForUnpairedDeviceActivity; }
            set
            {
                if (value)
                {
                    HookIntoDeviceChange();
                }
                else if (s_OnUnpairedDeviceUsed.length == 0 && s_AllUserCount == 0)
                {
                    UnhookFromDeviceChange();
                }
                s_ListenForUnpairedDeviceActivity = value;
            }
        }

        /// <summary>
        /// Associate an .inputactions asset with the user.
        /// </summary>
        /// <param name="actions"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AssociateActionsWithUser(IInputActionCollection actions)
        {
            var userIndex = index; // Throws if user is invalid.
            if (s_AllUserData[userIndex].actions == actions)
                return;

            // If we already had actions associated, reset the binding mask and device list.
            var oldActions = s_AllUserData[userIndex].actions;
            if (oldActions != null)
            {
                oldActions.devices = null;
                oldActions.bindingMask = null;
            }

            s_AllUserData[userIndex].actions = actions;

            // If we've switched to a different set of actions, synchronize our state.
            if (actions != null)
            {
                actions.devices = pairedDevices;
                if (s_AllUserData[userIndex].controlScheme != null)
                    ActivateControlScheme(s_AllUserData[userIndex].controlScheme.Value);
            }
        }

        public void AssociateActionsWithUser(InputActionAssetReference assetReference)
        {
            if (assetReference == null)
                throw new ArgumentNullException("assetReference");

            AssociateActionsWithUser(assetReference.asset);
        }

        public ControlSchemeChangeSyntax ActivateControlScheme(string schemeName)
        {
            // Look up control scheme by name in actions.
            var scheme = new InputControlScheme();
            if (!string.IsNullOrEmpty(schemeName))
            {
                var userIndex = index; // Throws if user is invalid.

                // Need actions to be available to be able to activate control schemes
                // by name only.
                if (s_AllUserData[userIndex].actions == null)
                    throw new InvalidOperationException(string.Format(
                        "Cannot set control scheme '{0}' by name on user #{1} as not actions have been associated with the user yet (AssociateActionsWithUser)",
                        schemeName, userIndex));

                var controlSchemes = s_AllUserData[userIndex].actions.controlSchemes;
                for (var i = 0; i < controlSchemes.Count; ++i)
                    if (string.Compare(controlSchemes[i].name, schemeName,
                        StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        scheme = controlSchemes[i];
                        break;
                    }

                // Throw if we can't find it.
                if (scheme == default(InputControlScheme))
                    throw new ArgumentException(string.Format("Cannot find control scheme '{0}' in actions '{1}'",
                        schemeName, s_AllUserData[userIndex].actions));
            }

            return ActivateControlScheme(scheme);
        }

        public ControlSchemeChangeSyntax ActivateControlScheme(InputControlScheme scheme)
        {
            var userIndex = index; // Throws if user is invalid.
            var isEmpty = scheme == default(InputControlScheme);

            if (s_AllUserData[userIndex].controlScheme != scheme || (isEmpty && s_AllUserData[userIndex].controlScheme != null))
            {
                if (isEmpty)
                    s_AllUserData[userIndex].controlScheme = null;
                else
                    s_AllUserData[userIndex].controlScheme = scheme;

                if (s_AllUserData[userIndex].actions != null)
                {
                    if (isEmpty)
                    {
                        s_AllUserData[userIndex].actions.bindingMask = null;
                        s_AllUserData[userIndex].controlSchemeMatch.Dispose();
                        s_AllUserData[userIndex].controlSchemeMatch = new InputControlScheme.MatchResult();
                    }
                    else
                    {
                        s_AllUserData[userIndex].actions.bindingMask = new InputBinding {groups = scheme.bindingGroup};
                        UpdateControlSchemeMatch(userIndex);
                    }
                }

                Notify(userIndex, InputUserChange.ControlSchemeChanged, null);
            }

            return new ControlSchemeChangeSyntax {m_UserIndex = userIndex};
        }

        public void PauseHaptics()
        {
            ////TODO
        }

        public void ResumeHaptics()
        {
            ////TODO
        }

        public void ResetHaptics()
        {
            ////TODO
        }

        /// <summary>
        /// Unpair a single device from the user.
        /// </summary>
        /// <param name="device">Device to unpair from the user. If the device is not currently paired to the user,
        /// the method does nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <remarks>
        /// If actions are associated with the user (<see cref="actions"/>), the list of devices used by the
        /// actions (<see cref="IInputActionCollection.devices"/>) is automatically updated.
        ///
        /// If a control scheme is activated on the user (<see cref="controlScheme"/>), <see cref="controlSchemeMatch"/>
        /// is automatically updated.
        ///
        /// Sends <see cref="InputUserChange.DeviceUnpaired"/> through <see cref="onChange"/>.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="UnpairDevices"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DeviceUnpaired"/>
        public void UnpairDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            var userIndex = index; // Throws if user is invalid.

            // Ignore if not currently paired to user.
            if (!pairedDevices.ContainsReference(device))
                return;

            RemoveDeviceFromUser(userIndex, device);
        }

        /// <summary>
        /// Unpair all devices from the user.
        /// </summary>
        /// <remarks>
        /// If actions are associated with the user (<see cref="actions"/>), the list of devices used by the
        /// actions (<see cref="IInputActionCollection.devices"/>) is automatically updated.
        ///
        /// If a control scheme is activated on the user (<see cref="controlScheme"/>), <see cref="controlSchemeMatch"/>
        /// is automatically updated.
        ///
        /// Sends <see cref="InputUserChange.DeviceUnpaired"/> through <see cref="onChange"/> for every device
        /// unpaired from the user.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="UnpairDevice"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DeviceUnpaired"/>
        public void UnpairDevices()
        {
            var userIndex = index; // Throws if user is invalid.

            // Reset device count so it appears all devices are gone from the user. We still want to send
            // notifications one by one, so we can't yet remove the devices from s_AllPairedDevices.
            var deviceCount = s_AllUserData[userIndex].deviceCount;
            var deviceStartIndex = s_AllUserData[userIndex].deviceStartIndex;
            s_AllUserData[userIndex].deviceCount = 0;
            s_AllUserData[deviceStartIndex].deviceStartIndex = 0;

            // Update actions, if necessary.
            var actions = s_AllUserData[userIndex].actions;
            if (actions != null)
                actions.devices = null;

            // Update control scheme, if necessary.
            if (s_AllUserData[userIndex].controlScheme != null)
                UpdateControlSchemeMatch(userIndex);

            // Notify.
            for (var i = 0; i < deviceCount; ++i)
                Notify(userIndex, InputUserChange.DeviceUnpaired, s_AllPairedDevices[deviceStartIndex + i]);

            // Remove.
            ArrayHelpers.EraseSliceWithCapacity(ref s_AllPairedDevices, ref s_AllPairedDeviceCount, deviceStartIndex, deviceCount);
        }

        /// <summary>
        /// Unpair all devices from the user and remove the user.
        /// </summary>
        /// <remarks>
        /// If actions are associated with the user (<see cref="actions"/>), the list of devices used by the
        /// actions (<see cref="IInputActionCollection.devices"/>) is reset as is the binding mask (<see
        /// cref="IInputActionCollection.bindingMask"/>) in case a control scheme is activated on the user.
        ///
        /// Sends <see cref="InputUserChange.DeviceUnpaired"/> through <see cref="onChange"/> for every device
        /// unpaired from the user.
        ///
        /// Sends <see cref="InputUserChange.Removed"/>.
        /// </remarks>
        /// <seealso cref="PerformPairingWithDevice"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="UnpairDevice"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DeviceUnpaired"/>
        /// <seealso cref="InputUserChange.Removed"/>
        public void UnpairDevicesAndRemoveUser()
        {
            UnpairDevices();

            var userIndex = index;
            RemoveUser(userIndex);
        }

        /// <summary>
        /// Return a list of all currently added devices that are not paired to any user.
        /// </summary>
        /// <returns>A (possibly empty) list of devices that are currently not paired to a user.</returns>
        /// <remarks>
        /// The resulting list uses <see cref="Allocator.Temp"> temporary, unmanaged memory</see>. If not disposed of
        /// explicitly, the list will automatically be deallocated at the end of the frame and will become unusable.
        /// </remarks>
        /// <seealso cref="InputSystem.devices"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="PerformPairingWithDevice"/>
        public static InputControlList<InputDevice> GetUnpairedInputDevices()
        {
            var list = new InputControlList<InputDevice>(Allocator.Temp);
            GetUnpairedInputDevices(ref list);
            return list;
        }

        /// <summary>
        /// Add all currently added devices that are not paired to any user to <paramref name="list"/>.
        /// </summary>
        /// <param name="list">List to add the devices to. Devices will be added to the end.</param>
        /// <returns>Number of devices added to <paramref name="list"/>.</returns>
        /// <seealso cref="InputSystem.devices"/>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="PerformPairingWithDevice"/>
        public static int GetUnpairedInputDevices(ref InputControlList<InputDevice> list)
        {
            var countBefore = list.Count;
            foreach (var device in InputSystem.devices)
            {
                // If it's in s_AllPairedDevices, there is *some* user that is using the device.
                // We don't care which one it is here.
                if (ArrayHelpers.ContainsReferenceTo(s_AllPairedDevices, s_AllPairedDeviceCount, device))
                    continue;

                list.Add(device);
            }

            return list.Count - countBefore;
        }

        /// <summary>
        /// Find the user (if any) that <paramref name="device"/> is currently paired to.
        /// </summary>
        /// <param name="device">An input device.</param>
        /// <returns>The user that <paramref name="device"/> is currently paired to or <c>null</c> if the device
        /// is not currently paired to an user.</returns>
        /// <remarks>
        /// Note that multiple users may be paired to the same device. If that is the case for <paramref name="device"/>,
        /// the method will return one of the users with no guarantee which one it is.
        ///
        /// To find all users paired to a device requires manually going through the list of users and their paired
        /// devices.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="PerformPairingWithDevice"/>
        public static InputUser? FindUserPairedToDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            var userIndex = TryFindUserIndex(device);
            if (userIndex == -1)
                return null;

            return s_AllUsers[userIndex];
        }

        public static InputUser? FindUserByAccount(InputUserAccountHandle platformUserAccountHandle)
        {
            if (platformUserAccountHandle == default(InputUserAccountHandle))
                throw new ArgumentException("Empty platform user account handle", "platformUserAccountHandle");

            var userIndex = TryFindUserIndex(platformUserAccountHandle);
            if (userIndex == -1)
                return null;

            return s_AllUsers[userIndex];
        }

        ////REVIEW: allow re-adding a user through this method?
        /// <summary>
        /// Pair the given device to a user.
        /// </summary>
        /// <param name="device">Device to pair to a user.</param>
        /// <param name="user">Optional parameter. If given, instead of creating a new user to pair the device
        /// to, the device is paired to the given user.</param>
        /// <param name="options">Optional set of options to modify pairing behavior.</param>
        /// <remarks>
        /// By default, a new user is created and <paramref name="device"/> is added <see cref="pairedDevices"/>
        /// of the user and <see cref="InputUserChange.DevicePaired"/> is sent on <see cref="onChange"/>.
        ///
        /// If a valid user is supplied to <paramref name="user"/>, the device is paired to the given user instead
        /// of creating a new user. By default, the device is added to the list of already paired devices for the user.
        /// This can be changed by using <see cref="InputUserPairingOptions.UnpairCurrentDevicesFromUser"/> which causes
        /// devices currently paired to the user to first be unpaired.
        ///
        /// The method will not prevent pairing of the same device to multiple users.
        ///
        /// Note that if the user has an associated set of actions (<see cref="actions"/>), the list of devices on the
        /// actions (<see cref="IInputActionCollection.devices"/>) will automatically be updated meaning that the newly
        /// paired devices will automatically reflect in the set of devices available to the user's actions. If the
        /// user has a control scheme that is currently activated (<see cref="controlScheme"/>), then <see cref="controlSchemeMatch"/>
        /// will also automatically update to reflect the matching of devices to the control scheme's device requirements.
        ///
        /// If the given device is associated with a user account at the platform level (queried through
        /// <see cref="QueryPairedUserAccountCommand"/>), the user's platform account details (<see cref="platformUserAccountHandle"/>,
        /// <see cref="platformUserAccountName"/>, and <see cref="platformUserAccountId"/>) are updated accordingly. In this case,
        /// <see cref="InputUserChange.AccountChanged"/> or <see cref="InputUserChange.AccountNameChanged"/> may be signalled.
        /// through <see cref="onChange"/>.
        ///
        /// If the given device is not associated with a user account at the platform level, but it does
        /// respond to <see cref="InitiateUserAccountPairingCommand"/>, then the device is NOT immediately paired
        /// to the user. Instead, pairing is deferred to until after an account selection has been made by the user.
        /// In this case, <see cref="InputUserChange.AccountSelectionInProgress"/> will be signalled through <see cref="onChange"/>
        /// and <see cref="InputUserChange.AccountChanged"/> will be signalled once the user has selected an account or
        /// <see cref="InputUserChange.AccountSelectionCancelled"/> will be signalled if the user cancels account
        /// selection. The device will be paired to the user once account selection is complete.
        ///
        /// This behavior is most useful on Xbox and Switch to require the user to choose which account to play with. Note that
        /// if the device is already associated with a user account, account selection will not be initiated. However,
        /// it can be explicitly forced to be performed by using <see
        /// cref="InputUserPairingOptions.ForcePlatformUserAccountSelection"/>. This is useful,
        /// for example, to allow the user to explicitly switch accounts.
        ///
        /// On Xbox and Switch, to permit playing even on devices that do not currently have an associated user account,
        /// use <see cref="InputUserPairingOptions.ForceNoPlatformUserAccountSelection"/>.
        ///
        /// On PS4, devices will always have associated user accounts meaning that the returned InputUser will always
        /// have updated platform account details.
        ///
        /// Note that user account queries and initiating account selection can be intercepted by the application. For
        /// example, on Switch where user account pairing is not stored at the platform level, one can, for example, both
        /// implement custom pairing logic as well as a custom account selection UI by intercepting <see cref="QueryPairedUserAccountCommand"/>
        /// and <seealso cref="InitiateUserAccountPairingCommand"/>.
        ///
        /// <example>
        /// <code>
        /// InputSystem.onDeviceCommand +=
        ///     (device, commandPtr, runtime) =>
        ///     {
        ///         // Dealing with InputDeviceCommands requires handling raw pointers.
        ///         unsafe
        ///         {
        ///             // We're only looking for QueryPairedUserAccountCommand and InitiateUserAccountPairingCommand here.
        ///             if (commandPtr->type != QueryPairedUserAccountCommand.Type && commandPtr->type != InitiateUserAccountPairingCommand)
        ///                 return null; // Command not handled.
        ///
        ///             // Check if device is the one your interested in. As an example, we look for Switch gamepads
        ///             // here.
        ///             if (!(device is Npad))
        ///                 return null; // Command not handled.
        ///
        ///             // If it's a QueryPairedUserAccountCommand, see if we have a user ID to use with the Npad
        ///             // based on last time the application ran.
        ///             if (commandPtr->type == QueryPairedUserAccountCommand.Type)
        ///             {
        ///                 ////TODO
        ///         }
        ///     };
        /// </code>
        /// </example>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Pair device to new user.
        /// var user = InputUser.PerformPairingWithDevice(wand1);
        ///
        /// // Pair another device to the same user.
        /// InputUser.PerformPairingWithDevice(wand2, user: user);
        /// </code>
        /// </example>
        /// <seealso cref="pairedDevices"/>
        /// <seealso cref="UnpairDevice"/>
        /// <seealso cref="UnpairDevices"/>
        /// <seealso cref="UnpairDevicesAndRemoveUser"/>
        /// <seealso cref="InputUserChange.DevicePaired"/>
        public static InputUser PerformPairingWithDevice(InputDevice device,
            InputUser user = default(InputUser),
            InputUserPairingOptions options = InputUserPairingOptions.None)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (user != default(InputUser) && !user.valid)
                throw new ArgumentException("Invalid user", "user");

            // Create new user, if needed.
            int userIndex;
            if (user == default(InputUser))
            {
                userIndex = AddUser();
            }
            else
            {
                // We have an existing user.
                userIndex = user.index;

                // See if we're supposed to clear out the user's currently paired devices first.
                if ((options & InputUserPairingOptions.UnpairCurrentDevicesFromUser) != 0)
                    user.UnpairDevices();

                // Ignore call if device is already paired to user.
                if (user.pairedDevices.ContainsReference(device))
                {
                    // Still might have to initiate user account selection.
                    if ((options & InputUserPairingOptions.ForcePlatformUserAccountSelection) != 0)
                        InitiateUserAccountSelection(userIndex, device, options);
                    return user;
                }
            }

            // Handle the user account side of pairing.
            var accountSelectionInProgress = InitiateUserAccountSelection(userIndex, device, options);

            // Except if we have initiate user account selection, pair the device to
            // to the user now.
            if (!accountSelectionInProgress)
                AddDeviceToUser(userIndex, device);

            return s_AllUsers[userIndex];
        }

        private static bool InitiateUserAccountSelection(int userIndex, InputDevice device,
            InputUserPairingOptions options)
        {
            // See if there's a platform user account we can get from the device.
            // NOTE: We don't query the current user account if the caller has opted to force account selection.
            var queryUserAccountResult =
                (options & InputUserPairingOptions.ForcePlatformUserAccountSelection) == 0
                ? UpdatePlatformUserAccount(userIndex, device)
                : 0;

            ////REVIEW: what should we do if there already is an account selection in progress? InvalidOperationException?
            // If the device supports user account selection but we didn't get one,
            // try to initiate account selection.
            if ((options & InputUserPairingOptions.ForcePlatformUserAccountSelection) != 0 ||
                (queryUserAccountResult != QueryPairedUserAccountCommand.Result.NotSupported &&
                 (queryUserAccountResult & QueryPairedUserAccountCommand.Result.DevicePairedToUserAccount) == 0 &&
                 (options & InputUserPairingOptions.ForceNoPlatformUserAccountSelection) == 0))
            {
                if (InitiateUserAccountSelectionAtPlatformLevel(device))
                {
                    s_AllUserData[userIndex].flags |= UserFlags.UserAccountSelectionInProgress;
                    s_OngoingAccountSelections.Append(
                        new OngoingAccountSelection
                        {
                            device = device,
                            userId = s_AllUsers[userIndex].id,
                        });

                    // Make sure we receive a notification for the configuration event.
                    HookIntoDeviceChange();

                    // Tell listeners that we started an account selection.
                    Notify(userIndex, InputUserChange.AccountSelectionInProgress, device);

                    return true;
                }
            }

            return false;
        }

        public bool Equals(InputUser other)
        {
            return m_Id == other.m_Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputUser && Equals((InputUser)obj);
        }

        public override int GetHashCode()
        {
            return (int)m_Id;
        }

        public static bool operator==(InputUser left, InputUser right)
        {
            return left.m_Id == right.m_Id;
        }

        public static bool operator!=(InputUser left, InputUser right)
        {
            return left.m_Id != right.m_Id;
        }

        /// <summary>
        /// Add a new user.
        /// </summary>
        /// <returns>Index of the newly created user.</returns>
        /// <remarks>
        /// Adding a user sends a notification with <see cref="InputUserChange.Added"/> through <see cref="onChange"/>.
        ///
        /// The user will start out with no devices and no actions assigned.
        ///
        /// The user is added to <see cref="all"/>.
        /// </remarks>
        private static int AddUser()
        {
            var id = ++s_LastUserId;

            // Add to list.
            var userCount = s_AllUserCount;
            ArrayHelpers.AppendWithCapacity(ref s_AllUsers, ref userCount, new InputUser {m_Id = id});
            var userIndex = ArrayHelpers.AppendWithCapacity(ref s_AllUserData, ref s_AllUserCount, new UserData());

            // Send notification.
            Notify(userIndex, InputUserChange.Added, null);

            return userIndex;
        }

        /// <summary>
        /// Remove an active user.
        /// </summary>
        /// <param name="userIndex">Index of active user.</param>
        /// <remarks>
        /// Removing a user also unassigns all currently assigned devices from the user. On completion of this
        /// method, <see cref="pairedDevices"/> of <paramref name="user"/> will be empty.
        /// </remarks>
        private static void RemoveUser(int userIndex)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_AllUserCount);
            Debug.Assert(s_AllUserData[userIndex].deviceCount == 0, "User must not have paired devices still");

            // Reset data from control scheme.
            if (s_AllUserData[userIndex].controlScheme != null)
            {
                if (s_AllUserData[userIndex].actions != null)
                    s_AllUserData[userIndex].actions.bindingMask = null;
                s_AllUserData[userIndex].controlSchemeMatch.Dispose();
            }

            // Remove lost devices.
            var lostDeviceCount = s_AllUserData[userIndex].lostDeviceCount;
            if (lostDeviceCount > 0)
            {
                ArrayHelpers.EraseSliceWithCapacity(ref s_AllLostDevices, ref s_AllLostDeviceCount,
                    s_AllUserData[userIndex].lostDeviceStartIndex, lostDeviceCount);
            }

            // Remove account selections that are in progress.
            for (var i = 0; i < s_OngoingAccountSelections.length; ++i)
            {
                if (s_OngoingAccountSelections[i].userId != s_AllUsers[userIndex].id)
                    continue;

                s_OngoingAccountSelections.RemoveAtByMovingTailWithCapacity(i);
                --i;
            }

            // Send notification (do before we actually remove the user).
            Notify(userIndex, InputUserChange.Removed, null);

            // Remove.
            var userCount = s_AllUserCount;
            ArrayHelpers.EraseAtWithCapacity(ref s_AllUsers, ref userCount, userIndex);
            ArrayHelpers.EraseAtWithCapacity(ref s_AllUserData, ref s_AllUserCount, userIndex);

            // Remove our hook if we no longer need it.
            if (s_AllUserCount == 0 && !s_ListenForUnpairedDeviceActivity)
                UnhookFromDeviceChange();
        }

        private static void Notify(int userIndex, InputUserChange change, InputDevice device)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_AllUserCount);

            for (var i = 0; i < s_OnChange.length; ++i)
                s_OnChange[i](s_AllUsers[userIndex], change, device);
        }

        private static int TryFindUserIndex(uint userId)
        {
            Debug.Assert(userId != kInvalidId);

            for (var i = 0; i < s_AllUserCount; ++i)
            {
                if (s_AllUsers[i].m_Id == userId)
                    return i;
            }
            return -1;
        }

        private static int TryFindUserIndex(InputUserAccountHandle platformHandle)
        {
            Debug.Assert(platformHandle != new InputUserAccountHandle());

            for (var i = 0; i < s_AllUserCount; ++i)
            {
                if (s_AllUserData[i].platformUserAccountHandle == platformHandle)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Find the user (if any) that is currently assigned the given <paramref name="device"/>.
        /// </summary>
        /// <param name="device">An input device that has been added to the system.</param>
        /// <returns>Index of the user that has <paramref name="device"/> among its <see cref="pairedDevices"/> or -1 if
        /// no user is currently assigned the given device.</returns>
        private static int TryFindUserIndex(InputDevice device)
        {
            Debug.Assert(device != null);

            var indexOfDevice = ArrayHelpers.IndexOfReference(s_AllPairedDevices, s_AllPairedDeviceCount, device);
            if (indexOfDevice == -1)
                return -1;

            for (var i = 0; i < s_AllUserCount; ++i)
            {
                var startIndex = s_AllUserData[i].deviceStartIndex;
                if (startIndex <= indexOfDevice && indexOfDevice < startIndex + s_AllUserData[i].deviceCount)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Add the given device to the user as either a lost device or a paired device.
        /// </summary>
        /// <param name="userIndex"></param>
        /// <param name="device"></param>
        /// <param name="asLostDevice"></param>
        private static void AddDeviceToUser(int userIndex, InputDevice device, bool asLostDevice = false, bool dontUpdateControlScheme = false)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_AllUserCount);
            Debug.Assert(device != null);
            if (asLostDevice)
                Debug.Assert(!s_AllUsers[userIndex].lostDevices.ContainsReference(device));
            else
                Debug.Assert(!s_AllUsers[userIndex].pairedDevices.ContainsReference(device));

            var deviceCount = asLostDevice
                ? s_AllUserData[userIndex].lostDeviceCount
                : s_AllUserData[userIndex].deviceCount;
            var deviceStartIndex = asLostDevice
                ? s_AllUserData[userIndex].lostDeviceStartIndex
                : s_AllUserData[userIndex].deviceStartIndex;

            // Move our devices to end of array.
            if (deviceCount > 0)
            {
                ArrayHelpers.MoveSlice(asLostDevice ? s_AllLostDevices : s_AllPairedDevices, deviceStartIndex,
                    asLostDevice ? s_AllLostDeviceCount - deviceCount : s_AllPairedDeviceCount - deviceCount,
                    deviceCount);

                // Adjust users that have been impacted by the change.
                for (var i = 0; i < s_AllUserCount; ++i)
                {
                    if (i == userIndex)
                        continue;

                    if ((asLostDevice ? s_AllUserData[i].lostDeviceStartIndex : s_AllUserData[i].deviceStartIndex) <= deviceStartIndex)
                        continue;

                    if (asLostDevice)
                        s_AllUserData[i].lostDeviceStartIndex -= deviceCount;
                    else
                        s_AllUserData[i].deviceStartIndex -= deviceCount;
                }
            }

            // Append to array.
            if (asLostDevice)
            {
                s_AllUserData[userIndex].lostDeviceStartIndex = s_AllLostDeviceCount - deviceCount;
                ArrayHelpers.AppendWithCapacity(ref s_AllLostDevices, ref s_AllLostDeviceCount, device);
                ++s_AllUserData[userIndex].lostDeviceCount;
            }
            else
            {
                s_AllUserData[userIndex].deviceStartIndex = s_AllPairedDeviceCount - deviceCount;
                ArrayHelpers.AppendWithCapacity(ref s_AllPairedDevices, ref s_AllPairedDeviceCount, device);
                ++s_AllUserData[userIndex].deviceCount;

                // If the user has actions, sync the devices on them with what we have now.
                var actions = s_AllUserData[userIndex].actions;
                if (actions != null)
                {
                    actions.devices = s_AllUsers[userIndex].pairedDevices;

                    // Also, if we have a control scheme, update the matching of device requirements
                    // against the device we now have.
                    if (!dontUpdateControlScheme && s_AllUserData[userIndex].controlScheme != null)
                        UpdateControlSchemeMatch(userIndex);
                }
            }

            // Make sure we get OnDeviceChange notifications.
            HookIntoDeviceChange();

            // Let listeners know.
            Notify(userIndex, asLostDevice ? InputUserChange.DeviceLost : InputUserChange.DevicePaired, device);
        }

        private static void RemoveDeviceFromUser(int userIndex, InputDevice device, bool asLostDevice = false)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_AllUserCount);
            Debug.Assert(device != null);

            var deviceIndex = asLostDevice
                ? ArrayHelpers.IndexOfReference(s_AllLostDevices, s_AllLostDeviceCount, device)
                : ArrayHelpers.IndexOfReference(s_AllPairedDevices, s_AllPairedDeviceCount, device);
            Debug.Assert(deviceIndex != -1);
            if (deviceIndex == -1)
            {
                // Device not in list. Ignore.
                return;
            }

            if (asLostDevice)
            {
                ArrayHelpers.EraseAtWithCapacity(ref s_AllLostDevices, ref s_AllLostDeviceCount, deviceIndex);
                --s_AllUserData[userIndex].lostDeviceCount;
            }
            else
            {
                ArrayHelpers.EraseAtWithCapacity(ref s_AllPairedDevices, ref s_AllPairedDeviceCount, deviceIndex);
                --s_AllUserData[userIndex].deviceCount;
            }

            // Adjust indices of other users.
            for (var i = 0; i < s_AllUserCount; ++i)
            {
                if ((asLostDevice ? s_AllUserData[i].lostDeviceStartIndex : s_AllUserData[i].deviceStartIndex) <= deviceIndex)
                    continue;

                if (asLostDevice)
                    --s_AllUserData[i].lostDeviceStartIndex;
                else
                    --s_AllUserData[i].deviceStartIndex;
            }

            if (!asLostDevice)
            {
                // Remove any ongoing account selections for the user on the given device.
                for (var i = 0; i < s_OngoingAccountSelections.length; ++i)
                {
                    if (s_OngoingAccountSelections[i].userId != s_AllUsers[userIndex].id ||
                        s_OngoingAccountSelections[i].device != device)
                        continue;

                    s_OngoingAccountSelections.RemoveAtByMovingTailWithCapacity(i);
                    --i;
                }

                // If the user has actions, sync the devices on them with what we have now.
                var actions = s_AllUserData[userIndex].actions;
                if (actions != null)
                {
                    actions.devices = s_AllUsers[userIndex].pairedDevices;

                    if (s_AllUsers[userIndex].controlScheme != null)
                        UpdateControlSchemeMatch(userIndex);
                }

                // Notify listeners.
                Notify(userIndex, InputUserChange.DeviceUnpaired, device);
            }
        }

        private static void UpdateControlSchemeMatch(int userIndex, bool autoPairMissing = false)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_AllUserCount);

            // Nothing to do if we don't have a control scheme.
            if (s_AllUserData[userIndex].controlScheme == null)
                return;

            // Get rid of last match result and start new match.
            s_AllUserData[userIndex].controlSchemeMatch.Dispose();
            var matchResult = new InputControlScheme.MatchResult();
            try
            {
                // Match the control scheme's requirements against the devices paired to the user.
                var scheme = s_AllUserData[userIndex].controlScheme.Value;
                if (scheme.deviceRequirements.Count > 0)
                {
                    var availableDevices = new InputControlList<InputDevice>(Allocator.Temp);
                    try
                    {
                        // Add devices already paired to user.
                        availableDevices.AddSlice(s_AllUsers[userIndex].pairedDevices);

                        // If we're supposed to grab whatever additional devices we need from what's
                        // available, add all unpaired devices to the list.
                        // NOTE: These devices go *after* the devices already paired (if any) meaning that
                        //       the control scheme matching will grab already paired devices *first*.
                        if (autoPairMissing)
                        {
                            var startIndex = availableDevices.Count;
                            var count = GetUnpairedInputDevices(ref availableDevices);

                            // We want to favor devices that are already assigned to the same platform user account.
                            // Sort the unpaired devices we've added to the list such that the ones belonging to the
                            // same user account come first.
                            if (s_AllUserData[userIndex].platformUserAccountHandle != null)
                                availableDevices.Sort(startIndex, count,
                                    new CompareDevicesByUserAccount
                                    {
                                        platformUserAccountHandle = s_AllUserData[userIndex].platformUserAccountHandle.Value
                                    });
                        }

                        matchResult = scheme.PickDevicesFrom(availableDevices);
                        if (matchResult.isSuccessfulMatch)
                        {
                            // If we had lost some devices, flush the list. We haven't regained the device
                            // but we're no longer missing devices to play.
                            if (s_AllUserData[userIndex].lostDeviceCount > 0)
                                ArrayHelpers.EraseSliceWithCapacity(ref s_AllLostDevices, ref s_AllLostDeviceCount,
                                    s_AllUserData[userIndex].lostDeviceStartIndex,
                                    s_AllUserData[userIndex].lostDeviceCount);

                            // Control scheme is satisfied with the devices we have available.
                            // If we may have grabbed as of yet unpaired devices, go and pair them to the user.
                            if (autoPairMissing)
                            {
                                // Update match result on user before potentially invoking callbacks.
                                s_AllUserData[userIndex].controlSchemeMatch = matchResult;

                                foreach (var device in matchResult.devices)
                                {
                                    // Skip if already paired to user.
                                    if (s_AllUsers[userIndex].pairedDevices.ContainsReference(device))
                                        continue;

                                    AddDeviceToUser(userIndex, device, dontUpdateControlScheme: true);
                                }
                            }
                        }
                    }
                    finally
                    {
                        availableDevices.Dispose();
                    }
                }

                s_AllUserData[userIndex].controlSchemeMatch = matchResult;
            }
            catch (Exception)
            {
                // If we had an exception and are bailing out, make sure we aren't leaking native memory
                // we allocated.
                matchResult.Dispose();
                throw;
            }
        }

        private static QueryPairedUserAccountCommand.Result UpdatePlatformUserAccount(int userIndex, InputDevice device)
        {
            Debug.Assert(userIndex >= 0 && userIndex < s_AllUserCount);

            InputUserAccountHandle? platformUserAccountHandle;
            string platformUserAccountName;
            string platformUserAccountId;

            // Fetch account details from backend.
            var queryResult = QueryPairedPlatformUserAccount(device, out platformUserAccountHandle,
                out platformUserAccountName, out platformUserAccountId);

            // Nothing much to do if not supported by device.
            if ((queryResult & QueryPairedUserAccountCommand.Result.NotSupported) != 0)
            {
                // Check if there's an account selection in progress. There shouldn't be as it's
                // weird for the device to no signal it does not support querying user account, but
                // just to be safe, we check.
                if ((s_AllUserData[userIndex].flags & UserFlags.UserAccountSelectionInProgress) != 0)
                    Notify(userIndex, InputUserChange.AccountSelectionCancelled, null);

                s_AllUserData[userIndex].platformUserAccountHandle = null;
                s_AllUserData[userIndex].platformUserAccountName = null;
                s_AllUserData[userIndex].platformUserAccountId = null;

                return queryResult;
            }

            // Check if there's an account selection that we have initiated.
            if ((s_AllUserData[userIndex].flags & UserFlags.UserAccountSelectionInProgress) != 0)
            {
                // Yes, there is. See if it is complete.

                if ((queryResult & QueryPairedUserAccountCommand.Result.UserAccountSelectionInProgress) != 0)
                {
                    // No, still in progress.
                }
                else if ((queryResult & QueryPairedUserAccountCommand.Result.UserAccountSelectionCancelled) != 0)
                {
                    // Got cancelled.
                    Notify(userIndex, InputUserChange.AccountSelectionCancelled, device);
                }
                else
                {
                    // Yes, it is complete.
                    s_AllUserData[userIndex].flags &= ~UserFlags.UserAccountSelectionInProgress;

                    s_AllUserData[userIndex].platformUserAccountHandle = platformUserAccountHandle;
                    s_AllUserData[userIndex].platformUserAccountName = platformUserAccountName;
                    s_AllUserData[userIndex].platformUserAccountId = platformUserAccountId;

                    Notify(userIndex, InputUserChange.AccountSelectionComplete, device);
                }
            }
            // Check if user account details have changed.
            else if (s_AllUserData[userIndex].platformUserAccountHandle != platformUserAccountHandle ||
                     s_AllUserData[userIndex].platformUserAccountId != platformUserAccountId)
            {
                s_AllUserData[userIndex].platformUserAccountHandle = platformUserAccountHandle;
                s_AllUserData[userIndex].platformUserAccountName = platformUserAccountName;
                s_AllUserData[userIndex].platformUserAccountId = platformUserAccountId;

                Notify(userIndex, InputUserChange.AccountChanged, device);
            }
            else if (s_AllUserData[userIndex].platformUserAccountName != platformUserAccountName)
            {
                Notify(userIndex, InputUserChange.AccountNameChanged, device);
            }

            return queryResult;
        }

        /// <summary>
        /// If the given device is paired to a user account at the platform level, return the platform user
        /// account details.
        /// </summary>
        /// <param name="device">Any input device.</param>
        /// <param name="platformAccountHandle">Receives the platform user account handle or null.</param>
        /// <param name="platformAccountName">Receives the platform user account name or null.</param>
        /// <param name="platformAccountId">Receives the platform user account ID or null.</param>
        /// <returns>True if the device is paired to a user account, false otherwise.</returns>
        /// <remarks>
        /// Sends <see cref="QueryPairedUserAccountCommand"/> to the device.
        /// </remarks>
        /// <seealso cref="QueryPairedUserAccountCommand.handle"/>
        /// <seealso cref="QueryPairedUserAccountCommand.name"/>
        /// <seealso cref="QueryPairedUserAccountCommand.id"/>
        private static QueryPairedUserAccountCommand.Result QueryPairedPlatformUserAccount(InputDevice device,
            out InputUserAccountHandle? platformAccountHandle, out string platformAccountName, out string platformAccountId)
        {
            Debug.Assert(device != null);

            // Query user account info from backend.
            var queryPairedUser = QueryPairedUserAccountCommand.Create();
            var result = (QueryPairedUserAccountCommand.Result)device.ExecuteCommand(ref queryPairedUser);
            if (result == QueryPairedUserAccountCommand.Result.NotSupported)
            {
                // Not currently paired to user account in backend.
                platformAccountHandle = null;
                platformAccountName = null;
                platformAccountId = null;
                return QueryPairedUserAccountCommand.Result.NotSupported;
            }

            // Success. There is a user account currently paired to the device and we now have the
            // platform's user account details.

            if ((result & QueryPairedUserAccountCommand.Result.DevicePairedToUserAccount) != 0)
            {
                platformAccountHandle =
                    new InputUserAccountHandle(device.description.interfaceName ?? "<Unknown>", queryPairedUser.handle);
                platformAccountName = queryPairedUser.name;
                platformAccountId = queryPairedUser.id;
            }
            else
            {
                // The device supports QueryPairedUserAccountCommand but reports that the
                // device is not currently paired to a user.
                //
                // NOTE: On Switch, where the system itself does not store account<->pairing, we will always
                //       end up here until we've initiated an account selection through the backend itself.
                platformAccountHandle = null;
                platformAccountName = null;
                platformAccountId = null;
            }

            return result;
        }

        /// <summary>
        /// Try to initiate user account pairing for the given device at the platform level.
        /// </summary>
        /// <param name="device"></param>
        /// <returns>True if the device accepted the request and an account picker has been raised.</returns>
        /// <remarks>
        /// Sends <see cref="InitiateUserAccountPairingCommand"/> to the device.
        /// </remarks>
        private static bool InitiateUserAccountSelectionAtPlatformLevel(InputDevice device)
        {
            Debug.Assert(device != null);

            var initiateUserPairing = InitiateUserAccountPairingCommand.Create();
            var initiatePairingResult = device.ExecuteCommand(ref initiateUserPairing);
            if (initiatePairingResult == (long)InitiateUserAccountPairingCommand.Result.ErrorAlreadyInProgress)
                throw new InvalidOperationException("User pairing already in progress");

            return initiatePairingResult == (long)InitiateUserAccountPairingCommand.Result.SuccessfullyInitiated;
        }

        /// <summary>
        /// Invoked in response to <see cref="InputSystem.onDeviceChange"/>.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="change"></param>
        /// <remarks>
        /// We monitor the device setup in the system for activity that impacts the user setup.
        /// </remarks>
        internal static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                // Existing device removed. May mean a user has lost a device due to the battery running
                // out or the device being unplugged.
                // NOTE: We ignore Disconnected here. Removed is what gets sent whenever a device is taken off of
                //       InputSystem.devices -- which is what we're interested in here.
                case InputDeviceChange.Removed:
                {
                    // Could have been removed from multiple users. Repeatedly search in s_AllPairedDevices
                    // until we can't find the device anymore.
                    var deviceIndex = ArrayHelpers.IndexOfReference(s_AllPairedDevices, device);
                    while (deviceIndex != -1)
                    {
                        // Find user. Must be there as we found the device in s_AllPairedDevices.
                        var userIndex = -1;
                        for (var i = 0; i < s_AllUserCount; ++i)
                        {
                            var deviceStartIndex = s_AllUserData[i].deviceStartIndex;
                            if (deviceStartIndex <= deviceIndex && deviceIndex < deviceStartIndex + s_AllUserData[i].deviceCount)
                            {
                                userIndex = i;
                                break;
                            }
                        }

                        // Add device to list of lost devices.
                        // NOTE: This will also send a DeviceLost notification.
                        // NOTE: Temporarily the device is on both lists.
                        AddDeviceToUser(userIndex, device, asLostDevice: true);

                        // Remove it from the user.
                        RemoveDeviceFromUser(userIndex, device);

                        // Search for another user paired to the same device.
                        deviceIndex = ArrayHelpers.IndexOfReference(s_AllPairedDevices, device, deviceIndex + 1);
                    }
                    break;
                }

                // New device was added. See if it was a device we previously lost on a user.
                case InputDeviceChange.Added:
                {
                    // Could be a previously lost device. Could affect multiple users. Repeatedly search in
                    // s_AllLostDevices until we can't find the device anymore.
                    var deviceIndex = ArrayHelpers.IndexOfReference(s_AllLostDevices, device);
                    while (deviceIndex != -1)
                    {
                        // Find user. Must be there as we found the device in s_AllLostDevices.
                        var userIndex = -1;
                        for (var i = 0; i < s_AllUserCount; ++i)
                        {
                            var deviceStartIndex = s_AllUserData[i].lostDeviceStartIndex;
                            if (deviceStartIndex <= deviceIndex && deviceIndex < deviceStartIndex + s_AllUserData[i].lostDeviceCount)
                            {
                                userIndex = i;
                                break;
                            }
                        }

                        // Remove from list of lost devices. No notification.
                        RemoveDeviceFromUser(userIndex, device, asLostDevice: true);

                        // Notify.
                        Notify(userIndex, InputUserChange.DeviceRegained, device);

                        // Add back as normally paired device.
                        AddDeviceToUser(userIndex, device);

                        // Search for another user who had lost the same device.
                        deviceIndex = ArrayHelpers.IndexOfReference(s_AllLostDevices, device, deviceIndex + 1);
                    }
                    break;
                }

                // Device had activity.
                case InputDeviceChange.StateChanged:
                {
                    // Ignore if we're not listening that kind of activity ATM.
                    if (!s_ListenForUnpairedDeviceActivity)
                        return;

                    // See if it's a device not belonging to any user.
                    if (ArrayHelpers.ContainsReferenceTo(s_AllPairedDevices, s_AllPairedDeviceCount, device))
                    {
                        // No, it's a device already paired to a player so do nothing.
                        return;
                    }

                    Profiler.BeginSample("InputCheckForUnpairedDeviceActivity");

                    ////TODO: allow filtering (e.g. by device requirements on user actions)

                    unsafe
                    {
                        // Yes, it is so let's find out whether there was actual user activity
                        // on the device. As a first level, we run the noise mask over the state
                        // while concurrently comparing whatever bits make it through to the default
                        // state buffer.
                        if (device.CheckStateIsAtDefaultIgnoringNoise())
                            return; // No activity at all.

                        ////REVIEW: should we restrict this to just leaf controls?
                        // Go through controls and for any one that isn't noisy or synthetic, find out
                        // if we have a magnitude greater than zero.
                        var controls = device.allControls;
                        for (var i = 0; i < controls.Count; ++i)
                        {
                            var control = controls[i];
                            if (control.noisy || control.synthetic)
                                continue;

                            // Check for default state. Cheaper check than magnitude evaluation
                            // which may involve several virtual method calls.
                            if (control.CheckStateIsAtDefault())
                                continue;

                            // Ending up here is costly. We now do per-control work that may involve
                            // walking all over the place in the InputControl machinery.
                            var magnitude = control.EvaluateMagnitude();
                            if (magnitude > 0)
                            {
                                // Yes, something was actuated on the device.
                                for (var n = 0; n < s_OnUnpairedDeviceUsed.length; ++n)
                                    s_OnUnpairedDeviceUsed[n](control);
                                break;
                            }
                        }
                    }

                    Profiler.EndSample();

                    break;
                }

                // Device had its configuration changed which may mean we have a different user account paired
                // to the device now.
                case InputDeviceChange.ConfigurationChanged:
                {
                    // See if the this is a device that we were waiting for an account selection on. If so, pair
                    // it to the user that was waiting.
                    var wasOngoingAccountSelection = false;
                    for (var i = 0; i < s_OngoingAccountSelections.length; ++i)
                    {
                        if (s_OngoingAccountSelections[i].device != device)
                            continue;

                        var userIndex = new InputUser { m_Id = s_OngoingAccountSelections[i].userId }.index;
                        var queryResult = UpdatePlatformUserAccount(userIndex, device);
                        if ((queryResult & QueryPairedUserAccountCommand.Result.UserAccountSelectionInProgress) == 0)
                        {
                            wasOngoingAccountSelection = true;
                            s_OngoingAccountSelections.RemoveAtByMovingTailWithCapacity(i);
                            --i;

                            // If the device wasn't paired to the user, pair it now.
                            if (!s_AllUsers[userIndex].pairedDevices.ContainsReference(device))
                                AddDeviceToUser(userIndex, device);
                        }
                    }

                    // If it wasn't a configuration change event from an account selection, go and check whether
                    // there was a user account change that happened outside the application.
                    if (!wasOngoingAccountSelection)
                    {
                        // Could paired to multiple users. Repeatedly search in s_AllPairedDevices
                        // until we can't find the device anymore.
                        var deviceIndex = ArrayHelpers.IndexOfReference(s_AllPairedDevices, device);
                        while (deviceIndex != -1)
                        {
                            // Find user. Must be there as we found the device in s_AllPairedDevices.
                            var userIndex = -1;
                            for (var i = 0; i < s_AllUserCount; ++i)
                            {
                                var deviceStartIndex = s_AllUserData[i].deviceStartIndex;
                                if (deviceStartIndex <= deviceIndex && deviceIndex < deviceStartIndex + s_AllUserData[i].deviceCount)
                                {
                                    userIndex = i;
                                    break;
                                }
                            }

                            // Check user account.
                            UpdatePlatformUserAccount(userIndex, device);

                            // Search for another user paired to the same device.
                            deviceIndex = ArrayHelpers.IndexOfReference(s_AllPairedDevices, device, deviceIndex + 1);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Syntax for configuring a control scheme on a user.
        /// </summary>
        public struct ControlSchemeChangeSyntax
        {
            /// <summary>
            /// Leave the user's paired devices in place but pair any available devices
            /// that are still required by the control scheme.
            /// </summary>
            /// <returns></returns>
            /// <remarks>
            /// If there are unpaired devices that, at the platform level, are associated with the same
            /// user account, those will take precedence over other unpaired devices.
            /// </remarks>
            public ControlSchemeChangeSyntax AndPairRemainingDevices()
            {
                UpdateControlSchemeMatch(m_UserIndex, autoPairMissing: true);
                return this;
            }

            internal int m_UserIndex;
        }

        private uint m_Id;

        [Flags]
        public enum UserFlags
        {
            BindToAllDevices = 1 << 0,

            /// <summary>
            /// Whether we have initiated a user account selection.
            /// </summary>
            UserAccountSelectionInProgress = 1 << 1,
        }

        /// <summary>
        /// Data we store for each user.
        /// </summary>
        internal struct UserData
        {
            /// <summary>
            /// The platform handle associated with the user.
            /// </summary>
            /// <remarks>
            /// If set, this identifies the user on the platform. It also means that the devices
            /// assigned to the user may be paired at the platform level.
            /// </remarks>
            public InputUserAccountHandle? platformUserAccountHandle;

            /// <summary>
            /// Plain-text user name as returned by the underlying platform. Null if not associated with user on platform.
            /// </summary>
            public string platformUserAccountName;

            /// <summary>
            /// Platform-specific ID that identifies the user across sessions even if the user
            /// name changes.
            /// </summary>
            /// <remarks>
            /// This might not be a human-readable string.
            /// </remarks>
            public string platformUserAccountId;

            /// <summary>
            /// Number of devices in <see cref="InputUser.s_AllPairedDevices"/> assigned to the user.
            /// </summary>
            public int deviceCount;

            /// <summary>
            /// Index in <see cref="InputUser.s_AllPairedDevices"/> where the devices for this user start. Only valid
            /// if <see cref="deviceCount"/> is greater than zero.
            /// </summary>
            public int deviceStartIndex;

            /// <summary>
            /// Input actions associated with the user.
            /// </summary>
            public IInputActionCollection actions;

            /// <summary>
            /// Currently active control scheme or null if no control scheme has been set on the user.
            /// </summary>
            /// <remarks>
            /// This also dictates the binding mask that we're using with <see cref="actions"/>.
            /// </remarks>
            public InputControlScheme? controlScheme;

            public InputControlScheme.MatchResult controlSchemeMatch;

            public int lostDeviceCount;

            public int lostDeviceStartIndex;

            public InputUserSettings settings;

            public UserFlags flags;
        }

        /// <summary>
        /// Compare two devices for being associated with a specific platform user account.
        /// </summary>
        private struct CompareDevicesByUserAccount : IComparer<InputDevice>
        {
            public InputUserAccountHandle platformUserAccountHandle;

            public int Compare(InputDevice x, InputDevice y)
            {
                var firstAccountHandle = GetUserAccountHandleForDevice(x);
                var secondAccountHandle = GetUserAccountHandleForDevice(x);

                if (firstAccountHandle == platformUserAccountHandle &&
                    secondAccountHandle == platformUserAccountHandle)
                    return 0;

                if (firstAccountHandle == platformUserAccountHandle)
                    return -1;

                if (secondAccountHandle == platformUserAccountHandle)
                    return 1;

                return 0;
            }

            private static InputUserAccountHandle? GetUserAccountHandleForDevice(InputDevice device)
            {
                ////TODO (need to cache this)
                return null;
            }
        }

        private struct OngoingAccountSelection
        {
            public InputDevice device;
            public uint userId;
        }

        private static uint s_LastUserId;
        private static int s_AllUserCount;
        private static int s_AllPairedDeviceCount;
        private static int s_AllLostDeviceCount;
        private static InputUser[] s_AllUsers;
        private static UserData[] s_AllUserData;
        private static InputDevice[] s_AllPairedDevices; // We keep a single array that we slice out to each user.
        private static InputDevice[] s_AllLostDevices;
        private static InlinedArray<OngoingAccountSelection> s_OngoingAccountSelections;
        private static InlinedArray<Action<InputUser, InputUserChange, InputDevice>> s_OnChange;
        private static InlinedArray<Action<InputControl>> s_OnUnpairedDeviceUsed;
        private static Action<InputDevice, InputDeviceChange> s_OnDeviceChangeDelegate;
        private static bool s_OnDeviceChangeHooked;
        private static bool s_ListenForUnpairedDeviceActivity;

        private static void HookIntoDeviceChange()
        {
            if (s_OnDeviceChangeHooked)
                return;
            if (s_OnDeviceChangeDelegate == null)
                s_OnDeviceChangeDelegate = OnDeviceChange;
            InputSystem.onDeviceChange += s_OnDeviceChangeDelegate;
            s_OnDeviceChangeHooked = true;
        }

        private static void UnhookFromDeviceChange()
        {
            if (!s_OnDeviceChangeHooked)
                return;
            InputSystem.onDeviceChange -= s_OnDeviceChangeDelegate;
            s_OnDeviceChangeHooked = false;
        }

        internal static void ResetGlobals()
        {
            // Release native memory held by control scheme match results.
            for (var i = 0; i < s_AllUserCount; ++i)
                s_AllUserData[i].controlSchemeMatch.Dispose();

            // Don't reset s_LastUserId and just let it increment instead so we never generate
            // the same ID twice.

            s_AllUserCount = 0;
            s_AllPairedDeviceCount = 0;
            s_AllUsers = null;
            s_AllUserData = null;
            s_AllPairedDevices = null;
            s_OngoingAccountSelections = new InlinedArray<OngoingAccountSelection>();
            s_OnChange = new InlinedArray<Action<InputUser, InputUserChange, InputDevice>>();
            s_OnUnpairedDeviceUsed = new InlinedArray<Action<InputControl>>();
            s_OnDeviceChangeDelegate = null;
            s_OnDeviceChangeHooked = false;
        }
    }
}
