using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools.Utils;

[TestFixture]
public class Vector3EqualityComparerTest
{
    static readonly object[] k_EqualInputs =
    {
        new object[] {new Vector3(10e-8f, 10e-8f, 10e-8f), new Vector3(0f, 0f, 0f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(0f, 0f, 0f), new Vector3(10e-8f, 10e-8f, 10e-8f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(-0.00009f, 10e-8f, 0f), new Vector3(-0.00009f, 0f, 10e-8f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(float.PositiveInfinity, 10e-8f, 0f), new Vector3(float.PositiveInfinity, 0f, 10e-8f), new Vector3EqualityComparer(10e-6f)}
    };

    [Test, TestCaseSource("k_EqualInputs")]
    public void VectorsAreEqual(Vector3 actual, Vector3 expected, Vector3EqualityComparer comparer)
    {
        Assert.That(actual, Is.EqualTo(expected).Using(comparer));
    }

    static readonly object[] k_NotEqualInputs =
    {
        new object[] {new Vector3(5f, 10f, 0f), new Vector3(5f, 10f, 10e-5f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(0f, 1f, 0f), new Vector3(0f, -1f, 0f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(float.PositiveInfinity, 0f, 0f), new Vector3(float.NegativeInfinity, 0f, 0f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(0f, float.NegativeInfinity, 0f), new Vector3(float.NegativeInfinity, 0f, 0f), new Vector3EqualityComparer(10e-6f)},
        new object[] {new Vector3(float.NegativeInfinity, 0f, 0f), new Vector3(float.PositiveInfinity, 0f, 0f), new Vector3EqualityComparer(10e-6f)}
    };

    [Test, TestCaseSource("k_NotEqualInputs")]
    public void VectorsAreNotEqual(Vector3 actual, Vector3 expected, Vector3EqualityComparer comparer)
    {
        Assert.That(actual, Is.Not.EqualTo(expected).Using(comparer));
    }

    [Test, TestCaseSource("k_EqualInputs")]
    public void HashCodeIsSameForEqualVector3Objects(Vector3 actual, Vector3 expected, Vector3EqualityComparer comparer)
    {
        Assert.That(comparer.GetHashCode(actual), Is.EqualTo(comparer.GetHashCode(expected)));
    }
}
