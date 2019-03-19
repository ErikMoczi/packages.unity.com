using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;


namespace Unity.Audio.Megacity
{
    [System.Serializable]
    public struct BoundingBox : IComponentData
    {
        public float3 center;
        public float3 size;
    }

    [ExecuteInEditMode]
    public class BoundingBoxComponent : ComponentDataProxy<BoundingBox>
    {
        public Vector3 size;

#if UNITY_EDITOR
        [DrawGizmo(GizmoType.Pickable)]
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0.5f, 0.2f, 0.2f);
            Gizmos.DrawCube(transform.position, 2.0f * size);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawCube(transform.position, 2.0f * size);
        }

#endif

        void Start()
        {
            GetComponent<GameObjectEntity>().EntityManager.SetComponentData(GetComponent<GameObjectEntity>().Entity, new BoundingBox
            {
                center = transform.position,
                // make sure size is always positive
                size = new float3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z))
            });
        }
    }
}
