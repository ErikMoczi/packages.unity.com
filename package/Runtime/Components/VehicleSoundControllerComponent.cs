using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Audio;
using Unity.Experimental.Audio;

namespace Unity.Audio.Megacity
{
    [Serializable]
    public class SoundLayer
    {
        public AudioClip m_AudioClip;
        [Range(0, 1)]
        public float m_Volume;
        [Range(0, 1)]
        public float m_VolumeMax;

        public bool m_InvertVolumeController;

        [Range(0, 20)]
        public float m_Pitch;
        [Range(0, 20)]
        public float m_PitchMax;

        public bool m_EqOn;

        [Range(20, 11000)]
        public float m_EqBand1;
        [Range(20, 11000)]
        public float m_EqBand2;

        public VehicleSoundController.Parameter m_VolumeController;
        public VehicleSoundController.Parameter m_PitchController;
        public VehicleSoundController.Parameter m_EqBand1Controller;
        public VehicleSoundController.Parameter m_EqBand2Controller;
    }

    struct VehicleSoundControllerSSS : ISystemStateSharedComponentData
    {
        internal NativeArray<int> m_HasFilter;
        internal StateVariableFilter[] m_Filters;
        internal ECSoundPlayerNode[] m_PlayerNodes; // looping sounds
        internal NativeArray<DSPConnection> m_PlayerConnections; // connections for volume control

        internal DSPNode m_MasterGainNode;

        internal void OnEnable(VehicleSoundController ctl, AudioManagerSystem audioManager, DSPCommandBlockInterceptor block)
        {
            m_PlayerNodes = new ECSoundPlayerNode[ctl.m_Layers.Length];
            m_PlayerConnections = new NativeArray<DSPConnection>(ctl.m_Layers.Length, Allocator.Persistent);
            m_Filters = new StateVariableFilter[ctl.m_Layers.Length * 2];
            m_HasFilter = new NativeArray<int>(ctl.m_Layers.Length, Allocator.Persistent);

            m_MasterGainNode = block.CreateDSPNode<GainNodeJob.Params, NoProvs, GainNodeJob>();
            DSPCommandBlockInterceptor.SetNodeName(m_MasterGainNode, "Gain", DSPCommandBlockInterceptor.Group.MainVehicle);
            block.AddInletPort(m_MasterGainNode, 2, SoundFormat.Stereo);
            block.AddOutletPort(m_MasterGainNode, 2, SoundFormat.Stereo);
            block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(m_MasterGainNode, GainNodeJob.Params.Vol1, ctl.m_MasterVolume, (uint)(44100 * ctl.m_FadeinTime));
            block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(m_MasterGainNode, GainNodeJob.Params.Vol2, ctl.m_MasterVolume, (uint)(44100 * ctl.m_FadeinTime));

            for (int i = 0; i < ctl.m_Layers.Length; i++)
            {
                if (ctl.m_Layers[i].m_AudioClip == null)
                {
                    continue;
                }

                m_PlayerNodes[i] = ECSoundPlayerNode.Create(block, ctl.m_Layers[i].m_AudioClip.CreateAudioSampleProvider(0, 0, true), ctl.m_Layers[i].m_Pitch);
                DSPCommandBlockInterceptor.SetNodeName(m_PlayerNodes[i].node, ctl.m_Layers[i].m_AudioClip.name, DSPCommandBlockInterceptor.Group.MainVehicle);

                if (ctl.m_Layers[i].m_EqOn)
                {
                    m_HasFilter[i] = 1;

                    m_Filters[i * 2 + 0] = StateVariableFilter.Create(block);
                    block.SetFloat<StateVariableFilter.NodeJob.Params, NoProvs, StateVariableFilter.NodeJob>(m_Filters[i * 2 + 0].node, StateVariableFilter.NodeJob.Params.FilterType, (int)StateVariableFilter.FilterType.Bell, 0);
                    block.SetFloat<StateVariableFilter.NodeJob.Params, NoProvs, StateVariableFilter.NodeJob>(m_Filters[i * 2 + 0].node, StateVariableFilter.NodeJob.Params.Q, 0.5f, 0);

                    m_Filters[i * 2 + 1] = StateVariableFilter.Create(block);
                    block.SetFloat<StateVariableFilter.NodeJob.Params, NoProvs, StateVariableFilter.NodeJob>(m_Filters[i * 2 + 1].node, StateVariableFilter.NodeJob.Params.FilterType, (int)StateVariableFilter.FilterType.Bell, 0);
                    block.SetFloat<StateVariableFilter.NodeJob.Params, NoProvs, StateVariableFilter.NodeJob>(m_Filters[i * 2 + 0].node, StateVariableFilter.NodeJob.Params.Q, 0.5f, 0);

                    DSPCommandBlockInterceptor.SetNodeName(m_Filters[i * 2 + 0].node, "Parametric EQ 1", DSPCommandBlockInterceptor.Group.MainVehicle);
                    DSPCommandBlockInterceptor.SetNodeName(m_Filters[i * 2 + 1].node, "Parametric EQ 2", DSPCommandBlockInterceptor.Group.MainVehicle);
                    
                    block.Connect(m_Filters[i * 2 + 0].node, 0, m_Filters[i * 2 + 1].node, 0);
                    block.Connect(m_Filters[i * 2 + 1].node, 0, m_MasterGainNode, 0);

                    m_PlayerConnections[i] = block.Connect(m_PlayerNodes[i].node, 0, m_Filters[i * 2 + 0].node, 0);
                }
                else
                {
                    m_PlayerConnections[i] = block.Connect(m_PlayerNodes[i].node, 0, m_MasterGainNode, 0);
                }
            }

            block.Connect(m_MasterGainNode, 0, audioManager.MasterChannel, 0);
        }

