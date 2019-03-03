using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools.Utils;

[TestFixture]
public class QuaternionEqualityComparerTest
{
    static readonly object[] k_EqualInputs =
    {
        new object[] {new Quaternion(10f, 10f, 10f, 10f), new Quaternion(10f, 10f, 10f, 10f), new QuaternionEqualityComparer(10e-6f) },
        new object[] {new Quaternion(10f, 0f, 0f, 0f), new Quaternion(1f, 10f, 0f, 0f), new QuaternionEqualityComparer(10e-6f) },
        new object[] {new Quaternion(-10f, 0f, 0f, 0f), new Quaternion(1f, 10f, 0f, 0f), new QuaternionEqualityComparer(10e-6f) }
    };

    [Test, TestCaseSource("k_EqualInputs")]
    public void QuaternionsAreEqual(Quaternion actual, Quaternion expected, QuaternionEqualityComparer comparer)
    {
        Assert.That(actual, Is.EqualTo(expected).Using(comparer));
    }

    static readonly object[] k_NotEqualInputs =
    {
        new object[] {new Quaternion(1f, 1f, 1f, 1f), new Quaternion(0f, 0f, 0f, 0f), new QuaternionEqualityComparer(10e-6f) },
        new object[] {new Quaternion(0f, 0f, 0f, 0f), new Quaternion(0f, 0f, 0f, 0f), new QuaternionEqualityComparer(10e-6f) },
        new object[] {new Quaternion(0.01f, 0f, 0f, 0f), new Quaternion(0.01f, 0f, 0f, 0f), new QuaternionEqualityComparer(10e-6f) }
    };

    [Test, TestCaseSource("k_NotEqualInputs")]
    public void QuaternionsAreNotEqual(Quaternion actual, Quaternion expected, QuaternionEqualityComparer comparer)
    {
        Assert.That(actual, Is.Not.EqualTo(expected).Using(comparer));
    }

    [Test, TestCaseSource("k_EqualInputs")]
    public void HashCodeIsSameForEqualQuaternionObjects(Quaternion actual, Quaternion expected, QuaternionEqualityComparer comparer)
    {
        Assert.That(comparer.GetHashCode(actual), Is.EqualTo(comparer.GetHashCode(expected)));
    }
}
