using System;
using UnityEngine;

namespace UnityEngine.Experimental.U2D.IK
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class Solver2DMenuAttribute : Attribute
    {
        string m_MenuPath;

        public string menuPath
        {
            get { return m_MenuPath; }
        }

        public Solver2DMenuAttribute(string _menuPath)
        {
            m_MenuPath = _menuPath;
        }
    }
}
