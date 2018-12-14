
using System.Collections.Generic;
using UnityEditor;
using Unity.Properties;

namespace Unity.Tiny
{
    internal interface IInspectorBackend
    {
        InspectorMode Mode { get; set; }
        List<IPropertyContainer> Targets { get; set; }

        bool Locked { get; set; }
        bool ShowFamilies { get; set; }

        void OnGUI();

        void Build();
    }
}

