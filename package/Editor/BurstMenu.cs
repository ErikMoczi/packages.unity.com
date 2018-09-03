using Unity.Burst.LowLevel;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;

class BurstMenu
{
    const string kEnableBurst = "Jobs/Enable Burst Compiler";
    [MenuItem(kEnableBurst, false)]
    static void EnableBurst()
    {
        JobsUtility.JobCompilerEnabled = !JobsUtility.JobCompilerEnabled;
    }

    [MenuItem(kEnableBurst, true)]
    static bool EnableBurstValidate()
    {
        Menu.SetChecked(kEnableBurst, JobsUtility.JobCompilerEnabled && BurstCompilerService.IsInitialized);
        return BurstCompilerService.IsInitialized;
    }
}
