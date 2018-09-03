using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IBoneHierarchyView
    {
        void SetRect(Rect rect);

        void Refresh();
        bool HandleFullViewCursor(ref Vector3 mousePosition);
        void ShowCreationCursor();

        void DrawBone(IBone bone, bool selected);
        void DrawLinkToParent(IBone bone, bool selected);
        void DrawPreviewLinkFromBone(IBone bone);
        void DrawPreviewTipFromTip(IBone bone);
        void DrawTip(IBone bone);

        bool HandleBoneSelect(IBone bone);
        bool HandleTipSelect(IBone bone);
        Vector3 HandleBoneNodeDrag(IBone bone);
        Vector3 HandleBoneTipDrag(IBone bone);
        Vector3 HandleBoneDrag(IBone bone);

        float GetBoneRadius();
    }

    internal class BoneHierarchyView : IBoneHierarchyView
    {
        private Dictionary<IBone, int> m_NodeControlIDMaps = new Dictionary<IBone, int>();
        private Dictionary<IBone, int> m_TipControlIDMaps = new Dictionary<IBone, int>();
        private Dictionary<IBone, int> m_BoneControlIDMaps = new Dictionary<IBone, int>();
        
        private Rect m_WorkingRect;
        
        protected virtual Color color
        {
            get
            {
                // TODO : Need a way to customize color property
                return Color.white;
            }
        }

        protected virtual float GetScale()
        {
            return 1.0f;
        }

        protected virtual float GetScale(Vector3 startPosition, Vector3 endPosition)
        {
            return 1.0f;
        }

        protected virtual Vector3 GetMouseWorldPosition(float z = 0.0f)
        {
            return Handles.inverseMatrix.MultiplyPoint(Event.current.mousePosition);
        }

        protected virtual void GetSliderDirection(out Vector3 forward, out Vector3 up, out Vector3 right)
        {
            forward = Vector3.forward;
            up = Vector3.up;
            right = Vector3.right;
        }

        public virtual void Refresh()
        {
            GUI.changed = true;
        }

        public virtual bool HandleFullViewCursor(ref Vector3 v)
        {
            var currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                v = GetMouseWorldPosition();
                if (m_WorkingRect.Contains(v))
                {
                    currentEvent.Use();
                    return true;
                }
            }

            return false;
        }

        public virtual void ShowCreationCursor()
        {
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                var min = Handles.matrix.MultiplyPoint(new Vector2(m_WorkingRect.xMin, m_WorkingRect.yMin));
                var max = Handles.matrix.MultiplyPoint(new Vector2(m_WorkingRect.xMax, m_WorkingRect.yMax));
                var texRect = new Rect(min.x, max.y, max.x - min.x, min.y - max.y);

                EditorGUIUtility.AddCursorRect(texRect, MouseCursor.ArrowPlus);
            }
        }

        public virtual void SetRect(Rect rect)
        {
            m_NodeControlIDMaps.Clear();
            m_BoneControlIDMaps.Clear();
            m_TipControlIDMaps.Clear();

            m_WorkingRect = rect;
        }

        private int GetBoneNodeControlID(IBone bone)
        {
            return GetControlID(bone, "Node".GetHashCode(), m_NodeControlIDMaps);
        }

        private int GetBoneTipControlID(IBone bone)
        {
            return GetControlID(bone, "Tip".GetHashCode(), m_TipControlIDMaps);
        }

        private int GetBoneControlID(IBone bone)
        {
            return GetControlID(bone, "Bone".GetHashCode(), m_BoneControlIDMaps);
        }

        private int GetControlID(IBone bone, int hint, Dictionary<IBone, int> container)
        {
            int id = -1;
            if (!container.TryGetValue(bone, out id))
            {
                if (Event.current != null && Event.current.type != EventType.Used)
                {
                    id = GUIUtility.GetControlID(hint, FocusType.Keyboard);
                    container.Add(bone, id);
                    Debug.Assert(id != -1, "Failed to get new control ID");
                }
            }

            return id;
        }

        public bool HandleBoneSelect(IBone bone)
        {
            var id = GetBoneNodeControlID(bone);
            var boneID = GetBoneControlID(bone);
            var currentEvent = Event.current;

            if (currentEvent != null && currentEvent.type == EventType.Layout)
            {
                HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(bone.position, GetBoneRadius() * 0.4f));
                HandleUtility.AddControl(boneID, HandleUtility.DistanceToLine(bone.position, bone.tip));
            }

            if (currentEvent != null && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                if (HandleUtility.nearestControl == id || HandleUtility.nearestControl == boneID)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HandleTipSelect(IBone bone)
        {
            var id = GetBoneTipControlID(bone);
            var currentEvent = Event.current;

            if (currentEvent != null && currentEvent.type == EventType.Layout)
                HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(bone.tip, GetBoneRadius() * 0.2f));

            if (currentEvent != null && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                if (HandleUtility.nearestControl == id)
                {
                    return true;
                }
            }
            return false;
        }

        public Vector3 HandleBoneNodeDrag(IBone bone)
        {
            var id = GetBoneNodeControlID(bone);

            Vector3 forward;
            Vector3 up;
            Vector3 right;
            GetSliderDirection(out forward, out up, out right);

            EditorGUI.BeginChangeCheck();
            var newPosition = Handles.Slider2D(id, bone.position, Vector3.zero, forward, up, right, HandleUtility.GetHandleSize(bone.position), FakeCap, Vector2.zero);
            if (EditorGUI.EndChangeCheck())
                return newPosition;
            return Vector3.zero;
        }

        public Vector3 HandleBoneTipDrag(IBone bone)
        {
            var id = GetBoneTipControlID(bone);

            Vector3 forward;
            Vector3 up;
            Vector3 right;
            GetSliderDirection(out forward, out up, out right);

            EditorGUI.BeginChangeCheck();
            var newPosition = Handles.Slider2D(id, bone.tip, Vector3.zero, forward, up, right, HandleUtility.GetHandleSize(bone.tip), FakeCap, Vector2.zero);
            if (EditorGUI.EndChangeCheck())
                return newPosition;
            return Vector3.zero;
        }

        public Vector3 HandleBoneDrag(IBone bone)
        {
            var id = GetBoneControlID(bone);

            Vector3 forward;
            Vector3 up;
            Vector3 right;
            GetSliderDirection(out forward, out up, out right);

            EditorGUI.BeginChangeCheck();
            var newPosition = Handles.Slider2D(id, bone.position, Vector3.zero, forward, up, right, HandleUtility.GetHandleSize(bone.position), FakeCap, Vector2.zero);
            if (EditorGUI.EndChangeCheck())
                return newPosition;
            return Vector3.zero;
        }

        public float GetBoneRadius()
        {
            return BoneDrawingUtility.GetBoneRadius(GetScale());
        }

        public void DrawBone(IBone bone, bool selected)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            var scale = GetScale(bone.position, bone.tip);

            BoneDrawingUtility.DrawBoneNodeOutline(bone.position, selected ? Color.yellow : color, scale);
            BoneDrawingUtility.DrawBoneBody(bone.position, bone.tip, selected ? Color.yellow : color, scale);
            BoneDrawingUtility.DrawBoneNode(bone.position, selected ? Color.green : Color.black, scale);
        }

        public void DrawLinkToParent(IBone bone, bool selected)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            BoneDrawingUtility.DrawParentLink(bone.position, bone.parent.tip, selected ? Color.yellow : color, GetScale());
        }

        public void DrawPreviewLinkFromBone(IBone bone)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            var mousePosition = GetMouseWorldPosition();

            BoneDrawingUtility.DrawParentLink(bone.tip, mousePosition, Color.green, GetScale());
        }

        public void DrawPreviewTipFromTip(IBone bone)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            var mousePosition = GetMouseWorldPosition();
            var scale = GetScale(bone.tip, mousePosition);

            BoneDrawingUtility.DrawBoneBody(bone.tip, mousePosition, Color.green, scale);
            BoneDrawingUtility.DrawBoneNodeOutline(bone.tip, Color.green, scale);
        }

        public void DrawTip(IBone bone)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            BoneDrawingUtility.DrawBoneNodeOutline(bone.tip, Color.cyan, GetScale() * 0.2f);
        }

        private void FakeCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            // Emptied to work around Handles.Slider2D's AddControl() during Event.Layout.
        }
    }
}
