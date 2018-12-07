

using System;

namespace Unity.Tiny
{
    internal class TinyCoroutineInnerException : Exception
    {
        public Exception Inner { get; private set; }

        public TinyCoroutineInnerException(Exception ex)
        {
            Inner = ex;
        }
    }
}

