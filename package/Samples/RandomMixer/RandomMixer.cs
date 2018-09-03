using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

using UnityEngine.Experimental.Animations;

public class RandomMixer : MonoBehaviour
{
    public AnimationClip[] clipVariations;
    public float[] clipProbabilities;
    public bool canPlayTwiceInARow;

    PlayableGraph m_Graph;
    AnimationScriptPlayable m_RandomMixerPlayable;

    void OnEnable()
    {
        var animator = GetComponent<Animator>();

        m_Graph = PlayableGraph.Create("CustomMixer");

        int muscleHandlesCount = MuscleHandle.muscleHandleCount;
        MuscleHandle[] muscleHandlesArray = new MuscleHandle[muscleHandlesCount];
        MuscleHandle.GetMuscleHandles(muscleHandlesArray);

        var job = new RandomMixerJob();
        job.muscleHandles = new NativeArray<MuscleHandle>(muscleHandlesArray, Allocator.Persistent);
        //job.inputProbabilities = clipProbabilities;

        m_RandomMixerPlayable = AnimationScriptPlayable.Create(m_Graph, job);
        m_RandomMixerPlayable.SetProcessInputs(false);

        foreach (var clip in clipVariations)
        {
            m_RandomMixerPlayable.AddInput(AnimationClipPlayable.Create(m_Graph, clip), 0, 1.0f);
        }

        var output = AnimationPlayableOutput.Create(m_Graph, "output", animator);
        output.SetSourcePlayable(m_RandomMixerPlayable);

        m_Graph.Play();
    }

    void Update()
    {
        //var job = m_CustomMixerPlayable.GetJobData<RandomMixerJob>();

        //job.blendPosition = swirrle;
        //job.blendRotation = rocking;

        //m_CustomMixerPlayable.SetJobData(job);
    }

    void OnDisable()
    {
        m_Graph.Destroy();
    }
}

public struct RandomMixerJob : IAnimationJob
{
    //public float[] inputProbabilities;

    //public float blendPosition;
    //public float blendRotation;
    //public TransformStreamHandle handle;
    public NativeArray<MuscleHandle> muscleHandles;

    private int currentClip;

    //public RandomMixerJob(Animator animator)
    //{
    //    blendPosition = 0.0f;
    //    blendRotation = 0.0f;
    //    handle = animator.BindTransform(animator.transform);
    //}

    public void ProcessRootMotion(AnimationStream stream)
    {
        AnimationStream streamA = stream.GetInputStream(0);
        AnimationStream streamB = stream.GetInputStream(1);

        //Vector3 posA = streamA.GetPosition(handle);
        //Vector3 posB = streamB.GetPosition(handle);
        //Quaternion rotB = streamB.GetRotation(handle);

        //stream.SetPosition(handle, posA + Vector3.Lerp(Vector3.zero, posB, blendPosition));
        //stream.SetRotation(handle, Quaternion.Lerp(Quaternion.identity, rotB, blendRotation));

        //if (endOfClip)
        //{
        //    SelectNextClip(stream.inputCount);
        //}
    }

    public void ProcessAnimation(AnimationStream stream)
    {
    }

    private void SelectNextClip(int numInputs)
    {
        currentClip = (currentClip + 1) % numInputs;
    }
}
