namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Represents the current state of the AR system.
    /// </summary>
    public enum SystemState
    {
        /// <summary>
        /// The AR system has not been initialized. Availability is unknown.
        /// <see cref="ARSubsystemManager.CheckAvailability"/>.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// The system is checking for the availability of AR.
        /// <see cref="ARSubsystemManager.CheckAvailability"/>.
        /// </summary>
        CheckingAvailability,

        /// <summary>
        /// AR is supported, but requires an additional install.
        /// <see cref="ARSubsystemManager.Install"/>.
        /// </summary>
        NeedsInstall,

        /// <summary>
        /// AR software is being installed. <see cref="ARSubsystemManager.Install"/>.
        /// </summary>
        Installing,

        /// <summary>
        /// AR is supported and ready.
        /// </summary>
        Ready,

        /// <summary>
        /// AR is not supported on the current device.
        /// </summary>
        Unsupported
    }
}
