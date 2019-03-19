namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// The update order for <c>MonoBehaviour</c>s in ARFoundation.
    /// </summary>
    public static class ARUpdateOrder
    {
        /// <summary>
        /// The <see cref="ARSession"/>'s update order. Should come first.
        /// </summary>
        public const int k_Session = int.MinValue;

        /// <summary>
        /// The <see cref="ARPlaneManager"/>'s update order. Should come after
        /// the <see cref="ARSession"/>.
        /// </summary>
        public const int k_PlaneManager = k_Session + 1;

        /// <summary>
        /// The <see cref="ARPlane"/>'s update order. Should come after the
        /// <see cref="ARPlaneManager"/>.
        /// </summary>
        public const int k_Plane = k_PlaneManager + 1;

        /// <summary>
        /// The <see cref="ARPointCloudManager"/>'s update order. Should come
        /// after the <see cref="ARSession"/>.
        /// </summary>
        public const int k_PointCloudManager = k_Session + 1;

        /// <summary>
        /// The <see cref="ARPointCloud"/>'s update order. Should come after
        /// the <see cref="ARPointCloudManager"/>.
        /// </summary>
        public const int k_PointCloud = k_PointCloudManager + 1;

        /// <summary>
        /// The <see cref="ARReferencePointManager"/>'s update order.
        /// Should come after the <see cref="ARSession"/>.
        /// </summary>
        public const int k_ReferencePointManager = k_Session + 1;

        /// <summary>
        /// The <see cref="ARReferencePointManager"/>'s update order.
        /// Should come after the <see cref="ARReferencePointManager"/>.
        /// </summary>
        public const int k_ReferencePoint = k_ReferencePointManager + 1;

        /// <summary>
        /// The <see cref="ARInputManager"/>'s update order. Should come after
        /// the <see cref="ARSession"/>.
        /// </summary>
        public const int k_InputManager = k_Session + 1;

        /// <summary>
        /// The <see cref="ARCameraManager"/>'s update order. Should come after
        /// the <see cref="ARSession"/>.
        /// </summary>
        public const int k_CameraManager = k_Session + 1;
    }
}
