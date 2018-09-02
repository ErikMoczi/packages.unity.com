using UnityEngine;
using System;

namespace ResourceManagement.ResourceProviders
{
    public class CompletionUpdater : MonoBehaviour
    {
        public Func<bool> operation;
        public object context;
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
