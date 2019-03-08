using NUnit.Framework;
using UnityEngine.TestTools.Utils;

[TestFixture]
public class UtilTests
{
    static readonly object[] k_EqualInputs =
    {
        new object[] {10000000f, 9999990f, 10e-6f},
        new object[] {10e-8f, 0f, 10e-6f},
        new object[] {1.0f, 1.0f, 10e-6f},
        new object[] {0f, 0f, 10e-6f},
        new object[] {-0.00009f, 0.00009f, 0.0002f},
        new object[] {float.PositiveInfinity, float.PositiveInfinity, 10e-6f},
        new object[] {float.NegativeInfinity, float.NegativeInfinity, 10e-6f},
    };

    [Test, TestCaseSource("k_EqualInputs")]
    public void AreFloatsEqualTrue(float expected, float actual, float error)
    {
        Assert.That(Utils.AreFloatsEqual(expected, actual, error), Is.True);
    }

    static readonly object[] k_NotEqualInputs =
    {
        new object[] {1f, -1f, 2f - 10e-7f},
        new object[] {-1f, 1f, 2f - 10e-7f},
        new object[] {float.NegativeInfinity, float.PositiveInfinity, 10e-6f},
        new object[] {float.PositiveInfinity, float.NegativeInfinity, 10e-6f},
        new object[] {10f, 0f, 10e-6f},
        new object[] {0f, 10f, 10e-6f},
        new object[] {5f, 10f, 10e-6f},
    };

    [Test, TestCaseSource("k_NotEqualInputs")]
    public void AreFloatsEqualFalse(float expected, float actual, float error)
    {
        Assert.That(Utils.AreFloatsEqual(expected, actual, error), Is.False);
    }

    static readonly object[] k_AbsoluteErrorEqualInputs =
    {
        new object[] {1f, -1f, 2f + 1e-5f},
        new object[] {-1f, 1f, 2f + 1e-5f},
        new object[] {0f, 10e-6f, 10e-5f},
        new object[] {0f, -10e-6f, 10e-5f},
    };

    [Test, TestCaseSource("k_AbsoluteErrorEqualInputs")]
    public void AreFloatsEqualAbsoluteErrorTrue(float expected, float actual, float error)
    {
        Assert.That(Utils.AreFloatsEqualAbsoluteError(expected, actual, error), Is.True);
    }

    static readonly object[] k_AbsoluteErrorNotEqualInputs =
    {
        new object[] {5f, 10f, 10e-6f},
        new object[] {1f, -1f, 2f - 10e-7f},
        new object[] {-1f, 1f, 2f - 10e-7f},
    };

    [Test, TestCaseSource("k_AbsoluteErrorNotEqualInputs")]
    public void AreFloatsEqualAbsoluteErrorFalse(float expected, float actual, float error)
    {
        Assert.That(Utils.AreFloatsEqualAbsoluteError(expected, actual, error), Is.False);
    }
}
