namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public struct TransformHandle
    {
        TransformStreamHandle m_StreamHandle;
        TransformSceneHandle m_SceneHandle;
        byte m_InStream;

        public Vector3 GetLocalPosition(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetLocalPosition(stream) : m_SceneHandle.GetLocalPosition(stream);

        public Quaternion GetLocalRotation(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetLocalRotation(stream) : m_SceneHandle.GetLocalRotation(stream);

        public Vector3 GetLocalScale(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetLocalScale(stream) : m_SceneHandle.GetLocalScale(stream);

        public Vector3 GetPosition(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetPosition(stream) : m_SceneHandle.GetPosition(stream);

        public Quaternion GetRotation(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.GetRotation(stream) : m_SceneHandle.GetRotation(stream);

        public bool IsResolved(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.IsResolved(stream) : true;

        public bool IsValid(AnimationStream stream) =>
            m_InStream == 1 ? m_StreamHandle.IsValid(stream) : m_SceneHandle.IsValid(stream);

        public void Resolve(AnimationStream stream)
        {
            if (m_InStream == 1)
                m_StreamHandle.Resolve(stream);
        }

        public void SetLocalPosition(AnimationStream stream, Vector3 position)
        {
            if (m_InStream == 1)
                m_StreamHandle.SetLocalPosition(stream, position);
        }

        public void SetLocalRotation(AnimationStream stream, Quaternion rotation)
        {
            if (m_InStream == 1)
                m_StreamHandle.SetLocalRotation(stream, rotation);
        }

        public void SetLocalScale(AnimationStream stream, Vector3 scale)
        {
            if (m_InStream == 1)
                m_StreamHandle.SetLocalScale(stream, scale);
        }

        public void SetPosition(AnimationStream stream, Vector3 position)
        {
            if (m_InStream == 1)
                m_StreamHandle.SetPosition(stream, position);
        }

        public void SetRotation(AnimationStream stream, Quaternion rotation)
        {
             if (m_InStream == 1)
                m_StreamHandle.SetRotation(stream, rotation);
        }

        public static TransformHandle Bind(Animator animator, Transform transform)
        {
            TransformHandle handle = new TransformHandle();
            if (transform == null)
                return handle;

            handle.m_InStream = (byte)(transform.IsChildOf(animator.transform) ? 1 : 0);
            if (handle.m_InStream == 1)
                handle.m_StreamHandle = animator.BindStreamTransform(transform);
            else
                handle.m_SceneHandle = animator.BindSceneTransform(transform);

            return handle;
        }
    }
}