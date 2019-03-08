using System;

using UnityEngine;

namespace Lumin.Common
{
    public class WaitForIAsyncResult : CustomYieldInstruction
    {
        private IAsyncResult asyncResult;

        public WaitForIAsyncResult(IAsyncResult result)
        {
            asyncResult = result;
        }

        public override bool keepWaiting
        {
            get
            {
                return !asyncResult.IsCompleted;
            }
        }
    }
}
