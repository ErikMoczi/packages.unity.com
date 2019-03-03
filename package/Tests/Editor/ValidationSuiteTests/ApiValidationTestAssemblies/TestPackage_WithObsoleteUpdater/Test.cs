#if UNITY_2018_1_OR_NEWER
public class Foo
{
    [System.Obsolete("reason (UnityUpgradable) -> Foo", false)]
    public int Bar { get; set; }
}
#endif
