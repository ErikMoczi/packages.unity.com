using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;
using UnityEditor.U2D.Interface;
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class GenericSelector<T>
    {
        public Func<int, bool> Filter;
        public ISelection selection { get; set; }
        public IList<T> items { get; set; }

        public void Select()
        {
            Debug.Assert(Filter != null);

            for (int i = 0; i < items.Count; i++)
            {
                if (Filter(i))
                {
                    selection.Select(i, true);
                }
            }
        }
    }
}
