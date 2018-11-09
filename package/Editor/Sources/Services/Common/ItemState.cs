using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class ItemState
    {
        [SerializeField] public string PackageId;
        [SerializeField] public bool Expanded;
        [SerializeField] public bool SeeAllVersions;
        [SerializeField] public bool Selected;

        public ItemState(string packageId, bool selected = false)
        {
            PackageId = packageId;
            Selected = selected;
        }
    }
}
