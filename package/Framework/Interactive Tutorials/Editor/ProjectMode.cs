using System;

namespace Unity.InteractiveTutorials
{
    public static class ProjectMode
    {
        public static bool IsAuthoringMode()
        {
            return Type.GetType("Unity.InteractiveTutorials.TutorialExporterWindow, Assembly-CSharp-Editor-firstpass-testable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") != null;
        }
    }
}
