using System;
using UnityEngine;

namespace UnityEditor.AI.Planner.Utility
{
    static class StringExtensions
    {
        public static void CopyToClipboard(this string s)
        {
            var te = new TextEditor();
            te.text = s;
            te.SelectAll();
            te.Copy();
        }
    }
}
