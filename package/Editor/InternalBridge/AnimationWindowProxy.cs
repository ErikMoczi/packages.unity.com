using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.IK
{
    public class AnimationWindowProxy
    {
        private AnimationWindow m_AnimationWindow;

        public int animationFrame
        {
            get { return m_AnimationWindow.state.currentFrame; }
            set { m_AnimationWindow.state.currentFrame = value; }
        }

        public AnimationClip animationClip
        {
            get { return m_AnimationWindow.state.activeAnimationClip; }
        }

        public int numberOfAnimationFrames
        {
            get
            {
                return (int) (animationClip.length * animationClip.frameRate);
            }
        }

        public GameObject rootGameObject
        {
            get { return m_AnimationWindow.state.activeRootGameObject; }
        }

        public float currentTime
        {
            get { return m_AnimationWindow.state.currentTime; }
        }

        public bool previewing
        {
            get { return m_AnimationWindow.state.previewing; }
        }

        internal AnimationWindowProxy(AnimationWindow animationWindow)
        {
            m_AnimationWindow = animationWindow;
        }

        public bool IsRecording()
        {
            return m_AnimationWindow.state.recording;
        }

        public void StartRecording()
        {
            m_AnimationWindow.state.StartRecording();
        }

        public void StopRecording()
        {
            m_AnimationWindow.state.StopRecording();
        }

        public int TimeToFrame(float time)
        {
            return m_AnimationWindow.state.TimeToFrameFloor(time);
        }
    }
}


