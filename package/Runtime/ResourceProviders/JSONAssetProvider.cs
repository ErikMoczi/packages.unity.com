using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    public class JsonAssetProvider : RawDataProvider
    {
        public override TObject Convert<TObject>(DownloadHandler handler)
        {
            try
            {
                return JsonUtility.FromJson<TObject>(handler.text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return default(TObject);
            }
        }
    }
}