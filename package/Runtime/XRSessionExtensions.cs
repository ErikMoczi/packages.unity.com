using System;
using System.Collections.Generic;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.ARExtensions
{
    /// <summary>
    /// Provides extensions to the <c>XRSessionSubsystem</c>.
    /// </summary>
    public static class XRSessionExtensions
    {
        /// <summary>
        /// A <c>delegate</c> used for asynchronous operations that retrieve data of type <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The type of data to operation retrieves.</typeparam>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> being extended.</param>
        /// <returns>A <see cref="Promise{T}"/> used to determine status and result of the asynchronous operation.</returns>
        public delegate Promise<T> AsyncDelegate<T>(XRSessionSubsystem sessionSubsystem);

        /// <summary>
        /// Registers a handler for asynchronous AR software installation. See <see cref="InstallAsync(XRSessionSubsystem)"/>.
        /// </summary>
        /// <remarks>
        /// A platform-specific package should implement an installation handler and register it using this method.
        /// </remarks>
        /// <param name="subsystemId">The string name associated with the package's session subsystem.</param>
        /// <param name="handler">An <see cref="AsyncDelegate{T}"/> to handle the request.</param>
        public static void RegisterInstallAsyncHandler(string subsystemId, AsyncDelegate<SessionInstallationStatus> handler)
        {
            s_InstallAsyncDelegates[subsystemId] = handler;
        }

        /// <summary>
        /// Registers a handler for an asynchronous AR availability check. See <see cref="GetAvailabilityAsync(XRSessionSubsystem)"/>.
        /// </summary>
        /// <remarks>
        /// A platform-specific package should implement an installation handler and register it using this method.
        /// </remarks>
        /// <param name="subsystemId">The string name associated with the package's session subsystem.</param>
        /// <param name="handler">An <see cref="AsyncDelegate{T}"/> to handle the request.</param>
        public static void RegisterGetAvailabilityAsyncHandler(string subsystemId, AsyncDelegate<SessionAvailability> handler)
        {
            s_GetAvailabilityAsyncDelegates[subsystemId] = handler;
        }

        /// <summary>
        /// Asynchronously retrieves the <see cref="SessionAvailability"/>. Used to determine whether
        /// the current device supports AR and if the necessary software is installed.
        /// </summary>
        /// <remarks>
        /// This platform-agnostic method is typically implemented by a platform-specific package.
        /// </remarks>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> to extend.</param>
        /// <returns>A <see cref="Promise{SessionAvailability}"/> which can be used to determine when the
        /// availability has been determined and retrieve the result.</returns>
        public static Promise<SessionAvailability> GetAvailabilityAsync(this XRSessionSubsystem sessionSubsystem)
        {
            return ExecuteAsync(sessionSubsystem, s_GetAvailabilityAsyncDelegates,
                SessionAvailability.Supported | SessionAvailability.Installed);
        }

        /// <summary>
        /// Asynchronously attempts to install AR software on the current device.
        /// </summary>
        /// <remarks>
        /// This platform-agnostic method is typically implemented by a platform-specific package.
        /// </remarks>
        /// <param name="sessionSubsystem">The <c>XRSessionSubsystem</c> to extend.</param>
        /// <returns>A <see cref="Promise{SessionInstallationStatus}"/> which can be used to determine when the
        /// installation completes and retrieve the result.</returns>
        public static Promise<SessionInstallationStatus> InstallAsync(this XRSessionSubsystem sessionSubsystem)
        {
            return ExecuteAsync(sessionSubsystem, s_InstallAsyncDelegates,
                SessionInstallationStatus.ErrorInstallNotSupported);
        }

        static Promise<T> ExecuteAsync<T>(this XRSessionSubsystem sessionSubsystem,
            Dictionary<string, AsyncDelegate<T>> delegates, T defaultValue = default(T))
        {
            if (sessionSubsystem == null)
                throw new ArgumentNullException("sessionSubsystem");

            AsyncDelegate<T> asyncDelegate;
            if (delegates.TryGetValue(sessionSubsystem.SubsystemDescriptor.id, out asyncDelegate))
            {
                return asyncDelegate(sessionSubsystem);
            }
            else
            {
                return Promise<T>.CreateResolvedPromise(defaultValue);
            }
        }

        static Dictionary<string, AsyncDelegate<SessionInstallationStatus>> s_InstallAsyncDelegates =
            new Dictionary<string, AsyncDelegate<SessionInstallationStatus>>();

        static Dictionary<string, AsyncDelegate<SessionAvailability>> s_GetAvailabilityAsyncDelegates =
            new Dictionary<string, AsyncDelegate<SessionAvailability>>();
    }
}
