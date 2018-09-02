using UnityEngine;

namespace UnityEngine.ResourceManagement
{
    class PooledInstanceProviderBehavior : MonoBehaviour
    {
        PooledInstanceProvider m_Provider;
        public void Init(PooledInstanceProvider p)
        {
            m_Provider = p;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            m_Provider.Update();
        }
    }
}
