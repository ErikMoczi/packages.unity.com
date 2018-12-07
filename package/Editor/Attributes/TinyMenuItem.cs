
using System;

namespace Unity.Tiny
{
    internal abstract class TinyMenuItemAttribute : TinyAttribute
    {
        public const string NoValidation = "__NoValidation__";

        public readonly string ItemNamePrefix;
        public readonly string ValidationMethodName;

        protected TinyMenuItemAttribute(string itemNamePrefix, int order = 1000)
            : this(itemNamePrefix, NoValidation, order)
        {
        }

        protected TinyMenuItemAttribute(string itemNamePrefix, string validationMethodName, int order = 1000)
            : base(order)
        {
            ItemNamePrefix = itemNamePrefix;
            ValidationMethodName = validationMethodName;
        }
    }

    internal class TinyEntityTemplateMenuItemAttribute : TinyMenuItemAttribute
    {
        public TinyEntityTemplateMenuItemAttribute(string itemNamePrefix, int order = 1000)
            : base(itemNamePrefix, order) { }

        public TinyEntityTemplateMenuItemAttribute(string itemNamePrefix, string validationMethodName, int order = 1000)
            : base(itemNamePrefix, validationMethodName, order) { }
    }
}
