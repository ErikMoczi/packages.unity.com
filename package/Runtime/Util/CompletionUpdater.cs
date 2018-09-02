using System;

namespace UnityEngine.ResourceManagement
{
    internal class CompletionUpdater : MonoBehaviour
    {
        public Func<bool> operation;
        void Update()
        {
            if (operation())
                Destroy(gameObject);
        }

        public static void UpdateUntilComplete(string name, Func<bool> func)
        {
            new GameObject(name).AddComponent<CompletionUpdater>().operation = func;
        }
    }
}
