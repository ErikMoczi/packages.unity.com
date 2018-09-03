using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;

using UnityEngine.Experimental.Animations;

public class CustomMixer : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    public float swirrle;
    [Range(0.0f, 1.0f)]
    public float rocking;

    PlayableGraph m_Graph;
    AnimationScriptPlayable m_CustomMixerPlayable;

    void OnEnable()
    {
        var upClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Samples/CustomMixer/up.anim");
        var rotateClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Samples/CustomMixer/rotate.anim");

        var animator = GetComponent<Animator>();

        m_Graph = PlayableGraph.Create("CustomMixer");

        m_CustomMixerPlayable = AnimationScriptPlayable.Create(m_Graph, new CustomMixerJob(animator));
        m_CustomMixerPlayable.SetProcessInputs(false);
        m_CustomMixerPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, upClip), 0, 1.0f);
        m_CustomMixerPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, rotateClip), 0, 1.0f);

        var output = AnimationPlayableOutput.Create(m_Graph, "output", animator);
        output.SetSourcePlayable(m_CustomMixerPlayable);

        m_Graph.Play();
    }

    void Update()
    {
        var job = m_CustomMixerPlayable.GetJobData<CustomMixerJob>();

        job.blendPosition = swirrle;
        job.blendRotation = rocking;

        m_CustomMixerPlayable.SetJobData(job);
    }

    void OnDisable()
    {
        m_Graph.Destroy();
    }
}

// Custom mixer idea
// =================
// Multiplier (filter):
//   Can multiply a position curve with another float curve.
//
// Clamper (filter):
//   Limit the position to a limited cube space.

// random mixer

// en parler a pp

public struct CustomMixerJob : IAnimationJob
{
    public float blendPosition;
    public float blendRotation;
    public TransformStreamHandle handle;

    public CustomMixerJob(Animator animator)
    {
        blendPosition = 0.0f;
        blendRotation = 0.0f;
        handle = animator.BindStreamTransform(animator.transform);
    }

    public void ProcessRootMotion(AnimationStream stream)
    {
        AnimationStream streamA = stream.GetInputStream(0);
        AnimationStream streamB = stream.GetInputStream(1);

        Vector3 posA = handle.GetPosition(streamA);
        Vector3 posB = handle.GetPosition(streamB);
        Quaternion rotB = handle.GetRotation(streamB);

        handle.SetPosition(stream, posA + Vector3.Lerp(Vector3.zero, posB, blendPosition));
        handle.SetRotation(stream, Quaternion.Lerp(Quaternion.identity, rotB, blendRotation));
    }

    public void ProcessAnimation(AnimationStream stream)
    {
    }
}
