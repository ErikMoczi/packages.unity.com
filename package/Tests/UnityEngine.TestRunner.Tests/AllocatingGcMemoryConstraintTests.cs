using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

public class AllocatingGCMemoryConstraintTests
{
    [Test]
    public void WithSimpleAllocation_ConstraintPasses()
    {
        Assert.That(() =>
            Assert.That(() =>
            {
                int a = new int[500].Length;
            }, Is.AllocatingGCMemory()),
            Throws.Nothing);
    }

    [Test]
    public void WithNoAllocation_ConstraintFails()
    {
        Assert.That(() =>
            Assert.That(() =>
            {
                #pragma warning disable 0219
                int a = 1;
                #pragma warning restore 0219
            }, Is.AllocatingGCMemory()),
            Throws.InstanceOf<AssertionException>()
                .With.Message.Contains("The provided delegate did not make any GC allocations."));
    }

    [Test]
    public void WhenNegated_WithSimpleAllocation_ConstraintFails()
    {
        Assert.That(() =>
            Assert.That(() =>
            {
                int a = new int[500].Length;
            }, Is.Not.AllocatingGCMemory()),
            Throws.InstanceOf<AssertionException>()
                .With.Message.Contains("not allocates GC memory"));
    }

    [Test]
    public void WhenNegated_WithNoAllocation_ConstraintPasses()
    {
        Assert.That(() =>
            Assert.That(() =>
            {
                #pragma warning disable 0219
                int a = 1;
                #pragma warning restore 0219
            }, Is.Not.AllocatingGCMemory()),
            Throws.Nothing);
    }

    [Test]
    public void WithNullDelegate_ThrowsArgumentNullException()
    {
        Assert.That(() =>
        {
            Assert.That(null, Is.AllocatingGCMemory());
        }, Throws.ArgumentNullException);
    }

    [Test]
    public void WithNonDelegateType_ThrowsArgumentException()
    {
        Assert.That(() =>
        {
            Assert.That("test", Is.AllocatingGCMemory());
        }, Throws.ArgumentException);
    }
}
