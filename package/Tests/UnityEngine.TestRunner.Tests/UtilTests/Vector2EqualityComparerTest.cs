using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools.Utils;

[TestFixture]
public class Vector2EqualityComparerTest
{
    static readonly object[] k_EqualInputs =
    {
        new object[] {new Vector2(10e-7f, 10e-7f), new Vector2(0f, 0f), new Vector2EqualityComparer(10e-6f)},
        new object[] {new Vector2(0f, 0f), new Vector2(10e-8f, 10e-8f), new Vector2EqualityComparer(10e-6f)},
        new object[] {new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2EqualityComparer(10e-6f)},
        new object[] {new Vector2(-0.00009f, 10e-8f), new Vector2(-0.00009f, 0f), new Vector2EqualityComparer(10e-6f)},
        new object[] {new Vector2(float.PositiveInfinity, 10e-8f), new Vector2(float.PositiveInfinity, 0f), new Vector2EqualityComparer(10e-6f)}
    };

    [Test, TestCaseSource("k_EqualInputs")]
    public void VectorsAreEqual(Vector2 actual, Vector2 expected, Vector2EqualityComparer comparer)
    {
        Assert.That(actual, Is.EqualTo(expected).Using(comparer));
    }

    static readonly object[] k_NotEqualInputs =
    {
        new object[] {new Vector2(float.NegativeInfinity, 10.01f), new Vector2(float.NegativeInfinity, 10f), new Vector2EqualityComparer(10e-6f)},
        new object[] {new Vector2(10f, float.NegativeInfinity), new Vector2(10.01f, float.NegativeInfinity), new Vector2EqualityComparer(10e-6f)},
        new object[] {new Vector2(float.NegativeInfinity, 10f), new Vector2(float.NegativeInfinity, 10.01f), new Vector2EqualityComparer(10e-6f)},
        new object[] {new Vector2(-0.00009f, 10e-5f), new Vector2(-0.00009f, 0f), new Vector2EqualityComparer(10e-6f)},
        new object[] {new Vector2(float.PositiveInfinity, 10e-5f), new Vector2(float.PositiveInfinity, 0f), new Vector2EqualityComparer(10e-6f)}
    };

    [Test, TestCaseSource("k_NotEqualInputs")]
    public void VectorsAreNotEqual(Vector2 actual, Vector2 expected, Vector2EqualityComparer comparer)
    {
        Assert.That(actual, Is.Not.EqualTo(expected).Using(comparer));
    }

    [Test, TestCaseSource("k_EqualInputs")]
    public void HashCodeIsSameForEqualVector2Objects(Vector2 actual, Vector2 expected, Vector2EqualityComparer comparer)
    {
        Assert.That(comparer.GetHashCode(actual), Is.EqualTo(comparer.GetHashCode(expected)));
    }
}
