using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    public class TextDataProvider : RawDataProvider
    {
        public override TObject Convert<TObject>(DownloadHandler handler)
        {
            return handler.text as TObject;
        }
    }
}