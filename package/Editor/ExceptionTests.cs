using System;
using System.Collections;
using FrameworkTests;

public class ExceptionTests
{
    // covers case 1111243
    [UnityTestExpectedToFail]
    public IEnumerator ExceptionTestWithEnumeratorPasses()
    {
        return new TestEnumerator();
    }

    class TestEnumerator : IEnumerator
    {
        public bool MoveNext()
        {
            throw new NotImplementedException("IEnumerator.MoveNext() is not implemented");
        }

        public void Reset()
        {
            throw new NotImplementedException("IEnumerator.Reset() is not implemented");
        }

        public object Current { get; }
    }
}
