
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinySelectionUtility
    {
        public static IRegistryObject[] GetRegistryObjectSelection()
        {
            var items = Selection.instanceIDs.Select(EditorUtility.InstanceIDToObject);
            return Filter(items).ToArray();
        }

        private static IEnumerable<IRegistryObject> Filter(IEnumerable<Object> objects)
        {
            foreach (var obj in objects)
            {
                var go = (GameObject)obj;
                if (null != go)
                {
                    var view = go.GetComponent<TinyEntityView>();
                    if (null != view)
                    {
                        var entity = view.EntityRef.Dereference(view.Registry);
                        if (null != entity)
                        {
                            yield return entity;
                        }
                    }
                }

                // TODO: Include other tiny types as well.
            }
        }
    }
}
