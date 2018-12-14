using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// This node is backed by the UnityEngine transform hierarchy
    /// </summary>
    internal interface ITransformNode
    {
        Transform Transform { get; }
    }
}