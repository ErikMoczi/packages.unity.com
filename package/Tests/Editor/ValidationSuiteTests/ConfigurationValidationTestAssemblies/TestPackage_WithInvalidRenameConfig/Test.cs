#if UNITY_2018_1_OR_NEWER
namespace TestPackage_WithInvalidRenameConfig
{
    public class Foo
    {
        [System.Obsolete("(UnityUpgradable) -> Baz()")]
        public void Bar() {}
    }
}
#endif