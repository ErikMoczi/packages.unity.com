using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.PackageManager.ValidationSuite
{
    public enum ValidationType
    {
        LocalDevelopment,

        VerifiedSet,

        CI,

        Publishing,

        AssetStore
    }
}
