
#if UNITY_EDITOR
using UnityEngine;

namespace Unity.Tiny
{
    [ExecuteInEditMode]
    internal class Sprite2DRendererHitBox2D : MonoBehaviour
    {
        public Sprite Sprite { get; set; }
        
        private void OnDrawGizmosSelected()
        {
            if (null == Sprite)
            {
                return;
            }
            
            var matrix = Gizmos.matrix;
            var color = Gizmos.color;
            
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            
            var bounds = Sprite.bounds;
            var p1 = new Vector3(bounds.min.x, bounds.min.y, transform.position.z);
            var p2 = new Vector3(bounds.min.x, bounds.max.y, transform.position.z);
            var p3 = new Vector3(bounds.max.x, bounds.max.y, transform.position.z);
            var p4 = new Vector3(bounds.max.x, bounds.min.y, transform.position.z);
            

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);

            Gizmos.matrix = matrix;
            Gizmos.color = color;
        }
    }
}
#endif // UNITY_EDITOR
