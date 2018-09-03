using Unity.Burst.LowLevel;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;

class BurstMenu
{
    public const string kEnableBurstCompilation = "Jobs/Enable Burst Compilation";

    [MenuItem(kEnableBurstCompilation, false)]
    static void EnableBurstCompilation()
    {
        EditorPrefs.SetBool(kEnableBurstCompilation, !EditorPrefs.GetBool(kEnableBurstCompilation, true));
    }

    [MenuItem(kEnableBurstCompilation, true)]
    static bool EnableBurstCompilationValidate()
    {
        Menu.SetChecked(kEnableBurstCompilation, EditorPrefs.GetBool(kEnableBurstCompilation, true));
        return BurstCompilerService.IsInitialized;
    }
    
    static bool IsBurstEnabled()
    {
        return BurstCompilerService.IsInitialized && EditorPrefs.GetBool(kEnableBurstCompilation, true);
    }

    public const string kEnableSafetyChecks = "Jobs/Enable Burst Safety Checks";
    [MenuItem(kEnableSafetyChecks, false)]
    static void EnableBurstSafetyChecks()
    {
        EditorPrefs.SetBool(kEnableSafetyChecks, !EditorPrefs.GetBool(kEnableSafetyChecks, true));
    }

    [MenuItem(kEnableSafetyChecks, true)]
    static bool EnableBurstSafetyChecksValidate()
    {
        Menu.SetChecked(kEnableSafetyChecks, EditorPrefs.GetBool(kEnableSafetyChecks, true));
        return IsBurstEnabled();
    }

    public const string kEnableBurst = "Jobs/Use Burst Jobs";
    [MenuItem(kEnableBurst, false)]
    static void UseBurstJobs()
    {
        JobsUtility.JobCompilerEnabled = !JobsUtility.JobCompilerEnabled;
    }

    [MenuItem(kEnableBurst, true)]
    static bool UseBurstJobsValidate()
    {
        Menu.SetChecked(kEnableBurst, JobsUtility.JobCompilerEnabled && BurstCompilerService.IsInitialized);
        return IsBurstEnabled();
    }
}
