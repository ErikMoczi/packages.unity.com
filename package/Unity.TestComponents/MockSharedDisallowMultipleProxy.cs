using System;

namespace Unity.Entities.Tests
{
    [Serializable]
    public struct MockSharedDisallowMultiple : ISharedComponentData
    {
        public int Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class MockSharedDisallowMultipleProxy : SharedComponentDataProxy<MockSharedDisallowMultiple>
    {

    }
}
