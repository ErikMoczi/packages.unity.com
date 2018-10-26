using System.Collections;
using UnityEditor;

namespace Unity.EditorCoroutines.Editor
{
    public static class EditorWindowCoroutineExtension
    {
        /// <summary>
        /// Start a editor coroutine, owned by the calling EditorWindow object.
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static EditorCoroutine StartCoroutine(this EditorWindow window, IEnumerator routine)
        {
            return new EditorCoroutine(routine, window);
        }

        /// <summary>
        /// Stop a editor coroutine.
        /// </summary>
        /// <param name="coroutine"></param>
        public static void StopCoroutine(this EditorWindow window, EditorCoroutine coroutine)
        {
            EditorCoroutineUtility.StopCoroutine(coroutine);
        }
    }
}