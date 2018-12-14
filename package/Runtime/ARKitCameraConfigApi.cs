using System;
using UnityEngine.XR.ARExtensions;

namespace UnityEngine.XR.ARKit
{
    internal class ARKitCameraConfigApi : ICameraConfigApi
    {
        public int GetConfigurationCount()
        {
            return Api.UnityARKit_cameraImage_getConfigurationCount();
        }

        public CameraConfiguration GetConfiguration(int index)
        {
            CameraConfiguration configuration;
            if (!Api.UnityARKit_cameraImage_tryGetConfiguration(index, out configuration))
                throw new IndexOutOfRangeException(string.Format(
                    "Configuration index {0} is out of range", index));

            return configuration;
        }

        public CameraConfiguration currentConfiguration
        {
            get
            {
                CameraConfiguration configuration;
                if (!Api.UnityARKit_cameraImage_tryGetCurrentConfiguration(out configuration))
                    throw new InvalidOperationException("Configuration could not be retrieved");

                return configuration;
            }
            set
            {
                if (!Api.UnityARKit_cameraImage_trySetConfiguration(value))
                    throw new InvalidOperationException("Invalid camera image configuration.");
            }
        }
    }
}