        internal void OnDisable(DSPCommandBlockInterceptor block)
        {
            for (int i = 0; i < m_PlayerNodes.Length; i++)
            {
                m_PlayerNodes[i].Dispose(block);
                if (m_HasFilter[i] != 0)
                {
                    m_Filters[i * 2 + 0].Dispose(block);
                    m_Filters[i * 2 + 1].Dispose(block);
                }
            }
            m_HasFilter.Dispose();
            m_PlayerConnections.Dispose();

            block.ReleaseDSPNode(m_MasterGainNode);
        }
    }

    struct VehicleSoundControllerSS : ISystemStateComponentData
    {
        internal float m_YawAngle;
        internal float m_PitchAngle;
        internal float m_RollAngle;
        internal float m_EngineStruggle;
        internal float m_Speed;

        internal float3 m_PreviousVehiclePosition;

        internal float m_MasterVolumeTarget;
        internal float m_MasterVolumeAdj;
        internal float m_MasterFadeoutTimer;

        float GetControllerValue(VehicleSoundController.Parameter p)
        {
            switch (p)
            {
                case VehicleSoundController.Parameter.YawAngle:
                    return m_YawAngle;
                case VehicleSoundController.Parameter.PitchAngle:
                    return m_PitchAngle;
                case VehicleSoundController.Parameter.RollAngle:
                    return m_RollAngle;
                case VehicleSoundController.Parameter.Velocity:
                    return m_Speed;
                case VehicleSoundController.Parameter.InverseYawAngle:
                    return -m_YawAngle;
                case VehicleSoundController.Parameter.InversePitchAngle:
                    return -m_PitchAngle;
                case VehicleSoundController.Parameter.InverseRollAngle:
                    return -m_RollAngle;
                case VehicleSoundController.Parameter.InverseVelocity:
                    return 1 - m_Speed;
                case VehicleSoundController.Parameter.AbsoluteYawAngle:
                    return math.abs(m_YawAngle);
                case VehicleSoundController.Parameter.AbsolutePitchAngle:
                    return math.abs(m_PitchAngle);
                case VehicleSoundController.Parameter.AbsoluteRollAngle:
                    return math.abs(m_RollAngle);
            }
            return 0;
        }

        internal void OnEnable(VehicleSoundController ctl, AudioManagerSystem audioManager, DSPCommandBlockInterceptor block)
        {
            m_YawAngle = 0;
            m_PitchAngle = 0;
            m_RollAngle = 0;
            m_EngineStruggle = 0;
            m_Speed = 0;

            m_MasterVolumeAdj = m_MasterVolumeTarget = 1;
            m_MasterFadeoutTimer = 5;

            m_PreviousVehiclePosition = audioManager.PlayerTransform.position;
        }

