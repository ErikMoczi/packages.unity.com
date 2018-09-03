using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    public class RectSlider
    {
        static int s_ControlID = -1;
        static Vector2 s_StartPosition = Vector2.zero;
        static Vector2 s_EndPosition = Vector2.zero;
        static Rect s_currentRect = new Rect(0f, 0f, 0f, 0f);

        static internal Rect Do()
        {
            if (s_ControlID == -1)
                s_ControlID = GUIUtility.GetControlID("RectSlider".GetHashCode(), FocusType.Passive);

            return Do(s_ControlID);
        }

        static internal Rect Do(int controlID)
        {
            EventType eventType = Event.current.GetTypeForControl(controlID);

            if (eventType == EventType.MouseDown)
            {
                s_StartPosition = GUIToWorld(Event.current.mousePosition);
                s_EndPosition = s_StartPosition;
                s_currentRect.position = s_StartPosition;
                s_currentRect.size = Vector2.zero;
            }

            if (eventType == EventType.Layout)
                HandleUtility.AddDefaultControl(controlID);

            s_EndPosition = Slider2D.Do(controlID, s_EndPosition);

            s_currentRect.min = s_StartPosition;
            s_currentRect.max = s_EndPosition;

            return s_currentRect;
        }

        static internal Vector3 GUIToWorld(Vector3 guiPosition)
        {
            return GUIToWorld(guiPosition, Vector3.forward, Vector3.zero);
        }

        static internal Vector3 GUIToWorld(Vector3 guiPosition, Vector3 planeNormal, Vector3 planePos)
        {
            Vector3 worldPos = Handles.inverseMatrix.MultiplyPoint(guiPosition);

            if (Camera.current)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

                planeNormal = Handles.matrix.MultiplyVector(planeNormal);

                planePos = Handles.matrix.MultiplyPoint(planePos);

                Plane plane = new Plane(planeNormal, planePos);

                float distance = 0f;

                if (plane.Raycast(ray, out distance))
                {
                    worldPos = Handles.inverseMatrix.MultiplyPoint(ray.GetPoint(distance));
                }
            }

            return worldPos;
        }
    }
}
