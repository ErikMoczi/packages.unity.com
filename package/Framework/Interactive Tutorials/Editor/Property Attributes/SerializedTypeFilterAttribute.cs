using System;

namespace Unity.InteractiveTutorials
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializedTypeFilterAttribute : Attribute
    {
        public Type baseType { get; private set; }

        public SerializedTypeFilterAttribute(Type baseType)
        {
            this.baseType = baseType;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializedTypeGUIViewFilterAttribute : Attribute
    {
        public Type baseType { get; private set; }

        public SerializedTypeGUIViewFilterAttribute()
        {
            this.baseType = GUIViewProxy.guiViewType;
        }
    }
}
