namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Represents the current state of an AR session.
    /// </summary>
    public enum ARSessionState
    {
        /// <summary>
        /// The session is not currently running.
        /// </summary>
        NotRunning,

        /// <summary>
        /// A session was requested, but the system is still initializing.
        /// </summary>
        /// <remarks>
        /// If tracking is lost during a session, the session stat may go
        /// back to <see cref="Initializing"/>.
        /// </remarks>
        Initializing,

        /// <summary>
        /// A session is running and receiving data.
        /// </summary>
        Running
    }
}
