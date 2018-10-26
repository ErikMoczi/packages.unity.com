namespace Unity.EditorCoroutines.Editor
{
    /// <summary>
    /// Suspends the editor coroutine execution for the given amount of seconds using unscaled time.
    /// </summary>
    public class EditorWaitForSeconds
    {
        public double WaitTime { get; }

        public EditorWaitForSeconds(float time)
        {
            WaitTime = time;
        }
    }
}