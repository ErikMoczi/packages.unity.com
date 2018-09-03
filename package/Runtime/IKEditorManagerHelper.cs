using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
namespace UnityEditor.Experimental.U2D.IK
{
    [DefaultExecutionOrder(-1)]
    [ExecuteInEditMode]
    internal class IKEditorManagerHelper : MonoBehaviour
    {
        public UnityEvent onLateUpdate = new UnityEvent();

        void LateUpdate()
        {
            if (Application.isPlaying)
                return;

            onLateUpdate.Invoke();
        }
    }
}
#endif
