using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline
{
    public class BuildSpriteData : IBuildSpriteData
    {
        public Dictionary<GUID, SpriteImporterData> ImporterData { get; private set; }

        public BuildSpriteData()
        {
            ImporterData = new Dictionary<GUID, SpriteImporterData>();
        }
    }
}
