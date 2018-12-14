

using UnityEngine;

namespace Unity.Tiny.Runtime.UILayout
{
    internal partial struct TinyRectTransform
    {
        public Vector2 offsetMin
        {
            get { return anchoredPosition - Vector2.Scale(sizeDelta, pivot); }
            set
            {
                var offset = value - (anchoredPosition - Vector2.Scale(sizeDelta, pivot));
                sizeDelta -= offset;
                anchoredPosition += Vector2.Scale(offset, Vector2.one - pivot);
            }
        }

        public Vector2 offsetMax
        {
            get
            {
                return anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot);
            }
            set
            {
                var offset = value - (anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot));
                sizeDelta += offset;
                anchoredPosition += Vector2.Scale(offset, pivot);
            }
        }

    }
}

