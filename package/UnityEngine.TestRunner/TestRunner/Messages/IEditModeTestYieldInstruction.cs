using System.Collections;

namespace UnityEngine.TestTools
{
    internal interface IEditModeTestYieldInstruction
    {
        bool ExpectDomainReload { get; }
        bool ExpectedPlaymodeState { get; }

        IEnumerator Perform();
    }
}
