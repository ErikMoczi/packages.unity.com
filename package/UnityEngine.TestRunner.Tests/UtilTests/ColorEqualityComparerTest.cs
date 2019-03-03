using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools.Utils;

[TestFixture]
public class ColorEqualityComparerTest
{
    static readonly object[] k_EqualInputs =
    {
        new object[] {new Color(0f, 0f, 0f, 1f), new Color(0f, 0f, 0f, 1f), new ColorEqualityComparer(0.0001f)},
        new object[] {new Color(0f, 0f, 0f, 1f), new Color(10e-6f, 0f, 0f, 1f), new ColorEqualityComparer(10e-5f)},
        new object[] {new Color(0f, 0f, 0f), new Color(-10e-6f, 0f, 0f, 1f), new ColorEqualityComparer(10e-5f)},
        new object[] {new Color(1f, 0f, 0f, 0f), new Color(-1f, 0f, 0f, 0f), new ColorEqualityComparer(2f + 1e-5f)}
    };

    [Test, TestCaseSource("k_EqualInputs")]
    public void ColorsAreEqual(Color actual, Color expected, ColorEqualityComparer comparer)
    {
        Assert.That(actual, Is.EqualTo(expected).Using(comparer));
    }

    static readonly object[] k_NotEqualInputs =
    {
        new object[] {new Color(0f, 5f, 0f), new Color(-10e-6f, 10f, 0f, 1f), new ColorEqualityComparer(10e-5f)},
        new object[] {new Color(-11f, 0f, 0f, 0f), new Color(1f, 0f, 0f, 0f), new ColorEqualityComparer(2f - 10e-7f)}
    };

    [Test, TestCaseSource("k_NotEqualInputs")]
    public void ColorsAreNotEqual(Color actual, Color expected, ColorEqualityComparer comparer)
    {
        Assert.That(actual, Is.Not.EqualTo(expected).Using(comparer));
    }

    [Test, TestCaseSource("k_EqualInputs")]
    public void HashCodeIsSameForEqualColorObjects(Color actual, Color expected, ColorEqualityComparer comparer)
    {
        Assert.That(comparer.GetHashCode(actual), Is.EqualTo(comparer.GetHashCode(expected)));
    }
}
