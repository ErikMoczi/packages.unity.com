

using UnityEngine;
using UnityEditor;
using System;

namespace Unity.Tiny
{
    internal class Separator : IDrawable, IDirtyable
    {
        #region Types
        
        public enum SeparatorOrientation
        {
            Vertical,
            Horizontal
        }
        
        [Serializable]
        internal class State
        {
            public float Position = 350f;
            public SeparatorOrientation Orientation = SeparatorOrientation.Vertical;
            public bool Draggable = true;
        }
        
        #endregion
        
        #region Constant
        
        private static readonly Color SelectedColor = new Color(0.2f, 0.6f, 1);
        private static readonly Color UnselectedColor = new Color(0.35f, 0.35f, 0.35f);
        
        #endregion
        
        #region Fields

        private readonly State m_State;
        
        private float m_Position;
        private bool m_Drag;
        private Vector2 m_MousePosition;
        
        #endregion
        
        #region Properties
        
        public IDrawable Left { get; set; }

        public IDrawable Right { get; set; }

        public float Position
        {
            get
            {
                return m_Position; 
            }
            set
            {
                m_State.Position = m_Position = value; 
            }
        }

        /// <summary>
        /// @TODO Integrate orienatation
        /// </summary>
        public SeparatorOrientation Orientation
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public bool Draggable
        {
            get { return m_State.Draggable; }
            set { m_State.Draggable = value; }
        }

        /// <summary>
        /// Minimum size for the left divison (in pixels)
        /// </summary>
        public float MinLeft { get; set; }

        /// <summary>
        /// Minimum size for the right division (in pixels)
        /// </summary>
        public float MinRight { get; set; }
        
        #endregion
        
        #region Public Methods

        public Separator(IDrawable left, IDrawable right, State state)
        {
            Left = left;
            Right = right;
            m_State = state;
            m_Position = state.Position;
        }
        
        public bool DrawLayout()
        {
            var repaint = false;
            Rect rect;
            
            using (var area = new EditorGUILayout.HorizontalScope())
            {
                rect = area.rect;
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Position)))
                {
                    repaint |= Left.DrawLayout();
                }

                if (Draggable)
                {
                    using (var scope = new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(3), GUILayout.ExpandHeight(true)))
                    {
                        // @SUPER HACK To fill in the gap between toolbars
                        // At the moment this class is only used in one way, this is a workaround for a graphical issue
                        if (Event.current.type == EventType.Repaint)
                        {
                            var toolbarRect = scope.rect;
                            toolbarRect.height = 18f;
                            toolbarRect.yMin += 1;
                            EditorStyles.toolbar.Draw(toolbarRect, false, false, false, false);
                        }
                        
                        GUILayout.FlexibleSpace();

                        using (new TinyGUIColorScope(m_Drag ? SelectedColor : UnselectedColor))
                        {
                            GUI.DrawTexture(scope.rect, TinyIcons.SeparatorVertical, ScaleMode.StretchToFill);
                        }
                        
                        EditorGUIUtility.AddCursorRect(scope.rect, MouseCursor.ResizeHorizontal);
                
                        if (Event.current.type == EventType.MouseDown && 
                            Event.current.button == 0 &&
                            scope.rect.Contains(Event.current.mousePosition))
                        {
                            m_Drag = true;
                        }
                    }
                }
                
                using (new EditorGUILayout.VerticalScope())
                {
                    repaint |= Right.DrawLayout();
                }
            }
            
            if (Event.current.type == EventType.MouseUp)
            {
                m_Drag = false;
            }

            if (m_Drag)
            {
                var delta = Event.current.mousePosition - m_MousePosition;
                Position += delta.x;
                repaint = true;

                if (rect.width > 0)
                { 
                    Position = Mathf.Clamp(Position, MinLeft, rect.width - MinRight);
                }
            }

            m_MousePosition = Event.current.mousePosition;

            return repaint;
        }
        
        #endregion

        public void SetDirty()
        {
            var left = Left as IDirtyable;
            
            if (left != null)
            {
                left.SetDirty();
            }
            
            var right = Right as IDirtyable;
            
            if (right != null)
            {
                right.SetDirty();
            }
        }
    }
}

