using UnityEngine;
using UnityEngine.TestTools.Utils;
using NUnit.Framework;

[TestFixture]
public class Vector4EqualityComparerTest
{
    static readonly object[] k_EqualInputs =
    {
        new object[] {new Vector4(0, 0, 1e-6f, 1e-6f), new Vector4(1e-6f, 0f, 0f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(0, 0, 1e-6f, 0f), new Vector4(0, 0f, 0f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(0, 1e-6f, 0f, 0f), new Vector4(0, 0f, 0f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(1e-6f, 0f, 0f, 0f), new Vector4(0, 0f, 0f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(10f, 10f, 0f), new Vector4(10f, 10f, 0f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(10f, 10f, 0f, 0f), new Vector4(10f, 10f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(10f, 10f, 0f, 0f), new Vector4(10f, 10f, 0f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(10e-10f, 0f, 0f, 0f), new Vector4(0f, 0f, 0f, 10e-10f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(0f, 0f, 0f, 0f), new Vector4(0f, 0f, 0f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(10f, 0f, 0f, 0f), new Vector4(10f, 0f, 0f, 0f), new Vector4EqualityComparer(10e-6f)}
    };

    [Test, TestCaseSource("k_EqualInputs")]
    public void VectorsAreEqual(Vector4 actual, Vector4 expected, Vector4EqualityComparer comparer)
    {
        Assert.That(actual, Is.EqualTo(expected).Using(comparer));
    }

    static readonly object[] k_NotEqualInputs =
    {
        new object[] {new Vector4(0, 0, 1e-4f, 1e-6f), new Vector4(1e-6f, 0f, 0f, 0f), new Vector4EqualityComparer(10e-6f)},
        new object[] {new Vector4(1, 0, 0, 0f), new Vector4(-1, 0f, 0f, 0f), new Vector4EqualityComparer(10e-3f)},
        new object[] {new Vector4(10e-5f, 0, 0, 0f), new Vector4(1f, 0f, 0f, 0f), new Vector4EqualityComparer(10e-8f)},
        new object[] {new Vector4(1e-5f, 0, 0, 0f), new Vector4(1e-2f, 0f, 0f, 0f), new Vector4EqualityComparer(10e-3f)},
        new object[] {new Vector4(10e-2f, 0, 0, 0f), new Vector4(0, 0f, 0f, 0f), new Vector4EqualityComparer(10e-7f)}
    };

    [Test, TestCaseSource("k_NotEqualInputs")]
    public void VectorsAreNotEqual(Vector4 actual, Vector4 expected, Vector4EqualityComparer comparer)
    {
        Assert.That(actual, Is.Not.EqualTo(expected).Using(comparer));
    }

    [Test, TestCaseSource("k_EqualInputs")]
    public void HashCodeIsSameForEqualVector4Objects(Vector4 actual, Vector4 expected, Vector4EqualityComparer comparer)
    {
        Assert.That(comparer.GetHashCode(actual), Is.EqualTo(comparer.GetHashCode(expected)));
    }
}
