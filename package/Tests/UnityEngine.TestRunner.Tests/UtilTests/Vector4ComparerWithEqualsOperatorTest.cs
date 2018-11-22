using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools.Utils;

[TestFixture]
public class Vector4ComparerWithEqualsOperatorTest
{
    static readonly object[] k_EqualInputs =
    {
        new object[] {new Vector4(0, 0, 1e-6f, 1e-6f), new Vector4(1e-6f, 0f, 0f, 0f)},
        new object[] {new Vector4(0, 0, 1e-6f, 0f), new Vector4(0, 0f, 0f, 0f)},
        new object[] {new Vector4(0, 1e-6f, 0f, 0f), new Vector4(0, 0f, 0f, 0f)},
        new object[] {new Vector4(1e-6f, 0f, 0f, 0f), new Vector4(0, 0f, 0f, 0f)},
        new object[] {new Vector4(10f, 10f, 0f), new Vector4(10f, 10f, 0f, 0f)},
        new object[] {new Vector4(10f, 10f, 0f, 0f), new Vector4(10f, 10f, 0f)},
        new object[] {new Vector4(10f, 10f, 0f, 0f), new Vector4(10f, 10f, 0f, 0f)},
        new object[] {new Vector4(10e-10f, 0f, 0f, 0f), new Vector4(0f, 0f, 0f, 10e-10f)},
        new object[] {new Vector4(0f, 0f, 0f, 0f), new Vector4(0f, 0f, 0f, 0f)},
        new object[] {new Vector4(10f, 0f, 0f, 0f), new Vector4(10f, 0f, 0f, 0f)}
    };

    [Test, TestCaseSource("k_EqualInputs")]
    public void VectorsAreEqual(Vector4 actual, Vector4 expected)
    {
        Assert.That(actual, Is.EqualTo(expected).Using(Vector4ComparerWithEqualsOperator.Instance));
    }

    static readonly object[] k_NotEqualInputs =
    {
        new object[] {new Vector4(1, 0, 0, 0f), new Vector4(-1, 0f, 0f, 0f)},
        new object[] {new Vector4(1e-5f, 0, 0, 0f), new Vector4(0, 0f, 0f)},
        new object[] {new Vector4(1e-5f, 0, 0, 0f), new Vector4(1e-2f, 0f, 0f, 0f)},
        new object[] {new Vector4(1e-5f, 0, 0, 0f), new Vector4(0, 0f, 0f, 0f)}
    };

    [Test, TestCaseSource("k_NotEqualInputs")]
    public void VectorsAreNotEqual(Vector4 actual, Vector4 expected)
    {
        Assert.That(actual, Is.Not.EqualTo(expected).Using(Vector4ComparerWithEqualsOperator.Instance));
    }

    [Test, TestCaseSource("k_EqualInputs")]
    public void HashCodeIsSameForEqualVector4Objects(Vector4 actual, Vector4 expected)
    {
        Assert.That(Vector4ComparerWithEqualsOperator.Instance.GetHashCode(actual), Is.EqualTo(Vector4ComparerWithEqualsOperator.Instance.GetHashCode(expected)));
    }
}