        internal void OnUpdate(VehicleSoundController ctl, AudioManagerSystem audioManager, DSPCommandBlockInterceptor block, VehicleSoundControllerSSS sss)
        {
            // get vehicle rotation in camera space

            Quaternion rotation = Quaternion.Inverse(audioManager.ListenerTransform.rotation) * audioManager.PlayerTransform.rotation;

            // set yaw/roll from player vehicle
            m_YawAngle = (float)math.abs(math.clamp(math.sin(rotation.eulerAngles.y / 180 * math.PI), -1, 1));

            float x = rotation.eulerAngles.x;

            // un-tilt X axis
            x -= 11; // NOTE this value needs to be in sync with player camera setup
            if (x > 360)
            {
                x -= 360;
            }

            m_PitchAngle = (float)math.clamp(math.sin(x / 180 * math.PI), -1, 1);

            m_RollAngle = (float)math.clamp(math.sin(rotation.eulerAngles.z / 180 * math.PI), -1, 1);

            // vehicle speed from position
            float3 pos = audioManager.PlayerTransform.position;
            float3 dir = pos - m_PreviousVehiclePosition;
            float speed = math.length(dir) / (50 * Time.deltaTime);
            m_PreviousVehiclePosition = pos;
            float prev = m_Speed;

            m_Speed = m_Speed + (speed - m_Speed) * 0.1f;
            if (m_Speed < 0)
            {
                m_Speed = 0;
            }
            else if (m_Speed > 1)
            {
                m_Speed = 1;
            }

            // turn down the volume when the car is moving forward at max speed;
            // otherwise fade to full folume -- that's when volume is less than max, or the angles are non-zero
            if (m_MasterVolumeTarget != 1
                && (math.abs(prev - m_Speed) > 0.0017
                    || math.abs(m_YawAngle) > 0.4
                    || math.abs(m_PitchAngle) > 0.4
                    || math.abs(m_RollAngle) > 0.4
                )
            )
            {
                m_MasterFadeoutTimer = ctl.m_FadeoutWait;
                m_MasterVolumeTarget = 1;
                block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(sss.m_MasterGainNode, GainNodeJob.Params.Vol1, ctl.m_MasterVolume * m_MasterVolumeTarget, (uint)(44100 * ctl.m_FadeinTime));
                block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(sss.m_MasterGainNode, GainNodeJob.Params.Vol2, ctl.m_MasterVolume * m_MasterVolumeTarget, (uint)(44100 * ctl.m_FadeinTime));
            }

            if (m_MasterFadeoutTimer > 0)
            {
                m_MasterFadeoutTimer -= Time.deltaTime;
                if (m_MasterFadeoutTimer <= 0)
                {
                    m_MasterVolumeTarget = ctl.m_FadeoutFloor;
                    block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(sss.m_MasterGainNode, GainNodeJob.Params.Vol1, ctl.m_MasterVolume * m_MasterVolumeTarget, (uint)(44100 * ctl.m_FadeoutTime));
                    block.SetFloat<GainNodeJob.Params, NoProvs, GainNodeJob>(sss.m_MasterGainNode, GainNodeJob.Params.Vol2, ctl.m_MasterVolume * m_MasterVolumeTarget, (uint)(44100 * ctl.m_FadeoutTime));
                }
            }

            for (int i = 0; i < ctl.m_Layers.Length; i++)
            {
                if (ctl.m_Layers[i].m_AudioClip == null)
                {
                    continue;
                }

                float volume = GetControllerValue(ctl.m_Layers[i].m_VolumeController);
                float pitch = GetControllerValue(ctl.m_Layers[i].m_PitchController);
                float eqband1 = GetControllerValue(ctl.m_Layers[i].m_EqBand1Controller) * 10;
                float eqband2 = GetControllerValue(ctl.m_Layers[i].m_EqBand2Controller) * 10;

                if (sss.m_HasFilter[i] != 0)
                {
                    block.SetFloat<StateVariableFilter.NodeJob.Params, NoProvs, StateVariableFilter.NodeJob>(sss.m_Filters[i * 2 + 0].node, StateVariableFilter.NodeJob.Params.Cutoff, ctl.m_Layers[i].m_EqBand1, 0);
                    block.SetFloat<StateVariableFilter.NodeJob.Params, NoProvs, StateVariableFilter.NodeJob>(sss.m_Filters[i * 2 + 0].node, StateVariableFilter.NodeJob.Params.GainInDBs, eqband1, 100);
                    block.SetFloat<StateVariableFilter.NodeJob.Params, NoProvs, StateVariableFilter.NodeJob>(sss.m_Filters[i * 2 + 1].node, StateVariableFilter.NodeJob.Params.Cutoff, ctl.m_Layers[i].m_EqBand2, 0);
                    block.SetFloat<StateVariableFilter.NodeJob.Params, NoProvs, StateVariableFilter.NodeJob>(sss.m_Filters[i * 2 + 1].node, StateVariableFilter.NodeJob.Params.GainInDBs, eqband2, 100);
                }
//                Debug.Log ("-- band1: " + eqband1);
//                Debug.Log ("-- band2: " + eqband2);

                // final volume
                if (ctl.m_Layers[i].m_InvertVolumeController)
                {
                    volume = 1 - volume;
                }
                var vol = ctl.m_Layers[i].m_Volume + (ctl.m_Layers[i].m_VolumeMax - ctl.m_Layers[i].m_Volume) * volume;
                block.SetAttenuation(sss.m_PlayerConnections[i], vol, 100);
//                if (i == 0) {
//                    Debug.Log($"layer params: volume: {vol}, v/y/p/r: {m_Speed} {m_YawAngle} {m_PitchAngle} {m_RollAngle}");
//                }

                // final pitch
                var p = ctl.m_Layers[i].m_Pitch + (ctl.m_Layers[i].m_PitchMax - ctl.m_Layers[i].m_Pitch) * pitch;
                sss.m_PlayerNodes[i].Update(block, p);
            }
        }
    }

    [Serializable]
    public struct VehicleSoundController : ISharedComponentData
    {
        public enum Parameter
        {
            Nothing,
            YawAngle,
            PitchAngle,
            RollAngle,
            Velocity,
            InverseYawAngle,
            InversePitchAngle,
            InverseRollAngle,
            InverseVelocity,
            AbsoluteYawAngle,
            AbsolutePitchAngle,
            AbsoluteRollAngle,
        };

        [Range(0, 1)]
        public float m_MasterVolume;
        public float m_FadeoutWait;
        public float m_FadeoutFloor;
        public float m_FadeoutTime;
        public float m_FadeinTime;
        public SoundLayer[] m_Layers;
        //public GameObject m_Vehicle;
        //public GameObject m_Camera;
    }

    public class VehicleSoundControllerComponent : SharedComponentDataProxy<VehicleSoundController>
    {
    }
}
