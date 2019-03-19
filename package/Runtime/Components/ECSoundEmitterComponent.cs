// #define DRAW_AUDIO_GIZMOS // <- get gizmos for changing cone properties, min/max distances in sound emitters (probably only relevant for small scenes).

using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Audio.Megacity
{
    public struct ECSoundEmitter : IComponentData
    {
        // All items are copied at bake-time by ECSoundEmitterComponent

        public int definitionIndex;

        public float3 position;

        public float3 coneDirection;

        public Entity definitionEntity;
        public Entity soundPlayerEntity;
    }

    [Serializable]
    public struct ECSoundEmitterDefinition : IComponentData
    {
        public int definitionIndex;
        public int soundPlayerIndexMin;
        public int soundPlayerIndexMax;
        public float probability;
        public float volume;
        public float coneAngle;
        public float coneTransition;
        public float minDist;
        public float maxDist;
        public float falloffMode;
    }

    public class ECSoundEmitterComponent : MonoBehaviour, IConvertGameObjectToEntity
    {
        public ECSoundEmitterDefinitionAsset definition;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem converstionSystem)
        {
            ECSoundEmitter emitter = new ECSoundEmitter();

            emitter.position = transform.position;
            emitter.coneDirection = -transform.right;

            if (definition != null)
            {
                emitter.definitionIndex = definition.data.definitionIndex;
                definition.Reflect(dstManager);
            }

            dstManager.AddComponentData(entity, emitter);
        }

        public float volume
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.volume;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.volume = value;
                    definition.Reflect(GetComponent<GameObjectEntity>().EntityManager);
                }
            }
        }

        public float coneAngle
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.coneAngle;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.coneAngle = value;
                    definition.Reflect(GetComponent<GameObjectEntity>().EntityManager);
                }
            }
        }

        public float coneTransition
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.coneTransition;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.coneTransition = value;
                    definition.Reflect(GetComponent<GameObjectEntity>().EntityManager);
                }
            }
        }

        public float minDist
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.minDist;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.minDist = value;
                    definition.Reflect(GetComponent<GameObjectEntity>().EntityManager);
                }
            }
        }

        public float maxDist
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.maxDist;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.maxDist = value;
                    definition.Reflect(GetComponent<GameObjectEntity>().EntityManager);
                }
            }
        }

        public float falloffMode
        {
            get
            {
                return (definition == null) ? 0.0f : definition.data.falloffMode;
            }
            set
            {
                if (definition != null)
                {
                    definition.data.falloffMode = value;
                    definition.Reflect(GetComponent<GameObjectEntity>().EntityManager);
                }
            }
        }

        void Start()
        {
        }

#if UNITY_EDITOR && DRAW_AUDIO_GIZMOS
        void DrawGizmos(float alpha)
        {
            if (definition == null)
                return;

            var v = definition.data;

            alpha *= 0.5f;

            Gizmos.color = new Color(0.0f, 1.0f, 0.0f, alpha);
            Gizmos.DrawWireSphere(transform.position, v.minDist);

            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, alpha);
            Gizmos.DrawWireSphere(transform.position, v.maxDist);

            var v1 = transform.right;
            var v2 = transform.up;
            var v3 = transform.forward;

            float angle1Deg = v.coneAngle;
            float halfAngle1Rad = angle1Deg * Mathf.PI / 360.0f;
            float c1 = 0.5f * v.maxDist * Mathf.Cos(halfAngle1Rad);
            float s1 = 0.5f * v.maxDist * Mathf.Sin(halfAngle1Rad);
            var p1a = v1 * c1 + v2 * s1;
            var p1b = v1 * c1 + v3 * s1;

            float angle2Deg = v.coneAngle - v.coneTransition;
            float halfAngle2Rad = angle2Deg * Mathf.PI / 360.0f;
            float c2 = 0.5f * v.maxDist * Mathf.Cos(halfAngle2Rad);
            float s2 = 0.5f * v.maxDist * Mathf.Sin(halfAngle2Rad);
            var p2a = v1 * c2 + v2 * s2;
            var p2b = v1 * c2 + v3 * s2;

            var col1 = new Color(1.0f, 1.0f, 0.0f, 0.1f * alpha);
            var col2 = new Color(0.0f, 1.0f, 1.0f, 0.1f * alpha);

            Handles.color = col1;
            Handles.DrawSolidArc(transform.position, v1, p1a, 360.0f, v.maxDist);

            Handles.color = col2;
            Handles.DrawSolidArc(transform.position, v1, p2a, 360.0f, v.maxDist);

            Handles.color = col1;
            Handles.DrawSolidArc(transform.position, v3, p1a, -angle1Deg, v.maxDist);

            Handles.color = col1;
            Handles.DrawSolidArc(transform.position, v2, p1b, angle1Deg, v.maxDist);

            Handles.color = col2;
            Handles.DrawSolidArc(transform.position, v3, p2a, -angle2Deg, v.maxDist);

            Handles.color = col2;
            Handles.DrawSolidArc(transform.position, v2, p2b, angle2Deg, v.maxDist);
        }

        [DrawGizmo(GizmoType.Pickable)]
        void OnDrawGizmos()
        {
            DrawGizmos(0.5f);
        }

        void OnDrawGizmosSelected()
        {
            DrawGizmos(0.25f);
        }

#endif
    }
}
