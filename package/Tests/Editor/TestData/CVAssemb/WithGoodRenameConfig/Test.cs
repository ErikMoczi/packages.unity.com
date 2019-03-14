#if UNITY_2018_1_OR_NEWER
namespace WithGoodRenameConfig
{
    public class Foo
    {
        [System.Obsolete("(UnityUpgradable) -> Baz()")]
        public void Bar() {}
        public void Baz() {}
    }
}
#endif
