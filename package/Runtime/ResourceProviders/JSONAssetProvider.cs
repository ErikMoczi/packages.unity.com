using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Converts JSON serialized text into the requested object.
    /// </summary>
    public class JsonAssetProvider : RawDataProvider
    {
        /// <summary>
        /// Converts raw text into requested object type via JSONUtility.FromJson.
        /// </summary>
        /// <typeparam name="TObject">Object type.</typeparam>
        /// <param name="handler">The handler with the text to convert.</param>
        /// <returns>Converted object of type TObject.</returns>
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