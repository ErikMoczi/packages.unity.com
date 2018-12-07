
using System;

namespace Unity.Tiny
{
    internal class TinyForceCancellationException : Exception
    {
        public TinyForceCancellationException(string message)
            :base(message)
        {
        }
    }
}
