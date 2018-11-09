using System;
using Boo.Lang;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Information to store to survive domain reload
    /// </summary>
    [Serializable]
    internal class SelectionManager
    {
        [SerializeField] private Selection ListSelection = new Selection();
        [SerializeField] private Selection SearchSelection = new Selection();
        [SerializeField] private Selection BuiltInSelection = new Selection();

        private PackageCollection Collection { get; set; }

        public Selection Selection
        {
            get
            {
                if (Collection == null)
                    return ListSelection;

                if (Collection.Filter == PackageFilter.All)
                    return SearchSelection;
                if (Collection.Filter == PackageFilter.Local)
                    return ListSelection;
                return BuiltInSelection;
            }
        }

        public void SetCollection(PackageCollection collection)
        {
            Collection = collection;
            ListSelection.SetCollection(collection);
            SearchSelection.SetCollection(collection);
            BuiltInSelection.SetCollection(collection);
        }

        public void ClearAll()
        {
            ListSelection.ClearSelection();
            SearchSelection.ClearSelection();
            BuiltInSelection.ClearSelection();
        }
    }
}
