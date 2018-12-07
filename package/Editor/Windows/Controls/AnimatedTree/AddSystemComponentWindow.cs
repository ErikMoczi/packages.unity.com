

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Unity.Tiny
{
    internal class AddSystemComponentWindow : TinyAnimatedTreeWindow<AddSystemComponentWindow, TinyType>
    {
        private List<TinyType> m_AllowedTypes;
        private Action<TinyType.Reference> m_OnTypeClicked;

        public static bool Show(Rect rect, IRegistry registry, List<TinyType> allowedTypes, Action<TinyType.Reference> onTypeClicked)
        {
            var window = GetWindow();
            if (null == window)
            {
                return false;
            }
            window.m_AllowedTypes = allowedTypes;
            window.m_OnTypeClicked = onTypeClicked;
            return Show(rect, registry);
        }

        protected override IEnumerable<TinyType> GetItems(TinyModule module)
        {
            return module.Components.Deref(Registry);
        }

        protected override void OnItemClicked(TinyType type)
        {
            m_OnTypeClicked?.Invoke((TinyType.Reference)type);
        }

        protected override bool FilterItem(TinyType type)
        {
            return m_AllowedTypes.Contains(type);
        }

        protected override string TreeName()
        {
            return $"{TinyConstants.ApplicationName} Components";
        }
    }
}

