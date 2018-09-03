using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;

public class TwoBoneIK : MonoBehaviour
{
    public Transform effector;
    public Transform shoulder;
    public Transform elbow;
    public Transform hand;

    PlayableGraph m_Graph;
    AnimationScriptPlayable m_IKPlayable;

    void OnEnable()
    {
        m_Graph = PlayableGraph.Create("TwoBoneIK");
        var output = AnimationPlayableOutput.Create(m_Graph, "ouput", GetComponent<Animator>());

        var twoBoneIKJob = new TwoBoneIKJob();
        twoBoneIKJob.Setup(GetComponent<Animator>(), shoulder, elbow, hand, effector);

        m_IKPlayable = AnimationScriptPlayable.Create(m_Graph, twoBoneIKJob);

        output.SetSourcePlayable(m_IKPlayable);
        m_Graph.Play();
    }

    void OnDisable()
    {
        m_Graph.Destroy();
    }
}

public struct TwoBoneIKJob : IAnimationJob
{
    public float time;

    public TransformSceneHandle effector;

    public TransformStreamHandle top;
    public TransformStreamHandle mid;
    public TransformStreamHandle low;

    Vector3 topT;
    Vector3 midT;
    Vector3 lowT;

    Quaternion topQ;
    Quaternion midQ;
    Quaternion lowQ;


    public void Setup(Animator animator, Transform topX, Transform midX, Transform lowX, Transform effectorX)
    {
        top = animator.BindStreamTransform(topX);
        mid = animator.BindStreamTransform(midX);
        low = animator.BindStreamTransform(lowX);

        effector = animator.BindSceneTransform(effectorX);
    }

    public void ProcessRootMotion(AnimationStream stream)
    {
    }

    public void ProcessAnimation(AnimationStream stream)
    {
        Solve(stream, top, mid, low, effector.GetPosition(stream));
    }

    /// <summary>
    /// Returns the angle needed between v1 and v2 so that their extremities are
    /// spaced with a specific length.
    /// </summary>
    /// <returns>The angle between v1 and v2.</returns>
    /// <param name="aLen">The desired length between the extremities of v1 and v2.</param>
    /// <param name="v1">First triangle edge.</param>
    /// <param name="v2">Second triangle edge.</param>
    private static float TriangleAngle(float aLen, Vector3 v1, Vector3 v2)
    {
        float aLen1 = v1.magnitude;
        float aLen2 = v2.magnitude;
        float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
        return Mathf.Acos(c);
    }

    private static void Solve(AnimationStream stream, TransformStreamHandle topHandle, TransformStreamHandle midHandle, TransformStreamHandle lowHandle, Vector3 effector)
    {
        Quaternion aRotation = topHandle.GetRotation(stream);
        Quaternion bRotation = midHandle.GetRotation(stream);
        Quaternion cRotation = lowHandle.GetRotation(stream);

        Vector3 aPosition = topHandle.GetPosition(stream);
        Vector3 bPosition = midHandle.GetPosition(stream);
        Vector3 cPosition = lowHandle.GetPosition(stream);

        Vector3 ab = bPosition - aPosition;
        Vector3 bc = cPosition - bPosition;
        Vector3 ac = cPosition - aPosition;
        Vector3 ad = effector - aPosition;

        float oldAbcAngle = TriangleAngle(ac.magnitude, ab, bc);
        float newAbcAngle = TriangleAngle(ad.magnitude, ab, bc);

        Vector3 axis = Vector3.Cross(ab, bc).normalized;
        float a = 0.5f * (oldAbcAngle - newAbcAngle);
        float sin = Mathf.Sin(a);
        float cos = Mathf.Cos(a);
        Quaternion q = new Quaternion(axis.x * sin, axis.y * sin, axis.z * sin, cos);

        Quaternion worldQ = q * bRotation;
        midHandle.SetRotation(stream, worldQ);

        aRotation = topHandle.GetRotation(stream);
        cPosition = lowHandle.GetPosition(stream);
        ac = cPosition - aPosition;
        Quaternion fromTo = Quaternion.FromToRotation(ac, ad);
        topHandle.SetRotation(stream, fromTo * aRotation);

        lowHandle.SetRotation(stream, cRotation);
    }
}
