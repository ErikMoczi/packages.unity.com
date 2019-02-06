
#if UNITY_EDITOR
using UnityEngine;

namespace Unity.Tiny
{
    [ExecuteInEditMode]
    internal class RectHitBox2D : MonoBehaviour
    {
        public Rect Box { get; set; }
        
        private void OnDrawGizmosSelected()
        {
            // pivot is always 0, 0 in the runtime
            var pivot = new Vector3(0, 0, 0);

            var matrix = Gizmos.matrix;
            var color = Gizmos.color;
            
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;

            var p1 = pivot + new Vector3(Box.x, Box.y, 0);
            var p2 = pivot + new Vector3(Box.x, Box.y, 0) + new Vector3(Box.width, 0);
            var p3 = pivot + new Vector3(Box.x, Box.y, 0) + new Vector3(Box.width, Box.height);
            var p4 = pivot + new Vector3(Box.x, Box.y, 0) + new Vector3(0, Box.height);
            
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
