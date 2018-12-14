using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal struct IMGUIPrefabOverrideScope : IDisposable
    {
        public IMGUIPrefabOverrideScope(Rect rect, int minHeight = 0)
        {
            TinyEditorUtility.DrawOverrideBackground(rect);
        }

        public void Dispose()
        {
        }
    }
}