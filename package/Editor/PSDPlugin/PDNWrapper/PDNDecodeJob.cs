#if ENABLE_JOBS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Collections;
using UnityEngine.Jobs;

struct PDNDecodeJobOut
{
    public int width;
    public int height;
    NativeArray<byte> buffer;
}

public class PDNDecodeJob : IJob
{
    PDNDecodeJobOut output;
    public void Execute()
    {
    }
}
#endif
