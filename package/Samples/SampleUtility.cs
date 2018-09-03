using System.Linq;
using UnityEngine;

static class SampleUtility
{
    public static AnimationClip LoadAnimationClipFromFbx(string fbxName, string clipName)
    {
        var clips = Resources.LoadAll<AnimationClip>(fbxName);
        return clips.FirstOrDefault(clip => clip.name == clipName);
    }
}
