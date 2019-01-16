using System.Collections;

namespace Unity.EditorCoroutines.Editor
{
    public static class EditorCoroutineUtility
    {
        /// <summary>
        /// Starts an editor coroutine, with the specified owner object. If the owner expires while the coroutine is still executing, execution will stop.
        /// </summary>
        /// <param name="routine"> IEnumerator to iterate over. </param>
        /// <param name="owner">Object owning the coroutine. </param>
        /// <returns></returns>
        public static EditorCoroutine StartCoroutine(IEnumerator routine, object owner)
        {
            return new EditorCoroutine(routine, owner);
        }

        /// <summary>
        /// Starts an editor coroutine, without a owning object. The editor coroutine will execute until it is done or otherwise canceled.
        /// </summary>
        /// <param name="routine"> IEnumerator to iterate over. </param>
        /// <returns></returns>
        public static EditorCoroutine StartCoroutineOwnerless(IEnumerator routine)
        {
            return new EditorCoroutine(routine);
        }

        /// <summary>
        /// Stops an editor coroutine.
        /// </summary>
        /// <param name="coroutine"></param>
        public static void StopCoroutine(EditorCoroutine coroutine)
        {
            coroutine.Stop();
        }
    }
}