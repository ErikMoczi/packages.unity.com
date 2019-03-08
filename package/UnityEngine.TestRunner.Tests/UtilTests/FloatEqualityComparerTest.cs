using NUnit.Framework;
using UnityEngine.TestTools.Utils;

[TestFixture]
public class FloatEqualityComparerTest
{
    static readonly object[] k_EqualInputs =
    {
        new object[] {10000000f, 9999990f, new FloatEqualityComparer(10e-6f)},
        new object[] {10e-8f, 0f, new FloatEqualityComparer(10e-6f)},
        new object[] {1.0f, 1.0f, new FloatEqualityComparer(10e-6f)},
        new object[] {0f, 0f, new FloatEqualityComparer(10e-6f)},
        new object[] {-0.00009f, 0.00009f, new FloatEqualityComparer(0.0002f)},
        new object[] {float.PositiveInfinity, float.PositiveInfinity, new FloatEqualityComparer(10e-6f)},
        new object[] {float.NegativeInfinity, float.NegativeInfinity, new FloatEqualityComparer(10e-6f)}
    };

    [Test, TestCaseSource("k_EqualInputs")]
    public void FloatsAreEqual(float actual, float expected, FloatEqualityComparer comparer)
    {
        Assert.That(actual, Is.EqualTo(expected).Using(comparer));
    }

    static readonly object[] k_NotEqualInputs =
    {
        new object[] {1f, -1f, new FloatEqualityComparer(2f - 10e-7f)},
        new object[] {-1f, 1f, new FloatEqualityComparer(2f - 10e-7f)},
        new object[] {float.NegativeInfinity, float.PositiveInfinity, new FloatEqualityComparer(10e-6f)},
        new object[] {float.PositiveInfinity, float.NegativeInfinity, new FloatEqualityComparer(10e-6f)},
        new object[] {10f, 0f, new FloatEqualityComparer(10e-6f)},
        new object[] {0f, 10f, new FloatEqualityComparer(10e-6f)},
        new object[] {5f, 10f, new FloatEqualityComparer(10e-6f)}
    };

    [Test, TestCaseSource("k_NotEqualInputs")]
    public void FloatsAreNotEqual(float actual, float expected, FloatEqualityComparer comparer)
    {
        Assert.That(actual, Is.Not.EqualTo(expected).Using(comparer));
    }

    [Test, TestCaseSource("k_EqualInputs")]
    public void HashCodeIsSameForEqualFloats(float actual, float expected, FloatEqualityComparer comparer)
    {
        Assert.That(comparer.GetHashCode(actual), Is.EqualTo(comparer.GetHashCode(expected)));
    }
}
